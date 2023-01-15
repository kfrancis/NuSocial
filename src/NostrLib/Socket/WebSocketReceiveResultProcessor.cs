using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace NostrLib.Socket
{
    public sealed class Chunk<T> : ReadOnlySequenceSegment<T>
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

    public class WebSocketReceiveResultProcessor : IDisposable
    {
        private Chunk<byte>? _currentChunk;
        private bool _disposedValue;
        private Chunk<byte>? _startChunk;

        ~WebSocketReceiveResultProcessor()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out ReadOnlySequence<byte> frame)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

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

            if (result.EndOfMessage)
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Release any partial decoded chunks.
                    var chunk = _startChunk;

                    while (chunk != null)
                    {
                        if (MemoryMarshal.TryGetArray(chunk.Memory, out var segment) && segment.Array != null)
                            ArrayPool<byte>.Shared.Return(segment.Array);

                        chunk = (Chunk<byte>)chunk.Next!;
                    }
                }

                _disposedValue = true;
            }
        }
    }
}
