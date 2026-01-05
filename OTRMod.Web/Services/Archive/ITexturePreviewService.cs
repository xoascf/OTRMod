using Microsoft.JSInterop;

namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Service for generating image previews using native C# decoding.
/// Uses Blob URLs for memory efficiency.
/// </summary>
public interface IImagePreviewService : IAsyncDisposable {
	/// <summary>
	/// Creates a PNG Blob URL from a texture resource.
	/// </summary>
	ValueTask<string?> CreatePreviewAsync(OTRMod.Z.Texture texture);

	/// <summary>
	/// Creates a JPEG Blob URL from a background resource.
	/// </summary>
	ValueTask<string?> CreatePreviewAsync(OTRMod.Z.Background background);

	/// <summary>
	/// Creates a PNG byte array from a texture resource (for download).
	/// </summary>
	ValueTask<byte[]?> CreatePngBytesAsync(OTRMod.Z.Texture texture);

	/// <summary>
	/// Gets the JPEG bytes from a background resource (for download).
	/// </summary>
	ValueTask<byte[]?> GetJpegBytesAsync(OTRMod.Z.Background background);

	/// <summary>
	/// Revokes a Blob URL to free memory.
	/// </summary>
	ValueTask RevokeBlobUrlAsync(string? url);
}

/// <summary>
/// Implementation using native C# decoding + JS Blob URLs.
/// </summary>
public sealed class ImagePreviewService : IImagePreviewService {
	private readonly IJSRuntime _js;
	private IJSObjectReference? _module;
	private bool _disposed;

	public ImagePreviewService(IJSRuntime js) => _js = js;

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

	public async ValueTask<string?> CreatePreviewAsync(OTRMod.Z.Background background) {
		try {
			var jpegBytes = await GetJpegBytesAsync(background);
			if (jpegBytes == null) return null;

			var module = await GetModuleAsync();
			return await module.InvokeAsync<string?>("createBlobUrl", jpegBytes, "image/jpeg");
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

	public ValueTask<byte[]?> GetJpegBytesAsync(OTRMod.Z.Background background) {
		try {
			// Background resources contain raw JPEG data, just trim at EOI marker
			var jpegBytes = background.GetJpegTrimmed();
			return ValueTask.FromResult<byte[]?>(jpegBytes.Length > 0 ? jpegBytes : null);
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