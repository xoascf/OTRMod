using BlazorDownloadFile;
using Blazored.LocalStorage;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OTRMod.Web;
using OTRMod.Web.Services;
using OTRMod.Web.Services.Archive;
using OTRMod.Web.Services.Generation;
using System.Globalization;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddPWAUpdater();
builder.Services.AddLocalization();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazorDownloadFile();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CultureService>();
builder.Services.AddScoped<SettingsService>();

// Generation state management (SOLID)
builder.Services.AddScoped<IGenerationStateManager, GenerationStateManager>();
builder.Services.AddScoped<IGenerationStatusDisplay, GenerationStatusDisplay>();

// Archive exploration
builder.Services.AddScoped<IArchiveExplorer, ArchiveExplorer>();
builder.Services.AddSingleton<IResourceAnalyzer, ResourceAnalyzer>();
builder.Services.AddScoped<IImagePreviewService, ImagePreviewService>();
builder.Services.AddSingleton<IFileSizeFormatter, FileSizeFormatter>();

WebAssemblyHost host = builder.Build();

// Set culture from LocalStorage
var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
var savedCulture = await localStorage.GetItemAsStringAsync(CultureService.CULTURE_KEY);

if (!string.IsNullOrEmpty(savedCulture)) {
	var culture = new CultureInfo(savedCulture);
	CultureInfo.DefaultThreadCurrentCulture = culture;
	CultureInfo.DefaultThreadCurrentUICulture = culture;
}

await host.RunAsync();