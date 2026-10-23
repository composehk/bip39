using System.Globalization;

namespace Bip39Net;

/// <summary>
/// Argument parser and text UI for the console demo.
/// Every <see cref="Run"/> prints <see cref="SecurityBanner"/> before doing work.
/// </summary>
public static class Bip39Cli
{
    public const string SecurityBanner =
        """
        ========================================================================
        SECURITY NOTICE / 安全声明
        仅供学习/演示，勿用于真实资金；勿在联网/不可信环境输入真实助记词。
        For learning and demonstration only. Do not use with real funds.
        Do not enter a real mnemonic on a networked or untrusted machine.
        ========================================================================
        """;

    public const string UsageText =
        """
        Bip39Net — composehk BIP39 learning demo (.NET 8, English wordlist)

        Usage:
          dotnet run -- generate [--words 12|15|18|21|24]
          dotnet run -- validate <mnemonic words...>
          dotnet run -- derive <mnemonic words...> [--passphrase <text>] [--path <bip32 path>]

        Defaults:
          generate uses 12 words.
          derive uses an empty BIP39 passphrase and path m/44'/0'/0'/0/0
          (first BIP44 Bitcoin receive key).

        derive prints the 64-byte seed (hex) and the xprv/xpub at --path,
        plus P2PKH and P2WPKH address previews of that same key.
        Checksum, seed, and BIP32 math are provided by NBitcoin.

        Exit codes: 0 success, 1 invalid mnemonic or path, 2 usage error.
        """;

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        stdout.WriteLine(SecurityBanner);
        stdout.WriteLine();

        ParsedArgs parsed;
        try
        {
            parsed = ParsedArgs.Parse(args);
        }
        catch (UsageException ex)
        {
            return Usage(stderr, ex.Message);
        }

        if (parsed.Positionals.Count == 0 || parsed.Help)
        {
            stdout.WriteLine(UsageText);
            var exit = parsed.Positionals.Count == 0 && !parsed.Help ? 2 : 0;
            Flush(stdout, stderr);
            return exit;
        }

        var command = parsed.Positionals[0].ToLowerInvariant();
        var words = parsed.Positionals.Skip(1).ToArray();

        try
        {
            var exit = command switch
            {
                "generate" => Generate(parsed, words, stdout),
                "validate" => Validate(parsed, words, stdout),
                "derive" => Derive(parsed, words, stdout, stderr),
                _ => Usage(stderr, $"Unknown command '{parsed.Positionals[0]}'.")
            };
            Flush(stdout, stderr);
            return exit;
        }
        catch (UsageException ex)
        {
            return Usage(stderr, ex.Message);
        }
    }

    static int Generate(ParsedArgs parsed, string[] words, TextWriter stdout)
    {
        parsed.EnsureOnly("words");
        if (words.Length > 0)
        {
            throw new UsageException("generate does not take a mnemonic. Use --words N.");
        }

        var count = 12;
        if (parsed.Options.TryGetValue("words", out var raw)
            && !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out count))
        {
            throw new UsageException("--words must be an integer: 12, 15, 18, 21, or 24.");
        }

        string mnemonic;
        try
        {
            mnemonic = Bip39Service.Generate(count);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new UsageException("--words must be 12, 15, 18, 21, or 24.");
        }

        stdout.WriteLine($"words: {count}");
        stdout.WriteLine($"mnemonic: {mnemonic}");
        return 0;
    }

    static int Validate(ParsedArgs parsed, string[] words, TextWriter stdout)
    {
        parsed.EnsureOnly();
        if (words.Length == 0)
        {
            throw new UsageException("validate requires a mnemonic.");
        }

        var result = Bip39Service.Validate(string.Join(' ', words));
        stdout.WriteLine($"valid: {(result.IsValid ? "true" : "false")}");
        stdout.WriteLine($"words: {result.WordCount}");
        if (result.Mnemonic.Length > 0)
        {
            stdout.WriteLine($"mnemonic: {result.Mnemonic}");
        }

        if (!result.IsValid)
        {
            stdout.WriteLine($"reason: {result.Reason}");
            return 1;
        }

        return 0;
    }

    static int Derive(ParsedArgs parsed, string[] words, TextWriter stdout, TextWriter stderr)
    {
        parsed.EnsureOnly("passphrase", "path");
        if (words.Length == 0)
        {
            throw new UsageException("derive requires a mnemonic.");
        }

        parsed.Options.TryGetValue("passphrase", out var passphrase);
        parsed.Options.TryGetValue("path", out var path);

        DerivationPreview preview;
        try
        {
            preview = Bip39Service.Derive(string.Join(' ', words), passphrase, path);
        }
        catch (ArgumentException ex)
        {
            stderr.WriteLine($"error: {ex.Message}");
            return 1;
        }

        stdout.WriteLine($"mnemonic: {preview.Mnemonic}");
        stdout.WriteLine(preview.Passphrase.Length == 0
            ? "passphrase: (empty)"
            : $"passphrase: {preview.Passphrase}");
        stdout.WriteLine($"seed: {preview.SeedHex}");
        stdout.WriteLine($"path: {preview.Path}");
        stdout.WriteLine($"xprv: {preview.Xprv}");
        stdout.WriteLine($"xpub: {preview.Xpub}");
        stdout.WriteLine($"address_p2pkh: {preview.AddressP2pkh}");
        stdout.WriteLine($"address_p2wpkh: {preview.AddressP2wpkh}");
        return 0;
    }

    static int Usage(TextWriter stderr, string message)
    {
        stderr.WriteLine($"error: {message}");
        stderr.WriteLine();
        stderr.WriteLine(UsageText);
        stderr.Flush();
        return 2;
    }

    static void Flush(TextWriter stdout, TextWriter stderr)
    {
        stdout.Flush();
        stderr.Flush();
    }

    sealed class ParsedArgs
    {
        public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Positionals { get; } = new();
        public bool Help { get; private set; }

        public static ParsedArgs Parse(string[] args)
        {
            var parsed = new ParsedArgs();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg == "--")
                {
                    continue;
                }

                if (arg is "-h" or "--help" or "help")
                {
                    parsed.Help = true;
                    continue;
                }

                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    var body = arg[2..];
                    string name;
                    string value;
                    var eq = body.IndexOf('=');
                    if (eq >= 0)
                    {
                        name = body[..eq];
                        value = body[(eq + 1)..];
                    }
                    else
                    {
                        name = body;
                        if (i + 1 >= args.Length || args[i + 1].StartsWith('-'))
                        {
                            throw new UsageException($"Missing value for --{name}.");
                        }

                        value = args[++i];
                    }

                    if (name is not ("words" or "passphrase" or "path"))
                    {
                        throw new UsageException($"Unknown option '--{name}'.");
                    }

                    parsed.Options[name] = value;
                    continue;
                }

                if (arg.StartsWith('-'))
                {
                    throw new UsageException($"Unknown option '{arg}'.");
                }

                parsed.Positionals.Add(arg);
            }

            return parsed;
        }

        public void EnsureOnly(params string[] allowed)
        {
            foreach (var key in Options.Keys)
            {
                var known = false;
                foreach (var name in allowed)
                {
                    if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                {
                    throw new UsageException($"Option --{key} is not valid for this command.");
                }
            }
        }
    }

    sealed class UsageException : Exception
    {
        public UsageException(string message) : base(message)
        {
        }
    }
}
