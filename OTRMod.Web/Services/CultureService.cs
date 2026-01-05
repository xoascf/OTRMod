using System.Globalization;
using Blazored.LocalStorage;

namespace OTRMod.Web.Services;

public class CultureService {
	public const string CULTURE_KEY = "app-culture";
	private readonly ILocalStorageService _localStorage;

	public event Action? OnCultureChanged;

	public CultureService(ILocalStorageService localStorage) {
		_localStorage = localStorage;
	}

	public async Task<string> GetSavedCultureAsync() {
		var culture = await _localStorage.GetItemAsStringAsync(CULTURE_KEY);
		return culture ?? "en";
	}

	public async Task SetCultureAsync(string culture) {
		var currentCulture = CultureInfo.CurrentCulture.Name;
		if (currentCulture != culture) {
			await _localStorage.SetItemAsStringAsync(CULTURE_KEY, culture);

			var newCulture = new CultureInfo(culture);
			CultureInfo.DefaultThreadCurrentCulture = newCulture;
			CultureInfo.DefaultThreadCurrentUICulture = newCulture;

			OnCultureChanged?.Invoke();
		}
	}
}