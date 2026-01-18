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

<img width="358" height="861" alt="image" src="https://github.com/user-attachments/assets/bb787c02-5c53-4a10-9a6f-99531929fb3a" />


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
builder.Services.AddBlazorSpeech();
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
            var options = new SpeechOptions
            {
                VoiceName = string.IsNullOrWhiteSpace(selectedVoice) ? null : selectedVoice,
                Rate = rate,
                Pitch = pitch,
                Volume = volume,
                Queue = false
            };

            await Speech.SpeakAsync(text, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error {0}", ex.Message);
        }
        finally
        {
            StateHasChanged();
        }
    }
```
### Subscribe on Speech Changes
```cs
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadVoicesAsync();
            Speech.SpeakingStateChanged += OnSpeakingStateChanged;
            isSpeaking = await Speech.IsSpeakingOrPendingSpeechAsync();
        }
        await base.OnAfterRenderAsync(firstRender);
    }
```
### React to change of voice state
```cs
    private async Task OnSpeakingStateChanged(bool speaking)
    {
        isSpeaking = speaking;

        // Ensure UI update happens
        await InvokeAsync(StateHasChanged);
    }
```
### Stop Speaking
```cs
 private async Task StopAsync()
    {
        try
        {
            await Speech.CancelAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error {0}", ex.Message);
        }
        finally
        {
            StateHasChanged();
        }
    }
```
### Cleanup after use
```cs
    public void Dispose()
    {
        Speech.SpeakingStateChanged -= OnSpeakingStateChanged;
    }
```
### Warning
For Firefox users its recommended to increase message size of RSignal to 2MB

