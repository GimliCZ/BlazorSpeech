using BlazorSpeech.Models;
using BlazorSpeech.Options;

namespace BlazorSpeech.Interfaces;

/// <summary>
///     Core speech synthesis abstraction - minimal, performant, cancellable
/// </summary>
public interface ISpeechSynthesizer : IAsyncDisposable
{
    /// <summary>
    ///     Speak text with specified options. Cancellable, non-blocking.
    /// </summary>
    ValueTask SpeakAsync(string text, SpeechOptions? options = null, CancellationToken ct = default);

    /// <summary>
    ///     Stop current speech immediately. Safe to call multiple times.
    /// </summary>
    ValueTask StopAsync(CancellationToken ct = default);

    /// <summary>
    ///     Check if currently speaking (best effort, may not be 100% accurate in browser mode)
    /// </summary>
    ValueTask<bool> IsSpeakingAsync(CancellationToken ct = default);

    /// <summary>
    ///     Get available voices (cached after first call)
    /// </summary>
    ValueTask<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken ct = default);
}