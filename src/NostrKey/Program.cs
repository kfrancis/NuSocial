using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using NBitcoin;
using NBitcoin.Secp256k1;
using SimpleBase;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NostrKey
{
    internal static class Extensions
    {
        public static string ToHex(this byte[] bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToHex());
            }

            return builder.ToString();
        }

        public static string ToHex(this Span<byte> bytes)
        {
            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToHex());
            }

            return builder.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        private static string ToHex(this byte b)
        {
            return b.ToString("x2");
        }
    }

    internal sealed class GenerateNostrKeyCommand : Command<GenerateNostrKeyCommand.Settings>
    {
        private readonly object _obj = new();

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var powDifficulty = settings.Difficulty;

            if (!string.IsNullOrEmpty(settings.VanityPrefix) || !string.IsNullOrEmpty(settings.VanityPubPrefix))
            {
                powDifficulty = 1; // initialize for further multiplication

                if (!string.IsNullOrEmpty(settings.VanityPrefix))
                {
                    // set pow difficulty as the length of the prefix translated to bits
                    powDifficulty *= (settings.VanityPrefix.Length * 4);
                }

                if (!string.IsNullOrEmpty(settings.VanityPubPrefix))
                {
                    // set pow difficulty as the length of the prefix translated to bits
                    powDifficulty *= (settings.VanityPubPrefix.Length * 4);
                }

                AnsiConsole.WriteLine($"Started mining process for a vanify prefix of: '{settings.VanityPrefix}' and 'npub1{settings.VanityPubPrefix}' (estimated pow: {powDifficulty})");
            }
            else
            {
                // Defaults to using difficulty
                AnsiConsole.WriteLine($"Started mining process with a difficulty of: {powDifficulty}");
            }

            var numCores = Environment.ProcessorCount;
            if (!string.IsNullOrEmpty(settings.VanityPubPrefix))
            {
                // Benchmarking of cores disabled for vanity npub key upon proper calculation.
            }
            else
            {
                BenchmarkCores(numCores, powDifficulty);
            }

            if (powDifficulty > 0)
            {
                var iterationCount = 0L;
                var workerThread = (long bestDiff, string prefix, string pubPrefix) =>
                {
                    var is_valid_pubkey = false;
                    var now = DateTime.Now;
                    while (!is_valid_pubkey)
                    {
                        Interlocked.Increment(ref iterationCount);

                        // something
                        var rng = GenerateScalar();
                        var (privateKey, publicKey) = GenerateKeyPair(rng);
                        var leading_zeroes = 0;

                        if (!string.IsNullOrEmpty(prefix) || !string.IsNullOrEmpty(pubPrefix))
                        {
                            var hexa_key = publicKey.ToBytes().ToHex();
                            var is_valid_pubkey_hex = true;
                            var is_valid_pubkey_bech32 = true;

                            if (!string.IsNullOrEmpty(prefix))
                            {
                                is_valid_pubkey_hex = hexa_key.StartsWith(prefix, StringComparison.Ordinal);
                            }

                            if (!string.IsNullOrEmpty(pubPrefix))
                            {
                                var prefixBytes = Encoding.ASCII.GetBytes(pubPrefix + hexa_key);
                                var bechKey = Base32.Bech32.Encode(prefixBytes);
                                is_valid_pubkey_bech32 = bechKey.StartsWith("npub1" + pubPrefix, StringComparison.Ordinal);
                            }

                            // only valid if both options are valid
                            // it one of both were not required, then it's considered valid
                            is_valid_pubkey = is_valid_pubkey_hex && is_valid_pubkey_bech32;
                        }
                        else
                        {
                            leading_zeroes = GetLeadingZeroBits(publicKey);
                            is_valid_pubkey = leading_zeroes > powDifficulty;
                            if (is_valid_pubkey)
                            {
                                AnsiConsole.WriteLine($"Leading zero bits: {leading_zeroes}");
                            }
                        }

                        if (is_valid_pubkey)
                        {
                            var iterations = Interlocked.Read(ref iterationCount);
                            var iterString = $"{iterations}";
                            var l = iterString.Length;
                            var f = iterString[0];

                            PrintKeys(privateKey, publicKey, iterations, f, l, now);

                            if (Interlocked.Read(ref bestDiff) > 0)
                            {
                                Interlocked.Exchange(ref bestDiff, leading_zeroes);
                            }
                        }
                    }
                };

                var threadCount = Math.Max(1, Math.Min(numCores, Math.Min(8, 8)));
                var threads = new Task[threadCount - 1];
                for (var i = 0; i < threads.Length; i++)
                {
                    threads[i] = Task.Run(() => workerThread(powDifficulty, settings.VanityPrefix, settings.VanityPubPrefix));
                }
                AnsiConsole.Status().Start("Thinking...", ctx =>
                {
                    workerThread(powDifficulty, settings.VanityPrefix, settings.VanityPubPrefix);
                    for (var i = 0; i < threads.Length; i++)
                    {
                        threads[i].Wait();
                    }
                });
            }
            else
            {
                var key = GenerateScalar();

                ECPrivKey? privKey = null;

                try
                {
                    if (Context.Instance.TryCreateECPrivKey(key, out privKey))
                    {
                        var pubKey = privKey.CreateXOnlyPubKey();

                        PrintKeys(privKey, pubKey);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                finally
                {
                    privKey?.Dispose();
                }
            }

            return 0;
        }

        private static void BenchmarkCores(int numCores, int powDifficulty)
        {
            var hashes_per_second_per_core = 0;

            AnsiConsole.WriteLine("Benchmarking a single core for 5 seconds...");
            var sw = new Stopwatch();
            var msLimit = 5000;
            var bDoExit = false;
            sw.Start();
            while (!bDoExit)
            {
                if (sw.Elapsed.TotalMilliseconds > msLimit)
                {
                    bDoExit = true;
                    sw.Stop();
                }

                if (Context.Instance.TryCreateECPrivKey(GenerateScalar(), out var privKey))
                {
                    _ = privKey.CreateXOnlyPubKey();
                    hashes_per_second_per_core += 1;
                }
            }

            hashes_per_second_per_core /= numCores;
            AnsiConsole.WriteLine($"A single core can mine roughly {hashes_per_second_per_core} h/s!");
        }

        private static (ECPrivKey privateKey, ECXOnlyPubKey publicKey) GenerateKeyPair(Scalar rng)
        {
            ECPrivKey? privKey = null;

            if (Context.Instance.TryCreateECPrivKey(rng, out privKey))
            {
                var pubKey = privKey.CreateXOnlyPubKey();

                return (privKey, pubKey);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static Scalar GenerateScalar()
        {
            var scalar = Scalar.Zero;
            Span<byte> output = stackalloc byte[32];
            do
            {
                RandomUtils.GetBytes(output);
                scalar = new Scalar(output, out var overflow);
                if (overflow != 0 || scalar.IsZero)
                {
                    continue;
                }
                break;
            } while (true);
            return scalar;
        }

        private static int GetLeadingZeroBits(ECXOnlyPubKey publicKey)
        {
            byte res = 0;
            foreach (var b in publicKey.ToBytes())
            {
                if (b == 0)
                {
                    res += 8;
                }
                else
                {
                    res += (byte)BitOperations.LeadingZeroCount(b);
                    return res;
                }
            }
            return res;
        }

        private void PrintKeys(ECPrivKey privKey, ECXOnlyPubKey pubKey)
        {
            lock (_obj)
            {
                Span<byte> privKeyc = stackalloc byte[65];
                privKey.WriteToSpan(privKeyc);
                var pubKeyStr = pubKey.ToBytes().ToHex();
                var privKeyStr = privKeyc.ToHex()[..64];

                AnsiConsole.MarkupLine($"Public Key: [green]{pubKeyStr}[/]");
                AnsiConsole.MarkupLine($"Private Key: [green]{privKeyStr}[/]");
            }
        }

        private void PrintKeys(ECPrivKey privKey, ECXOnlyPubKey pubKey, long iterations, char f, int l, DateTime now)
        {
            lock (_obj)
            {
                Span<byte> privKeyc = stackalloc byte[65];
                privKey.WriteToSpan(privKeyc);
                var pubKeyStr = pubKey.ToBytes().ToHex();
                var privKeyStr = privKeyc.ToHex()[..64];

                AnsiConsole.MarkupLine($"Public Key: [green]{pubKeyStr}[/]");
                AnsiConsole.MarkupLine($"Private Key: [green]{privKeyStr}[/]");

                AnsiConsole.WriteLine("{0} iterations (about {1}x10^{2} hashes) in {3} seconds. Avg rate {4} hashes/second",
                                iterations, f, l - 1, DateTime.Now.Subtract(now).TotalSeconds,
                                iterations / Math.Max(1, DateTime.Now.Subtract(now).TotalSeconds));
            }
        }

        public sealed class Settings : CommandSettings
        {
            [Description("Difficulty")]
            [CommandArgument(0, "[difficulty]")]
            [DefaultValue(0)]
            public int Difficulty { get; init; }

            [Description("Vanity Prefix")]
            [CommandOption("-p|--vanityPrefix <PREFIX>")]
            [DefaultValue("")]
            public string VanityPrefix { get; init; }

            [CommandOption("-c|--vanityPubPrefix <PREFIX>")]
            [DefaultValue("")]
            public string VanityPubPrefix { get; init; }
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // this makes emojis come up more reliably. might get built into spectre better in the future, so give a go deleting this at some point
                // they seem to show up fine on osx and actually need this to be off to work there
                Console.OutputEncoding = new System.Text.UTF8Encoding();
            }

            var app = new CommandApp<GenerateNostrKeyCommand>();
            return app.Run(args);
        }
    }
}
