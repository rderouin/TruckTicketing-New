using System.IO;
using System.Reflection;

namespace SE.Integrations.Api.Tests;

public static class TestTools
{
    public static string GetResourceAsString(this Assembly assembly, string fileName, params string[] folders)
    {
        // get the full resource path
        var assemblyName = assembly.GetName().Name;
        var directory = string.Join(".", folders);
        var fullPath = $@"{assemblyName}.{directory}.{fileName}";

        // get the resource stream
        using var stream = assembly.GetManifestResourceStream(fullPath);
        if (stream == null)
        {
            return null;
        }

        // read contents as string
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
