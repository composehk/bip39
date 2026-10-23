namespace Bip39Net.Cli;

public static class Program
{
    public static int Main(string[] args) =>
        Bip39Net.Bip39Cli.Run(args, Console.Out, Console.Error);
}
