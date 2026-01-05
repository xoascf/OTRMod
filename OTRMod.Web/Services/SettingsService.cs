using Blazored.LocalStorage;

namespace OTRMod.Web.Services;

public enum OutputFormat {
	O2R,
	OTR
}

public class SettingsService {
	public const string FORMAT_KEY = "app-output-format";
	private readonly ILocalStorageService _localStorage;

	public event Action? OnSettingsChanged;

	public SettingsService(ILocalStorageService localStorage) {
		_localStorage = localStorage;
	}

	public async Task<OutputFormat> GetOutputFormatAsync() {
		var format = await _localStorage.GetItemAsStringAsync(FORMAT_KEY);
		return format switch {
			"OTR" => OutputFormat.OTR,
			_ => OutputFormat.O2R // Default to O2R
		};
	}

	public async Task SetOutputFormatAsync(OutputFormat format) {
		await _localStorage.SetItemAsStringAsync(FORMAT_KEY, format.ToString());
		OnSettingsChanged?.Invoke();
	}

	public string GetFileExtension(OutputFormat format) => format switch {
		OutputFormat.OTR => ".otr",
		_ => ".o2r"
	};
}