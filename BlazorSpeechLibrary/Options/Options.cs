namespace BlazorSpeech.Options;

/// <summary>
///     Speech configuration - immutable for thread safety
/// </summary>
public sealed record SpeechOptions
{
    public static readonly SpeechOptions Default = new();

    /// <summary>
    ///     Voice to use (null = default system voice)
    /// </summary>
    public string? VoiceName { get; init; }

    /// <summary>
    ///     Speech rate: 0.1 to 10.0 (default: 1.0)
    /// </summary>
    public float Rate { get; init; } = 1.0f;

    /// <summary>
    ///     Pitch: 0.0 to 2.0 (default: 1.0)
    /// </summary>
    public float Pitch { get; init; } = 1.0f;

    /// <summary>
    ///     Volume: 0.0 to 1.0 (default: 1.0)
    /// </summary>
    public float Volume { get; init; } = 1.0f;

    /// <summary>
    ///     Language/locale (BCP 47 tag, e.g. "en-US")
    /// </summary>
    public string? Language { get; init; }
}