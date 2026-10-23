using System.Security.Cryptography;
using NBitcoin;

namespace Bip39Net;

/// <summary>
/// BIP39 generate / validate / derive preview.
/// Checksum, PBKDF2-HMAC-SHA512 seed, and BIP32 keys come from NBitcoin.
/// </summary>
public static class Bip39Service
{
    public const string DefaultPath = "m/44'/0'/0'/0/0";

    public static readonly int[] SupportedWordCounts = { 12, 15, 18, 21, 24 };

    static Bip39Service()
    {
        // Mnemonic generation reads this RNG. Prefer the OS CSPRNG when the
        // library has not installed one yet.
        RandomUtils.Random ??= new RandomNumberGeneratorRandom();
    }

    public static string Generate(int wordCount = 12)
    {
        var count = wordCount switch
        {
            12 => WordCount.Twelve,
            15 => WordCount.Fifteen,
            18 => WordCount.Eighteen,
            21 => WordCount.TwentyOne,
            24 => WordCount.TwentyFour,
            _ => throw new ArgumentOutOfRangeException(
                nameof(wordCount),
                wordCount,
                "Word count must be 12, 15, 18, 21, or 24.")
        };

        return new Mnemonic(Wordlist.English, count).ToString();
    }

    public static ValidationResult Validate(string? mnemonicText)
    {
        if (string.IsNullOrWhiteSpace(mnemonicText))
        {
            return ValidationResult.Invalid("Mnemonic is empty.");
        }

        var words = SplitWords(mnemonicText);
        var normalized = string.Join(' ', words);

        if (Array.IndexOf(SupportedWordCounts, words.Length) < 0)
        {
            return ValidationResult.Invalid(
                $"Word count must be 12, 15, 18, 21, or 24 (got {words.Length}).",
                normalized,
                words.Length);
        }

        var wordlist = Wordlist.English;
        foreach (var word in words)
        {
            if (!wordlist.WordExists(word, out _))
            {
                return ValidationResult.Invalid(
                    $"Word '{word}' is not in the English BIP39 wordlist.",
                    normalized,
                    words.Length);
            }
        }

        var mnemonic = new Mnemonic(normalized, wordlist);
        if (!mnemonic.IsValidChecksum)
        {
            return ValidationResult.Invalid(
                "Checksum does not match the English BIP39 wordlist.",
                normalized,
                words.Length);
        }

        return ValidationResult.Valid(mnemonic.ToString(), words.Length);
    }

    public static DerivationPreview Derive(string mnemonicText, string? passphrase, string? path)
    {
        var validation = Validate(mnemonicText);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Reason ?? "Invalid mnemonic.");
        }

        passphrase ??= "";
        var requestedPath = string.IsNullOrWhiteSpace(path) ? DefaultPath : path.Trim();

        KeyPath keyPath;
        try
        {
            keyPath = new KeyPath(requestedPath);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new ArgumentException($"Invalid BIP32 path '{requestedPath}'.", ex);
        }

        var mnemonic = new Mnemonic(validation.Mnemonic, Wordlist.English);
        var seed = mnemonic.DeriveSeed(passphrase);
        var seedHex = Convert.ToHexString(seed).ToLowerInvariant();
        CryptographicOperations.ZeroMemory(seed);

        var derived = mnemonic.DeriveExtKey(passphrase).Derive(keyPath);
        var pubKey = derived.PrivateKey.PubKey;
        var shownPath = keyPath.ToString();
        if (!shownPath.StartsWith("m/", StringComparison.OrdinalIgnoreCase))
        {
            shownPath = "m/" + shownPath;
        }

        return new DerivationPreview(
            validation.Mnemonic,
            passphrase,
            seedHex,
            shownPath,
            derived.GetWif(Network.Main).ToString(),
            derived.Neuter().GetWif(Network.Main).ToString(),
            pubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString(),
            pubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ToString());
    }

    public static string[] SplitWords(string mnemonicText)
    {
        return mnemonicText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static word => word.ToLowerInvariant())
            .ToArray();
    }
}

public sealed record ValidationResult(bool IsValid, string Mnemonic, int WordCount, string? Reason)
{
    public static ValidationResult Valid(string mnemonic, int wordCount) =>
        new(true, mnemonic, wordCount, null);

    public static ValidationResult Invalid(string reason, string mnemonic = "", int wordCount = 0) =>
        new(false, mnemonic, wordCount, reason);
}

public sealed record DerivationPreview(
    string Mnemonic,
    string Passphrase,
    string SeedHex,
    string Path,
    string Xprv,
    string Xpub,
    string AddressP2pkh,
    string AddressP2wpkh);
