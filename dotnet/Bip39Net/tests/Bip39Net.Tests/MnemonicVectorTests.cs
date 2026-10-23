using Bip39Net;

namespace Bip39Net.Tests;

/// <summary>
/// Official English BIP39 vectors (passphrase "TREZOR") from
/// https://github.com/trezor/python-mnemonic/blob/master/vectors.json
/// plus the widely published empty-passphrase BIP44 address.
/// </summary>
public class MnemonicVectorTests
{
    const string Abandon12 =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    const string Abandon18 =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon agent";

    const string Abandon24 =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art";

    const string Seed12 =
        "c55257c360c07c72029aebc1b53c05ed0362ada38ead3e3e9efa3708e53495531f09a6987599d18264c1e1c92f2cf141630c7a3c4ab7c81b2f001698e7463b04";

    const string Seed18 =
        "035895f2f481b1b0f01fcf8c289c794660b289981a78f8106447707fdd9666ca06da5a9a565181599b79f53b844d8a71dd9f439c52a3d7b3e8a79c906ac845fa";

    const string Seed24 =
        "bda85446c68413707090a52022edd26a1c9462295029f2e60cd7c4f2bbd3097170af7a4d73245cafa9c3cca8d561a7c3de6f5d4a10be8ed2a5e608d68f92fcc8";

    [Theory]
    [InlineData(Abandon12, 12)]
    [InlineData(Abandon18, 18)]
    [InlineData(Abandon24, 24)]
    public void Validate_accepts_official_english_vectors(string mnemonic, int words)
    {
        var result = Bip39Service.Validate(mnemonic);

        Assert.True(result.IsValid);
        Assert.Null(result.Reason);
        Assert.Equal(words, result.WordCount);
        Assert.Equal(mnemonic, result.Mnemonic);
    }

    [Fact]
    public void Validate_accepts_uppercase_and_extra_whitespace()
    {
        var result = Bip39Service.Validate(
            "  ABANDON abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon ABOUT  ");

        Assert.True(result.IsValid);
        Assert.Equal(Abandon12, result.Mnemonic);
    }

    [Fact]
    public void Validate_rejects_bad_checksum()
    {
        var result = Bip39Service.Validate(string.Join(' ', Enumerable.Repeat("abandon", 12)));

        Assert.False(result.IsValid);
        Assert.Equal(12, result.WordCount);
        Assert.Contains("Checksum", result.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_rejects_unknown_word()
    {
        var result = Bip39Service.Validate(
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon notaword");

        Assert.False(result.IsValid);
        Assert.Contains("notaword", result.Reason, StringComparison.Ordinal);
        Assert.Contains("wordlist", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_rejects_wrong_word_count()
    {
        var result = Bip39Service.Validate("abandon abandon abandon");

        Assert.False(result.IsValid);
        Assert.Contains("Word count", result.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(18)]
    [InlineData(21)]
    [InlineData(24)]
    public void Generate_returns_valid_mnemonic_of_requested_length(int words)
    {
        for (var i = 0; i < 3; i++)
        {
            var mnemonic = Bip39Service.Generate(words);
            var result = Bip39Service.Validate(mnemonic);

            Assert.True(result.IsValid, result.Reason);
            Assert.Equal(words, mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            Assert.Equal(words, result.WordCount);
        }
    }

    [Fact]
    public void Generate_rejects_unsupported_word_count()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Bip39Service.Generate(13));
        Assert.Equal(13, ex.ActualValue);
    }

    [Theory]
    [InlineData(Abandon12, Seed12)]
    [InlineData(Abandon18, Seed18)]
    [InlineData(Abandon24, Seed24)]
    public void Derive_matches_official_trezor_seed(string mnemonic, string seedHex)
    {
        var preview = Bip39Service.Derive(mnemonic, "TREZOR", "m/44'/0'/0'/0/0");

        Assert.Equal(seedHex, preview.SeedHex);
        Assert.Equal(128, preview.SeedHex.Length);
        Assert.Equal("m/44'/0'/0'/0/0", preview.Path);
        Assert.StartsWith("xprv", preview.Xprv, StringComparison.Ordinal);
        Assert.StartsWith("xpub", preview.Xpub, StringComparison.Ordinal);
    }

    [Fact]
    public void Derive_empty_passphrase_matches_known_bip44_address()
    {
        var preview = Bip39Service.Derive(Abandon12, "", "m/44'/0'/0'/0/0");

        Assert.Equal("1LqBGSKuX5yYUonjxT5qGfpUsXKYYWeabA", preview.AddressP2pkh);
        Assert.Equal("bc1qmxrw6qdh5g3ztfcwm0et5l8mvws4eva24kmp8m", preview.AddressP2wpkh);
        Assert.NotEqual(Seed12, preview.SeedHex);
    }

    [Fact]
    public void Derive_passphrase_changes_the_seed()
    {
        var empty = Bip39Service.Derive(Abandon12, null, null);
        var trezor = Bip39Service.Derive(Abandon12, "TREZOR", null);

        Assert.Equal(Bip39Service.DefaultPath, empty.Path);
        Assert.NotEqual(empty.SeedHex, trezor.SeedHex);
        Assert.NotEqual(empty.AddressP2pkh, trezor.AddressP2pkh);
    }

    [Fact]
    public void Derive_rejects_invalid_mnemonic_and_path()
    {
        Assert.Throws<ArgumentException>(() => Bip39Service.Derive("abandon abandon", "", null));
        Assert.Throws<ArgumentException>(() => Bip39Service.Derive(Abandon12, "", "not-a-path"));
    }
}
