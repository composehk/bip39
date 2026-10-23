# composehk .NET port demo

This fork (composehk) adds a small **.NET 8** console beside the original iancoleman/bip39 browser tool. The HTML and JavaScript sources are unchanged.

- Code: [`dotnet/Bip39Net`](dotnet/Bip39Net)
- How to run: [`dotnet/README.md`](dotnet/README.md)

The demo generates a BIP39 mnemonic, checks it against the English wordlist and checksum, and previews a seed plus a BIP32 key. Checksum, seed, and curve math come from [NBitcoin](https://github.com/MetacoSA/NBitcoin). It is for learning and demonstration only. Do not use it with real funds, and do not type a real mnemonic on a networked or untrusted machine.

仅供学习/演示，勿用于真实资金；勿在联网/不可信环境输入真实助记词。
