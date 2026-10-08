using System.ComponentModel.DataAnnotations;

namespace InterviewPrepBot.Api;

public sealed class StorageOptions
{
    public const string Section = "Storage";

    [Required] public string Path { get; set; } = "data/interview-prep.db";

    /// <summary>Relative paths resolve against the content root.</summary>
    public static string ResolvePath(string path, string contentRoot) =>
        System.IO.Path.GetFullPath(path, contentRoot);
}

public sealed class SecurityOptions
{
    public const string Section = "Security";

    [MinLength(1)] public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}
