using System.Buffers;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Nito.Disposables;
using Spectre.Console;
using System.Collections.Concurrent;
using NNostr.Client;

var overallCts = new CancellationTokenSource();
var servers = new ConcurrentDictionary<string, CancellationTokenSource>();

// each relay listener, aka NostrRelay, has it's own cts linked to the overall
servers.TryAdd("wss://relay.damus.io", CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token));
servers.TryAdd("wss://node01.nostress.cc", CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token));
servers.TryAdd("wss://nostr.ethtozero.fr", CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token));
servers.TryAdd("wss://nostr.bongbong.com", CancellationTokenSource.CreateLinkedTokenSource(overallCts.Token));

using var resultProcessor = new WebSocketReceiveResultProcessor();
var dispatch = (string url, ReadOnlySequence<byte> data, IDisposable releaseBuffer) =>
{
    try
    {
        var strData = Encoding.UTF8.GetString(data);
        using var doc = JsonDocument.Parse(strData);
        var json = doc.RootElement;
        var messageType = json[0].GetString()?.ToUpperInvariant() ?? string.Empty;
        switch (messageType)
        {
            case "EVENT":
                var subId = json[1].GetString() ?? string.Empty;
                var ev = json[2].Deserialize<NostrEvent>();
                if (ev?.Verify() == true)
                {
                    AnsiConsole.Write(new Markup($"[bold green]From {url}:[/] {ev.Content.EscapeMarkup()}\n"));
                }
                break;
            case "NOTICE":
                var msg = json[1].GetString();
                if (!string.IsNullOrEmpty(msg))
                {
                    AnsiConsole.Write(new Markup($"[yellow]Notice From {url}:[/] {msg.EscapeMarkup()}\n"));
                }
                break;
            case "EOSE":
                AnsiConsole.WriteLine($"{messageType} from {url}, disconnecting ..\n");
                if (servers.TryGetValue(url, out var cts))
                {
                    cts.Cancel(false);
                }
                break;
            //case "OK":
            //    break;
            default:
                AnsiConsole.WriteLine($"{messageType} from {url}\n");
                break;
        }
    }
    catch (TaskCanceledException)
    {
        // do nothing
    }
    catch (Exception)
    {
        throw;
    }
    finally
    {
        releaseBuffer.Dispose();
    }
};

// Start a new task to monitor for cancellation
_ = Task.Run(() => MonitorKeyboard(overallCts.Token));

// Connect to each server
foreach (var server in servers)
{
    _ = Task.Run(async () => await ConnectToServer(server.Key, resultProcessor, dispatch, server.Value.Token));
}

while (!overallCts.IsCancellationRequested)
{
    await Task.Yield();
}

static ArraySegment<byte> RentBuffer(int receiveChunkSize = 10000)
{
    return ArrayPool<byte>.Shared.Rent(receiveChunkSize); // Rent a buffer.
}

void ReturnRentedBuffer(ReadOnlySequence<byte> data)
{
    foreach (var chunk in data)
    {
        if (MemoryMarshal.TryGetArray(chunk, out var segment) && segment.Array != null)
            ArrayPool<byte>.Shared.Return(segment.Array);
    }
}

async Task ConnectToServer(string url, WebSocketReceiveResultProcessor resultProcessor, Action<string, ReadOnlySequence<byte>, IDisposable> dispatch, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            // Create a new ClientWebSocket
            using var client = new ClientWebSocket()
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(15)
                }
            };

            // Connect to the server
            AnsiConsole.WriteLine($"Connecting to: {url} ..");
            await client.ConnectAsync(new Uri(url), token);
            AnsiConsole.WriteLine($"{url} connected!");

            // Start a new cancellable task to receive messages from the server
            _ = Task.Run(async () => await ReceiveMessages(url, client, resultProcessor, dispatch, token), token);

            // Send a sub message to the server to get all of @jack's messages
            var filter = new NostrSubscriptionFilter();
            filter.Kinds = new List<int>() { 1 }.ToArray();
            filter.Authors = new Collection<string>() { "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2" }.ToArray();
            var filters = new List<NostrSubscriptionFilter>() { filter };
            var message = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new object[] { "REQ", "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2" }.Concat(filters))));
            await client.SendAsync(message, WebSocketMessageType.Text, true, token);

            // wait until disconnected
            while (client.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                await Task.Delay(500, token);
            }

            AnsiConsole.WriteLine($"{url} disconnected!");
        }
        catch (TaskCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine("Error connecting to server: " + ex.Message);
        }

        await Task.Delay(5000, token);
    }
}

async Task ReceiveMessages(string url, ClientWebSocket client, WebSocketReceiveResultProcessor resultProcessor, Action<string, ReadOnlySequence<byte>, IDisposable> dispatch, CancellationToken token)
{
    while (client.State == WebSocketState.Open && !token.IsCancellationRequested)
    {
        try
        {
            var buffer = RentBuffer();

            // Wait for a message from the server
            var result = await client.ReceiveAsync(buffer, token);

            if (resultProcessor.Receive(result, buffer, out var frame))
            {
                if (frame.IsEmpty)
                    break; // End of message with no data means socket closed - break so we can reconnect.

                // Send the frame, and delegate consumer should call to release the buffer once done.
                dispatch(url, frame, Disposable.Create(() => ReturnRentedBuffer(frame)));
            }
        }
        catch (TaskCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);

            // if an exception occurs, we close the websocket
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Error receiving message", CancellationToken.None);
        }
    }
}

void MonitorKeyboard(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
        {
            overallCts.Cancel();
            AnsiConsole.WriteLine("Cancellation requested by the user. Stopping all the connections...");
            break;
        }
    }
}

internal class Chunk<T> : ReadOnlySequenceSegment<T>
{
    public Chunk(ReadOnlyMemory<T> memory)
    {
        Memory = memory;
    }

    public Chunk<T> Add(ReadOnlyMemory<T> mem)
    {
        var segment = new Chunk<T>(mem)
        {
            RunningIndex = RunningIndex + Memory.Length
        };

        Next = segment;
        return segment;
    }
}

internal sealed class WebSocketReceiveResultProcessor : IDisposable
{
    private Chunk<byte>? _currentChunk = null;
    private Chunk<byte>? _startChunk = null;

    public void Dispose()
    {
        // Release any partial decoded chunks.
        var chunk = _startChunk;

        while (chunk != null)
        {
            if (MemoryMarshal.TryGetArray(chunk.Memory, out var segment) && segment.Array != null)
                ArrayPool<byte>.Shared.Return(segment.Array);

            chunk = (Chunk<byte>)chunk.Next!;
        }

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    public bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Close)
        {
            frame = default;
            return false;
        }

        var slice = buffer.Slice(0, result.Count);

        if (_startChunk == null)
        {
            _startChunk = _currentChunk = new Chunk<byte>(slice);
        }
        else
        {
            _currentChunk ??= new Chunk<byte>(slice);
            _currentChunk = _currentChunk.Add(slice);
        }

        if (result.EndOfMessage && _startChunk != null)
        {
            frame = _startChunk.Next == null ?
                new ReadOnlySequence<byte>(_startChunk.Memory) : new ReadOnlySequence<byte>(_startChunk, 0, _currentChunk, _currentChunk.Memory.Length);

            _startChunk = _currentChunk = null; // Reset so we can accept new chunks from scratch.
            return true;
        }
        else
        {
            frame = default;
            return false;
        }
    }
}
