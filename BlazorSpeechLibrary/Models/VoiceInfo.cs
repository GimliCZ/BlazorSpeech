using BlazorSpeech.DTOs;

namespace BlazorSpeech.Models;

/// <summary>
///     Immutable voice metadata exposed to consumers
/// </summary>
public sealed record VoiceInfo
{
    internal VoiceInfo(VoiceDto dto)
    {
        Name = dto.Name;
        LanguageTag = dto.Lang;
        VoiceUri = dto.VoiceUri;
        IsDefault = dto.IsDefault;
        IsLocalService = dto.IsLocalService;
    }

    public string Name { get; }
    public string LanguageTag { get; }
    public string VoiceUri { get; }
    public bool IsDefault { get; }
    public bool IsLocalService { get; }

    public override string ToString()
    {
        return $"{Name} [{LanguageTag}]{(IsDefault ? " (default)" : "")}";
    }
}