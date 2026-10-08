using System.Globalization;
using System.Text.RegularExpressions;

namespace InterviewPrepBot.Core.Questions;

public static partial class QuestionText
{
    /// <summary>Trim, collapse whitespace, case-fold. Shared by parser, single add and the unique index.</summary>
    public static string Normalize(string text)
    {
        var collapsed = WhitespaceRun().Replace(text.Trim(), " ");
        return collapsed.ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
