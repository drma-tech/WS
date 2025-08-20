﻿using System.Text;
using System.Text.RegularExpressions;

namespace WS.WEB.Core.Helper;

public static partial class StringHelper
{
    public static string Format(this string format, object? arg0)
    {
        return string.Format(format, arg0);
    }

    public static string RemoveSpecialCharacters(this string str, char[]? customExceptions = null, char? replace = null)
    {
        return RemoveSpecialCharacters(str.AsSpan(), customExceptions, replace).ToString();
    }

    public static ReadOnlySpan<char> RemoveSpecialCharacters(this ReadOnlySpan<char> str,
        char[]? customExceptions = null, char? replace = null)
    {
        Span<char> buffer = new char[str.Length];
        var idx = 0;
        char[] exceptions = ['-'];

        if (customExceptions != null) exceptions = exceptions.Union(customExceptions).ToArray();

        foreach (var c in str)
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || Array.Exists(exceptions, match => match == c))
            {
                buffer[idx] = c;
                idx++;
            }
            else if (replace != null)
            {
                buffer[idx] = replace.Value;
                idx++;
            }

        return buffer[..idx];
    }

    [GeneratedRegex(@"\p{Mn}", RegexOptions.Compiled)]
    private static partial Regex DiacriticsRegex();

    public static string RemoveDiacritics(this string Text)
    {
        return DiacriticsRegex().Replace(Text.Normalize(NormalizationForm.FormD), string.Empty);
    }

    public static string? ToSlug(this string? str)
    {
        if (str == null) return null;

        str = str.ToLowerInvariant();
        str = str.RemoveDiacritics();
        str = str.RemoveSpecialCharacters();

        str = Regex.Replace(str, @"\s+", "-", RegexOptions.NonBacktracking); // Replace spaces with hyphens
        str = Regex.Replace(str, @"-+", "-", RegexOptions.NonBacktracking); // Replace multiple hyphens with a single one
        str = str.Trim('-'); // Trim leading and trailing hyphens

        return str;
    }
}