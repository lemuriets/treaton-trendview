using System.Reflection;

namespace LogDecoder.Helpers;

public static class AppVersionProvider
{
    public static string GetProductVersion()
    {
        return Assembly.GetEntryAssembly()?
           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
           .InformationalVersion
           ?? "unknown";
    }
}