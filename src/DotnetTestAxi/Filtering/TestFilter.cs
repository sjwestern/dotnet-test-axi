namespace DotnetTestAxi.Filtering;

/// <summary>A filter value split into its text and optional leading/trailing wildcard.</summary>
internal readonly record struct Pattern(string Text, bool LeadingWildcard, bool TrailingWildcard)
{
    public static bool TryParse(string value, out Pattern pattern, out string error)
    {
        pattern = default;
        var leading = value.StartsWith('*');
        var trailing = value.Length > 1 && value.EndsWith('*');
        var text = value[(leading ? 1 : 0)..(value.Length - (trailing ? 1 : 0))];
        if (text.Length == 0)
        {
            error = "is empty";
            return false;
        }

        if (text.Contains('*'))
        {
            error = "may use * only at the start or end";
            return false;
        }

        pattern = new Pattern(text, leading, trailing);
        error = "";
        return true;
    }
}

/// <summary>Per-framework test filter, using xUnit v3 filter semantics (ADR-0001).</summary>
internal sealed record TestFilter(Pattern? Namespace, Pattern? Class, Pattern? Method)
{
    public const string ValidForms =
        "valid forms: --class MyApp.Tests.MyClass, --method MyApp.Tests.MyClass.MyMethod, --method '*MyMethod', --namespace 'MyApp.Tests*'";

    public static TestFilter Empty { get; } = new(null, null, null);

    public bool IsEmpty => Namespace is null && Class is null && Method is null;

    public static bool TryCreate(Args args, out TestFilter filter, out string error)
    {
        filter = Empty;
        if (!TryOne("--namespace", args.Namespace, out var ns, out error)
            || !TryOne("--class", args.Class, out var cls, out error)
            || !TryOne("--method", args.Method, out var method, out error))
        {
            return false;
        }

        if (method is { LeadingWildcard: false, Text: var t } && !t.Contains('.'))
        {
            error = $"--method '{args.Method}' must be qualified (Namespace.Class.Method) or start with *";
            return false;
        }

        filter = new TestFilter(ns, cls, method);
        error = "";
        return true;
    }

    private static bool TryOne(string flag, string? value, out Pattern? pattern, out string error)
    {
        pattern = null;
        error = "";
        if (value is null)
        {
            return true;
        }

        if (!Pattern.TryParse(value, out var p, out var why))
        {
            error = $"{flag} '{value}' {why}";
            return false;
        }

        pattern = p;
        return true;
    }
}
