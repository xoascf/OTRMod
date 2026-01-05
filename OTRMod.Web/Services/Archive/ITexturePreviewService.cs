using Microsoft.JSInterop;

namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Service for generating texture previews using native C# decoding.
/// Uses Blob URLs for memory efficiency.
/// </summary>
public interface ITexturePreviewService : IAsyncDisposable {
	/// <summary>
	/// Creates a PNG Blob URL from a texture resource.
	/// </summary>
	/// <param name="texture">The texture to preview.</param>
	/// <returns>Blob URL for the PNG image, or null on failure.</returns>
	ValueTask<string?> CreatePreviewAsync(OTRMod.Z.Texture texture);

	/// <summary>
	/// Creates a PNG byte array from a texture resource (for download).
	/// </summary>
	ValueTask<byte[]?> CreatePngBytesAsync(OTRMod.Z.Texture texture);

	/// <summary>
	/// Revokes a Blob URL to free memory.
	/// </summary>
	ValueTask RevokeBlobUrlAsync(string? url);
}

/// <summary>
/// Implementation using native C# texture decoding + JS Blob URLs.
/// </summary>
public sealed class TexturePreviewService : ITexturePreviewService {
	private readonly IJSRuntime _js;
	private IJSObjectReference? _module;
	private bool _disposed;

	public TexturePreviewService(IJSRuntime js) => _js = js;

	private async ValueTask<IJSObjectReference> GetModuleAsync() {
		return _module ??= await _js.InvokeAsync<IJSObjectReference>(
			"import", "./js/blob-helper.js");
	}

	public async ValueTask<string?> CreatePreviewAsync(OTRMod.Z.Texture texture) {
		try {
			var pngBytes = await CreatePngBytesAsync(texture);
			if (pngBytes == null) return null;

			var module = await GetModuleAsync();
			return await module.InvokeAsync<string?>("createBlobUrl", pngBytes, "image/png");
		}
		catch {
			return null;
		}
	}

	public ValueTask<byte[]?> CreatePngBytesAsync(OTRMod.Z.Texture texture) {
		try {
			// Use native OTRMod bitmap decoding
			var bitmap = texture.GetBitmap();
			var pngBytes = bitmap.ExportBytes(IronSoftware.Drawing.AnyBitmap.ImageFormat.Png);
			return ValueTask.FromResult<byte[]?>(pngBytes);
		}
		catch {
			return ValueTask.FromResult<byte[]?>(null);
		}
	}

	public async ValueTask RevokeBlobUrlAsync(string? url) {
		if (string.IsNullOrEmpty(url) || !url.StartsWith("blob:"))
			return;

		try {
			var module = await GetModuleAsync();
			await module.InvokeVoidAsync("revokeBlobUrl", url);
		}
		catch {
			// Ignore cleanup errors
		}
	}

	public async ValueTask DisposeAsync() {
		if (_disposed) return;
		_disposed = true;

		if (_module != null) {
			try { await _module.DisposeAsync(); }
			catch { /* ignore */ }
		}
	}
}