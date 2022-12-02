using System.Text;

namespace GithubWorkflowGenerator.Console.Extensions;

internal static class StringExtensions
{
    public static string ToKebabCase(this string s)
    {
        if (s.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            // if current char is already lowercase
            if (char.IsLower(s[i]))
            {
                builder.Append(s[i]);
            }
            // if current char is the first char
            else if (i == 0)
            {
                builder.Append(char.ToLower(s[i]));
            }
            // if current char is a number and the previous is not
            else if (char.IsDigit(s[i]) && !char.IsDigit(s[i - 1]))
            {
                builder.Append('-');
                builder.Append(s[i]);
            }
            // if current char is a number and previous is as well
            else if (char.IsDigit(s[i]))
            {
                builder.Append(s[i]);
            }
            // if current char is upper and previous char is lower
            else if (char.IsLower(s[i - 1]))
            {
                builder.Append('-');
                builder.Append(char.ToLower(s[i]));
            }
            // if current char is upper and next char doesn't exist or is upper
            else if (i + 1 == s.Length || char.IsUpper(s[i + 1]))
            {
                builder.Append(char.ToLower(s[i]));
            }
            // if current char is upper and next char is lower
            else
            {
                builder.Append('-');
                builder.Append(char.ToLower(s[i]));
            }
        }

        return builder.ToString();
    }
}
