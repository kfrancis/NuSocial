using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using NBitcoin;
using NBitcoin.Secp256k1;
using Spectre.Console;
using Spectre.Console.Cli;
using static System.Net.Mime.MediaTypeNames;

namespace NostrKey
{
    internal class Program
    {
        static int Main(string[] args)
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

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            int numCores = GetCores();
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

            if (!string.IsNullOrEmpty(settings.VanityPubPrefix))
            {
                // Benchmarking of cores disabled for vanity npub key upon proper calculation.
            }
            else
            {
                BenchmarkCores(numCores, powDifficulty);
            }

            var key = GenerateScalar();

            ECPrivKey? privKey = null;

            try
            {
                if (Context.Instance.TryCreateECPrivKey(key, out privKey))
                {
                    var pubKey = privKey.CreateXOnlyPubKey();
                    Span<byte> privKeyc = stackalloc byte[65];
                    privKey.WriteToSpan(privKeyc);
                    var pubKeyStr = pubKey.ToBytes().ToHex();
                    var privKeyStr = privKeyc.ToHex()[..64];

                    AnsiConsole.MarkupLine($"Public Key: [green]{pubKeyStr}[/]");
                    AnsiConsole.MarkupLine($"Private Key: [green]{privKeyStr}[/]");
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

            return 0;
        }

        private void BenchmarkCores(int numCores, int powDifficulty)
        {
            var hashes_per_second_per_core = 0;

            AnsiConsole.WriteLine("Benchmarking a single core for 5 seconds...");
            Stopwatch sw = new Stopwatch();
            int msLimit = 5000;
            bool bDoExit = false;
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

            hashes_per_second_per_core /= 10;
            AnsiConsole.WriteLine($"A single core can mine roughly {hashes_per_second_per_core} h/s!");
        }

        private int GetCores()
        {
            return Environment.ProcessorCount;
        }

        public sealed class Settings : CommandSettings
        {
            [Description("Difficulty")]
            [CommandArgument(0, "<difficulty>")]
            public int Difficulty { get; init; }

            [Description("Vanity Prefix")]
            [CommandOption("-v|--vanityPrefix <PREFIX>")]
            [DefaultValue("")]
            public string VanityPrefix { get; init; }

            [CommandOption("-c|--vanityPubPrefix <PREFIX>")]
            [DefaultValue("")]
            public string VanityPubPrefix { get; init; }
        }
    }
}
