namespace LogDecoder.Helpers;

public static class AppPaths
{
    /// <summary>
    /// Directory containing the running executable. Unlike <see cref="AppContext.BaseDirectory"/>,
    /// this resolves to the real <c>.exe</c> location even when published as a single file that
    /// self-extracts to a temp folder.
    /// </summary>
    public static string BaseDirectory
    {
        get
        {
            var exePath = Environment.ProcessPath;
            var dir = string.IsNullOrEmpty(exePath) ? null : Path.GetDirectoryName(exePath);
            return string.IsNullOrEmpty(dir) ? AppContext.BaseDirectory : dir;
        }
    }
}
