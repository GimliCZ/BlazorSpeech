namespace BlazorSpeech.DTOs;

public sealed record VoiceDto(
    string Name,
    string Lang,
    string VoiceUri,
    bool IsDefault,
    bool IsLocalService);