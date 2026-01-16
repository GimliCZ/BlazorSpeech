using BlazorSpeech.DTOs;
using BlazorSpeech.Interfaces;
using BlazorSpeech.Models;
using BlazorSpeech.Options;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorSpeech.Services;

/// <summary>
///     Browser-based speech synthesis - minimal JS interop, zero events crossing boundary
/// </summary>
public sealed class BrowserSpeechSynthesizer : ISpeechSynthesizer
{
    private static readonly string AssemblyName =
        typeof(BrowserSpeechSynthesizer).Assembly.GetName().Name ?? "BlazorSpeechLibrary";

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private IReadOnlyList<VoiceInfo>? _cachedVoices;
    private bool _disposed;
    private DotNetObjectReference<BrowserSpeechSynthesizer>? _objectReference;
    private Func<bool, Task>? _speakingStateChangedHandler;
    private IJSObjectReference? _subscriptionHandle;

    public BrowserSpeechSynthesizer(IJSRuntime jsRuntime, IOptions<BlazorSpeechOptions>? options = null)
    {
        var opts = options?.Value ?? new BlazorSpeechOptions();

        // Use custom path if provided, otherwise auto-detect
        var jsPath = opts.CustomJavaScriptPath
                     ?? $"./_content/{AssemblyName}/BlazorSpeechLibrary/{opts.JavaScriptFileName}";

        _moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>("import", jsPath).AsTask());
    }
    /// <summary>
    ///     Event fired when speaking state changes
    /// </summary>
    public event Func<bool, Task>? SpeakingStateChanged
    {
        add
        {
            var hadHandlers = _speakingStateChangedHandler != null;
            _speakingStateChangedHandler += value;

            // Subscribe to JS events when first handler is added
            if (!hadHandlers && _speakingStateChangedHandler != null)
                _ = SubscribeToSpeakingStateAsync();
        }
        remove
        {
            _speakingStateChangedHandler -= value;

            // Unsubscribe from JS events when last handler is removed
            if (_speakingStateChangedHandler == null)
                _ = UnsubscribeFromSpeakingStateAsync();
        }
    }


    public async ValueTask SpeakAsync(string text, SpeechOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(text))
            return;

        options ??= SpeechOptions.Default;

        // Sanitize input to prevent injection attacks
        var sanitized = SanitizeText(text);

        var module = await _moduleTask.Value;

        // Fire-and-forget - JS owns execution completely
        // No events, no callbacks, no sync context crossing
        await module.InvokeVoidAsync("speak", ct, new
        {
            text = sanitized,
            voice = options.VoiceName,
            rate = Math.Clamp(options.Rate, 0.1f, 10.0f),
            pitch = Math.Clamp(options.Pitch, 0.0f, 2.0f),
            volume = Math.Clamp(options.Volume, 0.0f, 1.0f),
            lang = options.Language,
            queue =  options.Queue,
        });
    }

    public async ValueTask CancelAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("cancel", ct);
    }
    
    public async ValueTask PauseAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("pause", ct);
    }
    
    public async ValueTask ResumeAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("resume", ct);
    }


    public async ValueTask<bool> IsSpeakingOrPendingSpeechAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("isSpeaking", ct);
    }
    
    public async ValueTask<bool> IsPausedAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("getPaused", ct);
    }

    public async ValueTask<bool> IsPendingAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("getPending", ct);
    }

    public async ValueTask<bool> IsSpeakingAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("getSpeaking", ct);
    }

    public async ValueTask<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        // Return cached voices if available
        if (_cachedVoices is not null && _cachedVoices.Count > 0) return _cachedVoices;

        try
        {
            var module = await _moduleTask.Value;

            // The JS getVoices() now properly waits for voices to load
            var voices = await module.InvokeAsync<VoiceDto[]?>("getVoices", ct);

            if (voices != null && voices.Length > 0)
                _cachedVoices = voices?
                    .Select(dto => new VoiceInfo(dto))
                    .OrderByDescending(v => v.IsDefault)
                    .ThenByDescending(v => v.IsLocalService)
                    .ThenBy(v => v.LanguageTag)
                    .ToList()
                    .AsReadOnly();
            else
                // Return empty list if no voices available
                _cachedVoices = Array.Empty<VoiceInfo>();

            return _cachedVoices!;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - return empty list
            Console.WriteLine($"CLEAN SPEECH LIBRARY: Error loading voices: {ex.Message}");
            _cachedVoices = Array.Empty<VoiceInfo>();
            return _cachedVoices;
        }
    }
    
    /// <summary>
    ///     JS callback - invoked when speaking state changes
    /// </summary>
    [JSInvokable]
    public async Task OnSpeakingStateChanged(bool isSpeaking)
    {
        if (_disposed)
            return;

        var handler = _speakingStateChangedHandler;
        if (handler != null)
            await handler.Invoke(isSpeaking);
    }

    private async Task SubscribeToSpeakingStateAsync()
    {
        if (_disposed || !_moduleTask.IsValueCreated)
            return;

        // Already subscribed
        if (_subscriptionHandle != null)
            return;

        try
        {
            _objectReference ??= DotNetObjectReference.Create(this);
            var module = await _moduleTask.Value;

            // Register callback with JS and store the handle for cleanup
            _subscriptionHandle = await module.InvokeAsync<IJSObjectReference>(
                "onSpeakingStateChanged", 
                _objectReference, 
                nameof(OnSpeakingStateChanged));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLEAN SPEECH LIBRARY: Error subscribing to speaking state: {ex.Message}");
        }
    }

    private async Task UnsubscribeFromSpeakingStateAsync()
    {
        if (_disposed || !_moduleTask.IsValueCreated || _subscriptionHandle == null)
            return;

        try
        {
            var module = await _moduleTask.Value;
            
            // Unsubscribe using the stored handle
            try
            {
                await module.InvokeVoidAsync("unsubscribeFromSpeakingState", _subscriptionHandle);
                await _subscriptionHandle.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected – NOOP
            }

            _subscriptionHandle = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLEAN SPEECH LIBRARY: Error unsubscribing from speaking state: {ex.Message}");
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unsubscribe from JS events first
        await UnsubscribeFromSpeakingStateAsync();

        // Clear event handlers
        _speakingStateChangedHandler = null;

        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("dispose");
            await module.DisposeAsync();
        }

        if (_subscriptionHandle != null)
        {
            await _subscriptionHandle.DisposeAsync();
        }
        // Dispose DotNetObjectReference
        _objectReference?.Dispose();
        _objectReference = null;
    }


    private static string SanitizeText(string text)
    {
        // Remove potential SSML injection attempts
        return text
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Trim();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BrowserSpeechSynthesizer));
    }
}