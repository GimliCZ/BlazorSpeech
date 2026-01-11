using BlazorSpeech.Interfaces;
using BlazorSpeech.Options;
using BlazorSpeech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorSpeech.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Register BlazorSpeechExample services (browser-based by default)
    /// </summary>
    public static IServiceCollection AddBlazorSpeech(
        this IServiceCollection services,
        Action<BlazorSpeechOptions>? configureOptions = null)
    {
        services.AddTransient<ISpeechSynthesizer, BrowserSpeechSynthesizer>();

        if (configureOptions != null) services.Configure(configureOptions);

        return services;
    }
}