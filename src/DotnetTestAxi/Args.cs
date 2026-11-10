namespace DotnetTestAxi;

internal sealed class Args
{
    public string? Target { get; private set; }
    public string? Class { get; private set; }
    public string? Method { get; private set; }
    public string? Namespace { get; private set; }
    public string? FromLog { get; private set; }
    public bool Full { get; private set; }
    public bool NoBuild { get; private set; }
    public bool Help { get; private set; }
    public bool Version { get; private set; }
    public List<string> Passthrough { get; } = [];
    public string? Error { get; private set; }

    public const string ValidFlags = "--class, --method, --namespace, --no-build, --full, --from-log, --help, --version";

    public static Args Parse(string[] argv)
    {
        var a = new Args();
        for (var i = 0; i < argv.Length; i++)
        {
            var arg = argv[i];
            switch (arg)
            {
                case "--":
                    a.Passthrough.AddRange(argv[(i + 1)..]);
                    return a;
                case "-h" or "--help": a.Help = true; break;
                case "-v" or "--version": a.Version = true; break;
                case "--full": a.Full = true; break;
                case "--no-build": a.NoBuild = true; break;
                case "--class": a.Class = Value(argv, ref i, a); break;
                case "--method": a.Method = Value(argv, ref i, a); break;
                case "--namespace": a.Namespace = Value(argv, ref i, a); break;
                case "--from-log": a.FromLog = Value(argv, ref i, a); break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        a.Error = $"unknown flag {arg}";
                    }
                    else if (a.Target is null)
                    {
                        a.Target = arg;
                    }
                    else
                    {
                        a.Error = $"unexpected argument {arg}";
                    }
                    break;
            }
        }

        return a;
    }

    private static string? Value(string[] argv, ref int i, Args a)
    {
        if (i + 1 >= argv.Length)
        {
            a.Error = $"{argv[i]} needs a value";
            return null;
        }

        return argv[++i];
    }
}
