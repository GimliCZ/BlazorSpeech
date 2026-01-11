namespace BlazorSpeech.Options;

public sealed class BlazorSpeechOptions
{
    /// <summary>
    ///     Custom JavaScript module path. If null, auto-detects from assembly name.
    ///     Example: "./_content/MyCustomName/cleanspeech.js"
    /// </summary>
    public string? CustomJavaScriptPath { get; set; }

    /// <summary>
    ///     JavaScript file name (default: cleanspeech.js)
    /// </summary>
    public string JavaScriptFileName { get; set; } = "cleanspeech.min.js";
}