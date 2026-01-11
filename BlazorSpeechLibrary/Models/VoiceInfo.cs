using CleanSpeechLibrary.DTOs;

namespace CleanSpeechLibrary.Models;

/// <summary>
/// Immutable voice metadata exposed to consumers
/// </summary>
public sealed record VoiceInfo
{
    public string Name { get; }
    public string LanguageTag { get; }
    public string VoiceUri { get; }
    public bool IsDefault { get; }
    public bool IsLocalService { get; }
    internal VoiceInfo(VoiceDto dto)
    {
        Name = dto.Name;
        LanguageTag = dto.Lang;
        VoiceUri = dto.VoiceUri;
        IsDefault = dto.IsDefault;
        IsLocalService = dto.IsLocalService;
    }

    public override string ToString()
        => $"{Name} [{LanguageTag}]{(IsDefault ? " (default)" : "")}";
}