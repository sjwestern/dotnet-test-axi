using System.Text;
using DotnetTestAxi.Filtering;

namespace DotnetTestAxi.Frameworks.Adapters;

/// <summary>Builds a VSTest <c>--filter</c> expression on FullyQualifiedName (Namespace.Class.Method).</summary>
internal static class VSTestExpression
{
    public static IReadOnlyList<string> Build(TestFilter filter)
    {
        var parts = new List<string>();
        if (filter.Namespace is { } ns)
        {
            // A trailing dot keeps "Ns" from matching "NsExtra"; a trailing wildcard drops it.
            parts.Add("FullyQualifiedName~" + Escape(ns.Text) + (ns.TrailingWildcard ? "" : "."));
        }

        if (filter.Class is { } c)
        {
            parts.Add("FullyQualifiedName~" + Escape(c.Text) + (c.TrailingWildcard ? "" : "."));
        }

        if (filter.Method is { } m)
        {
            var op = m.LeadingWildcard || m.TrailingWildcard ? "~" : "=";
            parts.Add("FullyQualifiedName" + op + Escape(m.Text));
        }

        return parts.Count == 0 ? [] : ["--filter", string.Join('&', parts)];
    }

    private static string Escape(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch is '\\' or '(' or ')' or '&' or '|' or '=' or '!' or '~') { sb.Append('\\'); }
            sb.Append(ch);
        }

        return sb.ToString();
    }
}
