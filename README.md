# BlazorSpeech
<p align="center">
  <a href="https://www.nuget.org/packages/BlazorSpeech/1.0.1" target="_blank">
    <img src="https://img.shields.io/nuget/v/BlazorSpeech.svg?label=NuGet%20v" alt="NuGet version" style="max-height:300px;" />
    <img src="https://img.shields.io/nuget/dt/BlazorSpeech.svg?label=Downloads" alt="NuGet downloads" style="max-height:300px;" />
  </a>
  <img src="https://img.shields.io/badge/Platform-.NET%2010-orange.svg" style="max-height:300px;" alt=".NET 10" />
  <img src="https://img.shields.io/github/license/GimliCZ/BlazorSpeech" alt="License" />
  <br />
  <img src="https://img.shields.io/github/issues/GimliCZ/BlazorSpeech" alt="Issues" />
  <img src="https://img.shields.io/github/stars/GimliCZ/BlazorSpeech" alt="Stars" />
  <img src="https://img.shields.io/github/forks/GimliCZ/BlazorSpeech" alt="Forks" />
  <img src="https://img.shields.io/github/last-commit/GimliCZ/BlazorSpeech" alt="Last Commit" />
</p>

**BlazorSpeech** is a lightweight Blazor library for easy and reliable speech synthesis in web applications. It
provides simple JavaScript interop for working with browser `SpeechSynthesis` APIs and managing available voices.

---

## Features

- Automatic detection of available speech voices
- One-time initialization with `voiceReady` promise
- Simple integration with Blazor projects
- Includes `cleanspeech.js` for easy JS interop

---
## Installation

Install via NuGet:
```
dotnet add package BlazorSpeech
```
## Quick Start
### Register
```cs
builder.Services.AddCleanSpeech();
```
### Inject to page
```cs
@inject ISpeechSynthesizer Speech
```
### Speak
```cs
 private async Task SpeakAsync()
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            errorMessage = null;
            isSpeaking = true;
            StateHasChanged();

            var options = new SpeechOptions
            {
                VoiceName = string.IsNullOrWhiteSpace(selectedVoice) ? null : selectedVoice,
                Rate = rate,
                Pitch = pitch,
                Volume = volume
            };

            await Speech.SpeakAsync(text, options);

            // Small delay to let speech start
            await Task.Delay(100);

            // Check if still speaking
            isSpeaking = await Speech.IsSpeakingAsync();
            StateHasChanged();

            // Poll until done
            while (isSpeaking)
            {
                await Task.Delay(500);
                isSpeaking = await Speech.IsSpeakingAsync();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
            isSpeaking = false;
        }
        finally
        {
            StateHasChanged();
        }
    }
```

