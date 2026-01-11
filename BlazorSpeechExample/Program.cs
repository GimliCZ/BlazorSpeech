using BlazorSpeech.Extensions;
using CleanSpeech.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => { options.DetailedErrors = true; })
    .AddHubOptions(o =>
    {
        // Increase maximum message size to 2 MB (adjust as needed)
        o.MaximumReceiveMessageSize = 2 * 1024 * 1024;
    });

builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });


// Register BlazorSpeechExample - clean, minimal, performant
builder.Services.AddBlazorSpeech();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseStaticFiles();

app.Run();