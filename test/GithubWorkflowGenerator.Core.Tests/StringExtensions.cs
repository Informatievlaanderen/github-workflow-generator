using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GithubWorkflowGenerator.Core.Tests;

internal static class StringExtensions
{
    public static string? ExceptCharacters(this string? s, IEnumerable<char> excludedCharacters)
    {
        if (s == null)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (char c in s.Where(c => !excludedCharacters.Contains(c)))
        {
            sb.Append(c);
        }

        return sb.ToString();
    }
}