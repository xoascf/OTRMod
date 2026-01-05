using Ionic.Zip;
using SturmScharf;

namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Service for exploring OTR/O2R archive files.
/// Caches content for fast repeated access.
/// </summary>
public sealed class ArchiveExplorer : IArchiveExplorer {
	private readonly IResourceAnalyzer _analyzer;
	private byte[]? _archiveData;
	private List<ArchiveEntry> _entries = new();
	private readonly Dictionary<string, byte[]> _contentCache = new();
	private readonly Dictionary<string, ResourceInfo?> _metadataCache = new();

	public ArchiveExplorer(IResourceAnalyzer analyzer) {
		_analyzer = analyzer;
	}

	public string? ArchiveName { get; private set; }
	public ArchiveFormat? Format { get; private set; }
	public bool IsLoaded => _archiveData != null;

	public event Action? OnArchiveChanged;

	public async Task LoadAsync(Stream stream, string fileName) {
		Close();

		using var ms = new MemoryStream();
		await stream.CopyToAsync(ms);
		_archiveData = ms.ToArray();

		ArchiveName = fileName;
		Format = fileName.EndsWith(".o2r", StringComparison.OrdinalIgnoreCase)
			? ArchiveFormat.O2R
			: ArchiveFormat.OTR;

		_entries = Format == ArchiveFormat.O2R ? LoadO2R() : LoadOTR();
		OnArchiveChanged?.Invoke();
	}

	public IReadOnlyList<ArchiveEntry> GetEntries() => _entries;

	public IReadOnlyList<ArchiveEntry> GetEntries(string directory) {
		var normalized = Normalize(directory);
		return _entries
			.Where(e => GetParent(e.Path) == normalized)
			.OrderByDescending(e => e.IsDirectory)
			.ThenBy(e => e.Name)
			.ToList();
	}

	public Task<byte[]?> GetContentAsync(string path) {
		if (_archiveData == null) return Task.FromResult<byte[]?>(null);

		var normalizedPath = Normalize(path);

		// Check cache first
		if (_contentCache.TryGetValue(normalizedPath, out var cached))
			return Task.FromResult<byte[]?>(cached);

		var content = Format == ArchiveFormat.O2R
			? GetO2RContent(normalizedPath)
			: GetOTRContent(normalizedPath);

		// Cache the content
		if (content != null)
			_contentCache[normalizedPath] = content;

		return Task.FromResult(content);
	}

	public ResourceInfo? AnalyzeMetadata(string path) {
		var normalizedPath = Normalize(path);

		// Check metadata cache
		if (_metadataCache.TryGetValue(normalizedPath, out var cached))
			return cached;

		// Get content (from cache if available)
		var content = GetContentAsync(normalizedPath).GetAwaiter().GetResult();
		if (content == null) return null;

		var info = _analyzer.AnalyzeMetadata(content);
		_metadataCache[normalizedPath] = info;
		return info;
	}

	public async Task<OTRMod.Z.Texture?> GetTextureAsync(string path) {
		var content = await GetContentAsync(path);
		return content == null ? null : _analyzer.GetTexture(content);
	}

	public void Close() {
		_archiveData = null;
		_entries.Clear();
		_contentCache.Clear();
		_metadataCache.Clear();
		ArchiveName = null;
		Format = null;
		OnArchiveChanged?.Invoke();
	}

	private List<ArchiveEntry> LoadO2R() {
		var entries = new List<ArchiveEntry>();
		var dirs = new HashSet<string>();

		using var stream = new MemoryStream(_archiveData!);
		using var zip = ZipFile.Read(stream);
		foreach (var entry in zip.Where(e => !e.IsDirectory)) {
			var path = Normalize(entry.FileName);
			entries.Add(new(Path.GetFileName(path), path, entry.UncompressedSize, false));
			AddDirs(path, dirs, entries);
		}
		return entries;
	}

	private List<ArchiveEntry> LoadOTR() {
		var entries = new List<ArchiveEntry>();
		var dirs = new HashSet<string>();

		using var stream = new MemoryStream(_archiveData!);
		using var mpq = MpqArchive.Open(stream, true);
		foreach (var file in mpq.GetMpqFiles().OfType<MpqKnownFile>()) {
			if (file.FileName is "(listfile)" or "(attributes)") continue;

			var path = Normalize(file.FileName);
			entries.Add(new(Path.GetFileName(path), path, file.MpqStream.Length, false));
			AddDirs(path, dirs, entries);
		}
		return entries;
	}

	private static void AddDirs(string path, HashSet<string> dirs, List<ArchiveEntry> entries) {
		var current = "";
		foreach (var part in path.Split('/').SkipLast(1)) {
			current = string.IsNullOrEmpty(current) ? part : $"{current}/{part}";
			if (dirs.Add(current))
				entries.Add(new(part, current, 0, true));
		}
	}

	private byte[]? GetO2RContent(string path) {
		using var stream = new MemoryStream(_archiveData!);
		using var zip = ZipFile.Read(stream);
		var entry = zip.FirstOrDefault(e => Normalize(e.FileName) == path);
		if (entry == null) return null;

		using var ms = new MemoryStream();
		entry.Extract(ms);
		return ms.ToArray();
	}

	private byte[]? GetOTRContent(string path) {
		using var stream = new MemoryStream(_archiveData!);
		using var mpq = MpqArchive.Open(stream, true);
		foreach (var file in mpq.GetMpqFiles().OfType<MpqKnownFile>()) {
			if (Normalize(file.FileName) == path) {
				using var ms = new MemoryStream();
				file.MpqStream.CopyTo(ms);
				return ms.ToArray();
			}
		}
		return null;
	}

	private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');
	private static string GetParent(string path) => path.Contains('/') ? path[..path.LastIndexOf('/')] : "";
}