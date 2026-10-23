# Bip39Net（composehk .NET 8 演示）

这是 [iancoleman/bip39](https://github.com/iancoleman/bip39) 的一个小型 C# 端口，只覆盖学习用的三条命令。英文词表、校验和、PBKDF2 seed 和 BIP32 派生都交给 [NBitcoin](https://www.nuget.org/packages/NBitcoin/7.0.50)，本仓库不手写椭圆曲线。原来的网页工具（`src/`）没有改。

每次运行都会先打印安全声明：仅供学习/演示，勿用于真实资金；勿在联网或不可信环境输入真实助记词。

## 要求

- .NET SDK 8（`net8.0`）

## 构建与测试

在仓库根目录：

```bash
dotnet build dotnet/Bip39Net/Bip39Net.sln
dotnet test dotnet/Bip39Net/Bip39Net.sln
```

## 演示

在 CLI 项目目录里可以直接 `dotnet run -- <command>`：

```bash
cd dotnet/Bip39Net/src/Bip39Net.Cli

dotnet run -- generate
dotnet run -- generate --words 24
dotnet run -- validate abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about
dotnet run -- derive abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about --passphrase TREZOR --path "m/44'/0'/0'/0/0"
```

在仓库根目录用 `--project`（`--` 后面才是本工具的参数）：

```bash
dotnet run --project dotnet/Bip39Net/src/Bip39Net.Cli -- generate --words 12
dotnet run --project dotnet/Bip39Net/src/Bip39Net.Cli -- validate abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about
dotnet run --project dotnet/Bip39Net/src/Bip39Net.Cli -- derive abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about --path "m/44'/0'/0'/0/0"
```

## 命令

| 命令 | 作用 | 默认 |
| --- | --- | --- |
| `generate [--words N]` | 生成英文 BIP39 助记词 | 12 词。`N` 只能是 12、15、18、21、24 |
| `validate <words...>` | 检查词数、英文词表和校验和 | |
| `derive <words...> [--passphrase TEXT] [--path PATH]` | 打印 seed hex，以及该 BIP32 路径上的 xprv / xpub、P2PKH 与 P2WPKH 地址预览 | 口令为空字符串；路径 `m/44'/0'/0'/0/0`（BIP44 比特币第一个收款地址） |

`derive` 打印的 xprv/xpub 是 `--path` 那一层的扩展密钥，不是账户层（`m/44'/0'/0'`）的 xpub。地址是这一把密钥的传统地址和原生隔离见证地址。

退出码：`0` 成功，`1` 助记词或路径无效，`2` 用法错误。

## 已知向量

BIP39 官方英文向量（口令 `TREZOR`）里，12 个 `abandon ... about` 的 seed 为：

```text
c55257c360c07c72029aebc1b53c05ed0362ada38ead3e3e9efa3708e53495531f09a6987599d18264c1e1c92f2cf141630c7a3c4ab7c81b2f001698e7463b04
```

同一句助记词、空口令、路径 `m/44'/0'/0'/0/0` 的传统地址是 `1LqBGSKuX5yYUonjxT5qGfpUsXKYYWeabA`。这是公开测试短语，不要往这个地址打币。

## 布局

```text
dotnet/Bip39Net/Bip39Net.sln
dotnet/Bip39Net/src/Bip39Net.Core/    # 生成、校验、派生
dotnet/Bip39Net/src/Bip39Net.Cli/     # 控制台入口
dotnet/Bip39Net/tests/Bip39Net.Tests/ # 官方向量与词数测试
```
