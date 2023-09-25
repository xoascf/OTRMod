using SturmScharf.Compression.Common;
using SturmScharf.Extensions;
using System.ComponentModel;
using System.IO;

namespace SturmScharf;

public abstract class MpqFile : IDisposable, IComparable, IComparable<MpqFile>, IEquatable<MpqFile> {
	private readonly bool _isStreamOwner;
	private readonly ulong? _name;
	private readonly MpqCompressionType _compressionType;
	private MpqLocale _locale;

	private MpqFileFlags _targetFlags;

	internal MpqFile(ulong? hashedName, MpqStream mpqStream, MpqFileFlags flags, MpqLocale locale, bool leaveOpen) {
		_name = hashedName;
		MpqStream = mpqStream ?? throw new ArgumentNullException(nameof(mpqStream));
		_isStreamOwner = !leaveOpen;

		_targetFlags = flags;
		_locale = locale;
		_compressionType = MpqCompressionType.ZLib;
	}

	public ulong Name => _name.GetValueOrDefault(default);

	public MpqStream MpqStream { get; }

	public MpqFileFlags TargetFlags {
		get => _targetFlags;
		set {
			if ((value & MpqFileFlags.Garbage) != 0)
				throw new ArgumentException("Invalid enum.", nameof(value));

			if (value.HasFlag(MpqFileFlags.Encrypted) && EncryptionSeed is null)
				throw new ArgumentException("Cannot set encrypted flag when there is no encryption seed.",
				nameof(value));

			_targetFlags = value;
		}
	}

	public MpqLocale Locale {
		get => _locale;
		set {
			if (!value.IsDefined())
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(MpqLocale));

			_locale = value;
		}
	}

	internal bool IsFilePositionFixed => !MpqStream.CanRead && MpqStream.Flags.IsOffsetEncrypted();

	/// <summary>
	/// Position in the <see cref="HashTable" />.
	/// </summary>
	internal abstract uint HashIndex { get; }

	/// <summary>
	/// Gets a value that, combined with <see cref="HashIndex" />, represents the range of indices where the file may be
	/// placed.
	/// </summary>
	/// <remarks>
	/// This value is always zero for <see cref="MpqKnownFile" />.
	/// For <see cref="MpqUnknownFile" />, it depends on the <see cref="MpqHash" />es preceding this file's hash in the
	/// <see cref="MpqArchive" /> from which the file was retrieved.
	/// </remarks>
	internal abstract uint HashCollisions { get; }

	/// <summary>
	/// Gets the base encryption seed used to encrypt this <see cref="MpqFile" />'s stream.
	/// </summary>
	/// <remarks>
	/// If the <see cref="MpqFile" /> has the <see cref="MpqFileFlags.BlockOffsetAdjustedKey" /> flag, this seed must be
	/// adjusted based on the file's position and size.
	/// </remarks>
	protected abstract uint? EncryptionSeed { get; }

	public int CompareTo(object? value) => MpqFileComparer.Default.Compare(this, value);

	public int CompareTo(MpqFile? mpqFile) => MpqFileComparer.Default.Compare(this, mpqFile);

	/// <inheritdoc />
	public void Dispose() {
		if (_isStreamOwner) {
			MpqStream.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public bool Equals(MpqFile? other) => MpqFileComparer.Default.Equals(this, other);

	public static MpqFile New(Stream? stream, string fileName, bool leaveOpen = false) => New(stream, fileName, MpqLocale.Neutral, leaveOpen);

	public static MpqFile New(Stream? stream, string fileName, MpqLocale locale, bool leaveOpen = false) {
		MpqStream mpqStream =
			stream as MpqStream ?? new MpqStream(stream ?? new MemoryStream(), fileName, leaveOpen);
		return new MpqKnownFile(fileName, mpqStream, mpqStream.Flags, locale, leaveOpen);
	}

	public static MpqFile New(Stream? stream, MpqHash mpqHash, uint hashIndex, uint hashCollisions,
		uint? encryptionSeed = null) {
		MpqStream mpqStream = stream as MpqStream ?? new MpqStream(stream ?? new MemoryStream(), null);
		return new MpqUnknownFile(mpqStream, mpqStream.Flags, mpqHash, hashIndex, hashCollisions, encryptionSeed);
	}

	public static MpqFile New(Stream? stream) {
		MpqStream mpqStream = stream as MpqStream ?? new MpqStream(stream ?? new MemoryStream(), null);
		return new MpqOrphanedFile(mpqStream, mpqStream.Flags);
	}

	public static bool Exists(string path) {
		if (File.Exists(path))
			return true;

		string? subPath = path;
		string fullPath = new FileInfo(path).FullName;
		while (!File.Exists(subPath)) {
			subPath = new FileInfo(subPath).DirectoryName;
			if (subPath is null)
				return false;
		}

		string relativePath =
			fullPath[(subPath.Length + (subPath.EndsWith(@"\", StringComparison.Ordinal) ? 0 : 1))..];

		using MpqArchive archive = MpqArchive.Open(subPath);
		return Exists(archive, relativePath);
	}

	public static bool Exists(MpqArchive archive, string path) {
		if (archive.FileExists(path))
			return true;

		string subPath = path;
		int ignoreLength = new FileInfo(subPath).FullName.Length - path.Length;
		while (!archive.FileExists(subPath)) {
			string directoryName = new FileInfo(subPath).DirectoryName ?? string.Empty;
			if (directoryName.Length <= ignoreLength)
				return false;

			subPath = directoryName[ignoreLength..];
		}

		string relativePath =
			path[(subPath.Length + (subPath.EndsWith(@"\", StringComparison.Ordinal) ? 0 : 1))..];

		using MpqStream subArchiveStream = archive.OpenFile(subPath);
		using MpqArchive subArchive = MpqArchive.Open(subArchiveStream);
		return Exists(subArchive, relativePath);
	}

	/// <exception cref="FileNotFoundException"></exception>
	public static Stream OpenRead(string path) {
		if (File.Exists(path))
			return File.OpenRead(path);

		string? subPath = path;
		string fullPath = new FileInfo(path).FullName;
		while (!File.Exists(subPath)) {
			subPath = new FileInfo(subPath).DirectoryName;
			if (subPath is null)
				throw new FileNotFoundException($"File not found: {path}");
		}

		string relativePath =
			fullPath[(subPath.Length + (subPath.EndsWith(@"\", StringComparison.Ordinal) ? 0 : 1))..];

		using MpqArchive archive = MpqArchive.Open(subPath);
		return OpenRead(archive, relativePath);
	}

	/// <exception cref="FileNotFoundException"></exception>
	public static Stream OpenRead(MpqArchive archive, string path) {
		static Stream GetArchiveFileStream(MpqArchive archive, string filePath) {
			using MpqStream mpqStream = archive.OpenFile(filePath);
			using MemoryStream memoryStream = new((int)mpqStream.Length);

			mpqStream.CopyTo(memoryStream);

			return new MemoryStream(memoryStream.ToArray(), false);
		}

		if (archive.FileExists(path))
			return GetArchiveFileStream(archive, path);

		string subPath = path;
		int ignoreLength = new FileInfo(subPath).FullName.Length - path.Length;
		while (!archive.FileExists(subPath)) {
			string directoryName = new FileInfo(subPath).DirectoryName ?? string.Empty;
			if (directoryName.Length <= ignoreLength)
				throw new FileNotFoundException($"File not found: {path}");

			subPath = directoryName[ignoreLength..];
		}

		string relativePath =
			path[(subPath.Length + (subPath.EndsWith(@"\", StringComparison.Ordinal) ? 0 : 1))..];

		using MpqStream subArchiveStream = archive.OpenFile(subPath);
		using MpqArchive subArchive = MpqArchive.Open(subArchiveStream);
		return GetArchiveFileStream(subArchive, relativePath);
	}

	public override int GetHashCode() => HashCode.Combine(_name, _locale);

	internal void AddToArchive(MpqArchive mpqArchive, uint index, out MpqEntry mpqEntry, out MpqHash mpqHash) {
		uint headerOffset = mpqArchive.HeaderOffset;
		uint absoluteFileOffset = (uint)mpqArchive.BaseStream.Position;
		uint relativeFileOffset = absoluteFileOffset - headerOffset;

		bool mustChangePosition = _targetFlags.IsOffsetEncrypted() && MpqStream.FilePosition != absoluteFileOffset;
		if (_targetFlags == MpqStream.Flags && mpqArchive.BlockSize == MpqStream.BlockSize && !mustChangePosition) {
			MpqStream.CopyBaseStreamTo(mpqArchive.BaseStream);
			GetTableEntries(mpqArchive, index, relativeFileOffset, MpqStream.CompressedSize, MpqStream.FileSize,
				out mpqEntry, out mpqHash);
		}
		else {
			if (!MpqStream.CanRead)
				throw new InvalidOperationException(
				"Unable to re-encode the mpq file, because its stream cannot be read.");

			using Stream newStream = MpqStream.Transform(_targetFlags, _compressionType, relativeFileOffset,
				mpqArchive.BlockSize);
			newStream.CopyTo(mpqArchive.BaseStream);
			GetTableEntries(mpqArchive, index, relativeFileOffset, (uint)newStream.Length, MpqStream.FileSize,
				out mpqEntry, out mpqHash);
		}
	}

	protected abstract void GetTableEntries(MpqArchive mpqArchive, uint index, uint relativeFileOffset,
		uint compressedSize, uint fileSize, out MpqEntry mpqEntry, out MpqHash mpqHash);

	public override bool Equals(object? obj) => Equals(obj as MpqFile);
}