using Bip39Net;

namespace Bip39Net.Tests;

public class CliTests
{
    const string Abandon12 =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    const string Seed12 =
        "c55257c360c07c72029aebc1b53c05ed0362ada38ead3e3e9efa3708e53495531f09a6987599d18264c1e1c92f2cf141630c7a3c4ab7c81b2f001698e7463b04";

    [Fact]
    public void Every_command_prints_the_security_banner()
    {
        var invocations = new[]
        {
            Array.Empty<string>(),
            new[] { "generate" },
            new[] { "generate", "--words", "15" },
            new[] { "validate", "abandon" },
            Abandon12.Split(' ').Prepend("derive").Append("--passphrase").Append("TREZOR").ToArray()
        };

        foreach (var args in invocations)
        {
            var (code, stdout, _) = Run(args);
            Assert.Contains(
                "仅供学习/演示，勿用于真实资金；勿在联网/不可信环境输入真实助记词。",
                stdout,
                StringComparison.Ordinal);
            Assert.Contains("Do not use with real funds", stdout, StringComparison.Ordinal);
            Assert.InRange(code, 0, 2);
        }
    }

    [Fact]
    public void Generate_default_is_twelve_words()
    {
        var (code, stdout, stderr) = Run("generate");

        Assert.Equal(0, code);
        Assert.Equal("", stderr);
        Assert.Contains("words: 12", stdout, StringComparison.Ordinal);
        var mnemonic = Field(stdout, "mnemonic");
        Assert.Equal(12, mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.True(Bip39Service.Validate(mnemonic).IsValid);
    }

    [Fact]
    public void Generate_words_flag_sets_length()
    {
        var (code, stdout, _) = Run("generate", "--words=24");

        Assert.Equal(0, code);
        Assert.Contains("words: 24", stdout, StringComparison.Ordinal);
        Assert.Equal(24, Field(stdout, "mnemonic").Split(' ').Length);
    }

    [Fact]
    public void Validate_known_vector_exits_zero()
    {
        var args = Abandon12.Split(' ').Prepend("validate").ToArray();
        var (code, stdout, _) = Run(args);

        Assert.Equal(0, code);
        Assert.Contains("valid: true", stdout, StringComparison.Ordinal);
        Assert.Contains("words: 12", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_bad_checksum_exits_one()
    {
        var args = Enumerable.Repeat("abandon", 12).Prepend("validate").ToArray();
        var (code, stdout, _) = Run(args);

        Assert.Equal(1, code);
        Assert.Contains("valid: false", stdout, StringComparison.Ordinal);
        Assert.Contains("Checksum", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Derive_prints_official_seed_and_address_preview()
    {
        var args = Abandon12.Split(' ')
            .Prepend("derive")
            .Append("--passphrase")
            .Append("TREZOR")
            .Append("--path")
            .Append("m/44'/0'/0'/0/0")
            .ToArray();

        var (code, stdout, stderr) = Run(args);

        Assert.Equal(0, code);
        Assert.Equal("", stderr);
        Assert.Equal(Seed12, Field(stdout, "seed"));
        Assert.Equal("m/44'/0'/0'/0/0", Field(stdout, "path"));
        Assert.StartsWith("xprv", Field(stdout, "xprv"), StringComparison.Ordinal);
        Assert.StartsWith("xpub", Field(stdout, "xpub"), StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_command_is_a_usage_error()
    {
        var (code, stdout, stderr) = Run();

        Assert.Equal(2, code);
        Assert.Contains("Usage:", stdout, StringComparison.Ordinal);
        Assert.Equal("", stderr);
    }

    static (int Code, string Stdout, string Stderr) Run(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var code = Bip39Cli.Run(args, stdout, stderr);
        return (code, stdout.ToString(), stderr.ToString());
    }

    static string Field(string stdout, string name)
    {
        var prefix = name + ": ";
        var line = stdout.Split('\n').Select(static l => l.TrimEnd('\r'))
            .FirstOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal));
        Assert.NotNull(line);
        return line[prefix.Length..];
    }
}
