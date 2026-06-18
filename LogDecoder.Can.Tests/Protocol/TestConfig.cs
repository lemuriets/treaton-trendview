namespace LogDecoder.Can.Tests.Protocol;

/// <summary>
/// Locates the repository-root <c>config/</c> folder (no longer copied into bin)
/// by walking up from the test directory.
/// </summary>
public static class TestConfig
{
    public static string Root()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "config");
            if (Directory.Exists(candidate) && Directory.GetDirectories(candidate).Length > 0)
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Root config/ folder not found by walking up from the test directory.");
    }
}
