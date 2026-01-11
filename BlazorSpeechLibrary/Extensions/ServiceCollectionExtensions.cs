using CleanSpeechLibrary.Interfaces;
using CleanSpeechLibrary.Options;
using CleanSpeechLibrary.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CleanSpeechLibrary.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register BlazorSpeechExample services (browser-based by default)
    /// </summary>
    public static IServiceCollection AddCleanSpeech(
        this IServiceCollection services,
        Action<CleanSpeechOptions>? configureOptions = null)
    {
        services.AddTransient<ISpeechSynthesizer, BrowserSpeechSynthesizer>();
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        
        return services;
    }
}