using System.Reflection;

namespace CleanSpeechLibrary.Resolver;

/// <summary>
/// Diagnostic helper to verify JS path resolution
/// </summary>
public static class PathResolver
{
    /// <summary>
    /// Get the resolved JavaScript path that will be used
    /// </summary>
    public static string GetJavaScriptPath()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "BlazorSpeechExample";
        return $"./_content/{assemblyName}/cleanspeech.js";
    }

    /// <summary>
    /// Get detailed assembly information for debugging
    /// </summary>
    public static string GetAssemblyInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return $"""
                Assembly Name: {assembly.GetName().Name}
                Full Name: {assembly.FullName}
                Location: {assembly.Location}
                Expected JS Path: {GetJavaScriptPath()}
                """;
    }
}