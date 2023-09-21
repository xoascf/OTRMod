using SturmScharf.Compression;
using SturmScharf.Compression.Common;
using SturmScharf.Extensions;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace SturmScharf;

/// <summary>
/// A Stream based class for reading a file from an <see cref="MpqArchive" />.
/// </summary>
public class MpqStream : Stream {
	private readonly uint _baseEncryptionSeed;
#if NET40
	private readonly uint[] _blockPositions = new uint[0];
#else
	private readonly uint[] _blockPositions = Array.Empty<uint>();
#endif
	private readonly bool _canRead;
	private readonly uint _encryptionSeed;

	private readonly bool _isSingleUnit;
	private readonly bool _isStreamOwner;
	private readonly Stream _stream;
	private int _currentBlockIndex;

	private byte[]? _currentData;
	private long _position;

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqStream" /> class.
	/// </summary>
	/// <param name="archive">The archive from which to load a file.</param>
	/// <param name="entry">The file's entry in the <see cref="BlockTable" />.</param>
	internal MpqStream(MpqArchive archive, MpqEntry entry)
		: this(entry, archive.BaseStream, archive.BlockSize) {
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqStream"/> class.
	/// </summary>
	/// <param name="entry">The file's entry in the <see cref="BlockTable"/>.</param>
	/// <param name="baseStream">The <see cref="MpqArchive"/>'s stream.</param>
	/// <param name="blockSize">The <see cref="MpqArchive.BlockSize"/>.</param>
	internal MpqStream(MpqEntry entry, Stream baseStream, int blockSize) {
		_canRead = true;
		_isStreamOwner = false;

		FilePosition = entry.FilePosition;
		FileSize = entry.FileSize;
		CompressedSize = entry.CompressedSize;
		Flags = entry.Flags;
		IsCompressed = (Flags & MpqFileFlags.Compressed) != 0;
		IsEncrypted = Flags.HasFlag(MpqFileFlags.Encrypted);
		_isSingleUnit = Flags.HasFlag(MpqFileFlags.SingleUnit);

		_encryptionSeed = entry.EncryptionSeed;
		_baseEncryptionSeed = entry.BaseEncryptionSeed;

		_stream = baseStream;
		BlockSize = blockSize;

		if (_isSingleUnit) {
			// Read the entire file into memory
			byte[] filedata = new byte[CompressedSize];
			lock (_stream) {
				_stream.Seek(FilePosition, SeekOrigin.Begin);
				int read = _stream.Read(filedata, 0, filedata.Length);
				if (read != filedata.Length) {
					throw new MpqParserException("Insufficient data or invalid data length");
				}
			}

			if (IsEncrypted && FileSize > 3) {
				if (_encryptionSeed == 0) {
					throw new MpqParserException("Unable to determine encryption key");
				}

				StormBuffer.DecryptBlock(filedata, _encryptionSeed);
			}

			_currentData = Flags.HasFlag(MpqFileFlags.CompressedMulti) && CompressedSize > 0
				? DecompressMulti(filedata, FileSize)
				: filedata;
		}
		else {
			_currentBlockIndex = -1;

			// Compressed files start with an array of offsets to make seeking possible
			if (IsCompressed) {
				int blockPositionsCount = (int)((FileSize + BlockSize - 1) / BlockSize) + 1;

				// Files with metadata have an extra block containing block checksums
				if (Flags.HasFlag(MpqFileFlags.FileHasMetadata)) {
					blockPositionsCount++;
				}

				_blockPositions = new uint[blockPositionsCount];

				lock (_stream) {
					_stream.Seek(FilePosition, SeekOrigin.Begin);
					using (BinaryReader br = new(_stream, new UTF8Encoding(), true)) {
						for (int i = 0; i < blockPositionsCount; i++) {
							_blockPositions[i] = br.ReadUInt32();
						}
					}
				}

				uint blockpossize = (uint)blockPositionsCount * 4;

				if (IsEncrypted && blockPositionsCount > 1) {
					uint maxOffset1 = (uint)BlockSize + blockpossize;
					if (_encryptionSeed == 0) {
						// This should only happen when the file name is not known.
						if (!entry.TryUpdateEncryptionSeed(_blockPositions[0], _blockPositions[1], blockpossize, maxOffset1)) {
							throw new MpqParserException("Unable to determine encyption seed");
						}
					}

					_encryptionSeed = entry.EncryptionSeed;
					_baseEncryptionSeed = entry.BaseEncryptionSeed;
					StormBuffer.DecryptBlock(_blockPositions, _encryptionSeed - 1);
				}

				uint currentPosition = _blockPositions[0];
				for (int i = 1; i < blockPositionsCount; i++) {
					uint currentBlockSize = _blockPositions[i] - currentPosition;

					_canRead = currentBlockSize > 0 && currentBlockSize <= BlockSize;

					currentPosition = _blockPositions[i];
				}
			}
		}
	}

	internal MpqStream(Stream baseStream, string? fileName, bool leaveOpen = false)
		: this(
			new MpqEntry(fileName, 0, 0, (uint)baseStream.Length, (uint)baseStream.Length,
				MpqFileFlags.Exists | MpqFileFlags.SingleUnit), baseStream, 0) {
		_isStreamOwner = !leaveOpen;
	}

	public MpqFileFlags Flags { get; }

	public bool IsCompressed { get; }

	public bool IsEncrypted { get; }

	public uint CompressedSize { get; }

	public uint FileSize { get; }

	public uint FilePosition { get; }

	public int BlockSize { get; }

	/// <inheritdoc />
	public override bool CanRead => _canRead;

	/// <inheritdoc />
	public override bool CanSeek => _canRead;

	/// <inheritdoc />
	public override bool CanWrite => false;

	/// <inheritdoc />
	public override long Length => FileSize;

	/// <inheritdoc />
	public override long Position {
		get => _position;
		set => Seek(value, SeekOrigin.Begin);
	}

	/// <summary>
	/// Re-encodes the stream using the given parameters.
	/// </summary>
	internal Stream Transform(MpqFileFlags targetFlags, MpqCompressionType compressionType, uint targetFilePosition,
		int targetBlockSize) {
		using MemoryStream memoryStream = new();
		CopyTo(memoryStream);
		memoryStream.Position = 0;
		long fileSize = memoryStream.Length;

		using Stream compressedStream =
			GetCompressedStream(memoryStream, targetFlags, compressionType, targetBlockSize);
		uint compressedSize = (uint)compressedStream.Length;

		MemoryStream resultStream = new();

		uint blockPosCount = (uint)(((int)fileSize + targetBlockSize - 1) / targetBlockSize) + 1;
		if (targetFlags.HasFlag(MpqFileFlags.Encrypted) && blockPosCount > 1) {
			int[] blockPositions = new int[blockPosCount];
			bool singleUnit = targetFlags.HasFlag(MpqFileFlags.SingleUnit);

			bool hasBlockPositions = !singleUnit && (targetFlags & MpqFileFlags.Compressed) != 0;
			if (hasBlockPositions) {
				for (int blockIndex = 0; blockIndex < blockPosCount; blockIndex++) {
					using (BinaryReader reader = new(compressedStream, Encoding.UTF8, true)) {
						for (int i = 0; i < blockPosCount; i++) {
							blockPositions[i] = (int)reader.ReadUInt32();
						}
					}

					compressedStream.Seek(0, SeekOrigin.Begin);
				}
			}
			else {
				if (singleUnit) {
					blockPosCount = 2;
				}

				blockPositions[0] = 0;
				for (int blockIndex = 2; blockIndex < blockPosCount; blockIndex++) {
					blockPositions[blockIndex - 1] = targetBlockSize * (blockIndex - 1);
				}

				blockPositions[blockPosCount - 1] = (int)compressedSize;
			}

			uint encryptionSeed = _baseEncryptionSeed;
			if (targetFlags.HasFlag(MpqFileFlags.BlockOffsetAdjustedKey)) {
				encryptionSeed = MpqEntry.AdjustEncryptionSeed(encryptionSeed, targetFilePosition, (uint)fileSize);
			}

			int currentOffset = 0;
			using (BinaryWriter writer = new(resultStream, UTF8EncodingProvider.StrictUTF8, true)) {
				for (int blockIndex = hasBlockPositions ? 0 : 1; blockIndex < blockPosCount; blockIndex++) {
					int toWrite = blockPositions[blockIndex] - currentOffset;

					byte[] data = StormBuffer.EncryptStream(compressedStream,
						(uint)(encryptionSeed + blockIndex - 1), currentOffset, toWrite);
					writer.Write(data);

					currentOffset += toWrite;
				}
			}
		}
		else {
			compressedStream.CopyTo(resultStream);
		}

		resultStream.Position = 0;
		return resultStream;
	}

	private static Stream GetCompressedStream(Stream baseStream, MpqFileFlags targetFlags,
		MpqCompressionType compressionType, int targetBlockSize) {
		MemoryStream resultStream = new();
		bool singleUnit = targetFlags.HasFlag(MpqFileFlags.SingleUnit);

		void TryCompress(uint bytes) {
			long offset = baseStream.Position;
			Stream compressedStream = compressionType switch {
				MpqCompressionType.ZLib => ZLibCompression.Compress(baseStream, (int)bytes, true),

				_ => throw new NotSupportedException()
			};

			long length = compressedStream.Length + 1;
			if (!singleUnit && length >= bytes) {
				baseStream.CopyTo(resultStream, offset, (int)bytes, StreamExtensions.DefaultBufferSize);
			}
			else {
				resultStream.WriteByte((byte)compressionType);
				compressedStream.Position = 0;
				compressedStream.CopyTo(resultStream);
			}

			compressedStream.Dispose();

			if (singleUnit) {
				baseStream.Dispose();
			}
		}

		uint length = (uint)baseStream.Length;

		if ((targetFlags & MpqFileFlags.Compressed) == 0) {
			baseStream.CopyTo(resultStream);
		}
		else if (singleUnit) {
			TryCompress(length);
		}
		else {
			uint blockCount = (uint)((length + targetBlockSize - 1) / targetBlockSize) + 1;
			uint[] blockOffsets = new uint[blockCount];

			blockOffsets[0] = 4 * blockCount;
			resultStream.Position = blockOffsets[0];

			for (int blockIndex = 1; blockIndex < blockCount; blockIndex++) {
				uint bytesToCompress = blockIndex + 1 == blockCount
					? (uint)(baseStream.Length - baseStream.Position)
					: (uint)targetBlockSize;

				TryCompress(bytesToCompress);
				blockOffsets[blockIndex] = (uint)resultStream.Position;
			}

			resultStream.Position = 0;
			using (BinaryWriter writer = new(resultStream, UTF8EncodingProvider.StrictUTF8, true)) {
				for (int blockIndex = 0; blockIndex < blockCount; blockIndex++) {
					writer.Write(blockOffsets[blockIndex]);
				}
			}
		}

		resultStream.Position = 0;
		return resultStream;
	}

	/// <inheritdoc />
	public override void Flush() {
	}

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin) {
		if (!CanSeek) {
			throw new NotSupportedException();
		}

		long target = origin switch {
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => Position + offset,
			SeekOrigin.End => Length + offset,

			_ => throw new InvalidEnumArgumentException(nameof(origin), (int)origin, typeof(SeekOrigin))
		};

		if (target < 0) {
			throw new ArgumentOutOfRangeException(nameof(offset),
				"Attempted to Seek before the beginning of the stream");
		}

		if (target > Length) {
			throw new ArgumentOutOfRangeException(nameof(offset), "Attempted to Seek beyond the end of the stream");
		}

		return _position = target;
	}

	/// <inheritdoc />
	public override void SetLength(long value) {
		throw new NotSupportedException("SetLength is not supported");
	}

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) {
		if (!CanRead) {
			throw new NotSupportedException();
		}

		if (_isSingleUnit) {
			return ReadInternal(buffer, offset, count);
		}

		int toread = count;
		int readtotal = 0;

		while (toread > 0) {
			int read = ReadInternal(buffer, offset, toread);
			if (read == 0) {
				break;
			}

			readtotal += read;
			offset += read;
			toread -= read;
		}

		return readtotal;
	}

	/// <inheritdoc />
	public override int ReadByte() {
		if (!CanRead) {
			throw new NotSupportedException();
		}

		if (_position >= Length) {
			return -1;
		}

		BufferData();
		return _currentData[_isSingleUnit ? _position++ : (int)(_position++ & BlockSize - 1)];
	}

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) {
		throw new NotSupportedException("Write is not supported");
	}

	/// <inheritdoc />
	public override void Close() {
		base.Close();
		if (_isStreamOwner) {
			_stream.Close();
		}
	}

	/// <summary>
	/// Copy the base stream, so that the contents do not get decompressed nor decrypted.
	/// </summary>
	internal void CopyBaseStreamTo(Stream target) {
		lock (_stream) {
			_stream.CopyTo(target, FilePosition, (int)CompressedSize, StreamExtensions.DefaultBufferSize);
		}
	}

	private static byte[] DecompressMulti(byte[] input, uint outputLength) {
		using MemoryStream memoryStream = new(input);
		return GetDecompressionFunction((MpqCompressionType)memoryStream.ReadByte(), outputLength)
			.Invoke(memoryStream);
	}

	private static Func<Stream, byte[]> GetDecompressionFunction(MpqCompressionType compressionType,
		uint outputLength) {
		return compressionType switch {
			MpqCompressionType.Huffman => HuffmanCoding.Decompress,
			MpqCompressionType.ZLib => stream => ZLibCompression.Decompress(stream, outputLength),
			MpqCompressionType.PKLib => stream => PKDecompress(stream, outputLength),
			MpqCompressionType.BZip2 => stream => BZip2Compression.Decompress(stream, outputLength),
			MpqCompressionType.Lzma => throw new NotImplementedException("LZMA compression is not yet supported"),
			MpqCompressionType.Sparse => throw new NotImplementedException(
				"Sparse compression is not yet supported"),
			MpqCompressionType.ImaAdpcmMono => stream => AdpcmCompression.Decompress(stream, 1),
			MpqCompressionType.ImaAdpcmStereo => stream => AdpcmCompression.Decompress(stream, 2),

			MpqCompressionType.Sparse | MpqCompressionType.ZLib => throw new NotImplementedException(
				"Sparse compression + Deflate compression is not yet supported"),
			MpqCompressionType.Sparse | MpqCompressionType.BZip2 => throw new NotImplementedException(
				"Sparse compression + BZip2 compression is not yet supported"),

			MpqCompressionType.ImaAdpcmMono | MpqCompressionType.Huffman => stream =>
				AdpcmCompression.Decompress(HuffmanCoding.Decompress(stream), 1),
			MpqCompressionType.ImaAdpcmMono | MpqCompressionType.PKLib => stream =>
				AdpcmCompression.Decompress(PKDecompress(stream, outputLength), 1),

			MpqCompressionType.ImaAdpcmStereo | MpqCompressionType.Huffman => stream =>
				AdpcmCompression.Decompress(HuffmanCoding.Decompress(stream), 2),
			MpqCompressionType.ImaAdpcmStereo | MpqCompressionType.PKLib => stream =>
				AdpcmCompression.Decompress(PKDecompress(stream, outputLength), 2),

			_ => throw new NotSupportedException(
				$"Compression of type 0x{compressionType.ToString("X")} is not yet supported")
		};
	}

	private static byte[] PKDecompress(Stream data, uint expectedLength) {
		int b1 = data.ReadByte();
		int b2 = data.ReadByte();
		int b3 = data.ReadByte();
		if (b1 == 0 && b2 == 0 && b3 == 0) {
			using (BinaryReader reader = new(data)) {
				uint expectedStreamLength = reader.ReadUInt32();
				if (expectedStreamLength != data.Length) {
					throw new InvalidDataException("Unexpected stream length value");
				}

				if (expectedLength + 8 == expectedStreamLength) {
					return reader.ReadBytes((int)expectedLength);
				}

				MpqCompressionType comptype = (MpqCompressionType)reader.ReadByte();
				if (comptype != MpqCompressionType.ZLib) {
					throw new NotImplementedException();
				}

				return ZLibCompression.Decompress(data, expectedLength);
			}
		}

		data.Seek(-3, SeekOrigin.Current);
		return PKLibCompression.Decompress(data, expectedLength);
	}

	private int ReadInternal(byte[] buffer, int offset, int count) {
		if (_position >= Length) {
			return 0;
		}

		BufferData();

		long localPosition = _isSingleUnit ? _position : _position & BlockSize - 1;
		int canRead = (int)(_currentData.Length - localPosition);
		int bytesToCopy = canRead > count ? count : canRead;
		if (bytesToCopy <= 0) {
			return 0;
		}

		Array.Copy(_currentData, localPosition, buffer, offset, bytesToCopy);

		_position += bytesToCopy;
		return bytesToCopy;
	}

	[MemberNotNull(nameof(_currentData))]
	private void BufferData() {
		if (!_isSingleUnit) {
			int requiredBlock = (int)(_position / BlockSize);
			if (requiredBlock != _currentBlockIndex || _currentData is null) {
				int expectedLength = Math.Min((int)(Length - requiredBlock * BlockSize), BlockSize);
				_currentData = LoadBlock(requiredBlock, expectedLength);
				_currentBlockIndex = requiredBlock;
			}
		}
		else if (_currentData is null) {
			_currentData = LoadSingleUnit();
		}
	}

	private byte[] LoadSingleUnit() {
		byte[] fileData = new byte[CompressedSize];
		lock (_stream) {
			_stream.Seek(FilePosition, SeekOrigin.Begin);
			_stream.CopyTo(fileData, 0, fileData.Length);
		}

		if (IsEncrypted && FileSize > 3) {
			if (_encryptionSeed == 0) {
				throw new MpqParserException("Unable to determine encryption key");
			}

			StormBuffer.DecryptBlock(fileData, _encryptionSeed);
		}

		return Flags.HasFlag(MpqFileFlags.CompressedMulti) && CompressedSize > 0
			? DecompressMulti(fileData, FileSize)
			: fileData;
	}

	private byte[] LoadBlock(int blockIndex, int expectedLength) {
		long offset;
		int bufferSize;

		if (IsCompressed) {
			offset = _blockPositions[blockIndex];
			bufferSize = (int)(_blockPositions[blockIndex + 1] - offset);
		}
		else {
			offset = (uint)(blockIndex * BlockSize);
			bufferSize = expectedLength;
		}

		offset += FilePosition;

		byte[] buffer = new byte[bufferSize];
		lock (_stream) {
			_stream.Seek(offset, SeekOrigin.Begin);
			_stream.CopyTo(buffer, 0, bufferSize);
		}

		if (IsEncrypted && bufferSize > 3) {
			if (_encryptionSeed == 0) {
				throw new MpqParserException("Unable to determine encryption key");
			}

			StormBuffer.DecryptBlock(buffer, (uint)(blockIndex + _encryptionSeed));
		}

		if (IsCompressed && bufferSize != expectedLength) {
			buffer = Flags.HasFlag(MpqFileFlags.CompressedPK)
				? PKLibCompression.Decompress(buffer, (uint)expectedLength)
				: DecompressMulti(buffer, (uint)expectedLength);
		}

		return buffer;
	}

	private bool TryPeekCompressionType(out MpqCompressionType? mpqCompressionType) {
		int bufferSize = Math.Min((int)CompressedSize, 4);

		byte[] buffer = new byte[bufferSize];
		lock (_stream) {
			_stream.Seek(FilePosition, SeekOrigin.Begin);
			int read = _stream.Read(buffer, 0, bufferSize);
			if (read != bufferSize) {
				mpqCompressionType = null;
				return false;
			}
		}

		if (IsEncrypted && bufferSize > 3) {
			if (_encryptionSeed == 0) {
				mpqCompressionType = null;
				return false;
			}

			StormBuffer.DecryptBlock(buffer, _encryptionSeed);
		}

		if (Flags.HasFlag(MpqFileFlags.CompressedMulti) && bufferSize > 0) {
			mpqCompressionType = (MpqCompressionType)buffer[0];
			return true;
		}

		mpqCompressionType = null;
		return true;
	}

	private bool TryPeekCompressionType(int blockIndex, int expectedLength,
		out MpqCompressionType? mpqCompressionType) {
		uint offset = _blockPositions[blockIndex];
		int bufferSize = (int)(_blockPositions[blockIndex + 1] - offset);

		if (bufferSize == expectedLength) {
			mpqCompressionType = null;
			return !IsEncrypted || bufferSize < 4 || _encryptionSeed != 0;
		}

		offset += FilePosition;
		bufferSize = Math.Min(bufferSize, 4);

		byte[] buffer = new byte[bufferSize];
		lock (_stream) {
			_stream.Seek(offset, SeekOrigin.Begin);
			int read = _stream.Read(buffer, 0, bufferSize);
			if (read != bufferSize) {
				mpqCompressionType = null;
				return false;
			}
		}

		if (IsEncrypted && bufferSize > 3) {
			if (_encryptionSeed == 0) {
				mpqCompressionType = null;
				return false;
			}

			StormBuffer.DecryptBlock(buffer, (uint)(blockIndex + _encryptionSeed));
		}

		if (Flags.HasFlag(MpqFileFlags.CompressedPK)) {
			mpqCompressionType = null;
			return true;
		}

		mpqCompressionType = (MpqCompressionType)buffer[0];
		return true;
	}
}