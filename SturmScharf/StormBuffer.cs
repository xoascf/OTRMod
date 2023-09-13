using SturmScharf.Extensions;
using System.Collections.Generic;
using System.IO;

namespace SturmScharf;

internal sealed class StormBuffer {
	private static readonly Lazy<StormBuffer> _stormBuffer = new(() => new StormBuffer());

	private readonly uint[] _buffer;

	private StormBuffer() {
		uint seed = 0x100001;

		_buffer = new uint[0x500];

		for (uint index1 = 0; index1 < 0x100; index1++) {
			uint index2 = index1;
			for (int i = 0; i < 5; i++, index2 += 0x100) {
				seed = (seed * 125 + 3) % 0x2aaaab;
				uint temp = (seed & 0xffff) << 16;
				seed = (seed * 125 + 3) % 0x2aaaab;

				_buffer[index2] = temp | seed & 0xffff;
			}
		}
	}

	private static uint[] Buffer => _stormBuffer.Value._buffer;

	/// <summary>
	/// </summary>
	/// <param name="input">The input string for which a hash value should be generated.</param>
	/// <param name="offset">
	/// A key to generate different values for the same string. Values commonly used are 0, 0x100, 0x200,
	/// and 0x300.
	/// </param>
	/// <returns>A hashed value for the given <paramref name="input" />.</returns>
	internal static uint HashString(string input, int offset) {
		uint seed1 = 0x7fed7fed;
		uint seed2 = 0xeeeeeeee;

		foreach (char c in NormalizeString(input)) {
			int val = c;
			seed1 = Buffer[offset + val] ^ seed1 + seed2;
			seed2 = (uint)val + seed1 + seed2 + (seed2 << 5) + 3;
		}

		return seed1;
	}

	internal static bool TryGetHashString(string input, int offset, out uint hash) {
		hash = 0x7fed7fed;

		if (input.ContainsInvalidChar())
			return false;

		uint seed2 = 0xeeeeeeee;
		foreach (char c in NormalizeString(input)) {
			int val = c;
			hash = Buffer[offset + val] ^ hash + seed2;
			seed2 = (uint)val + hash + seed2 + (seed2 << 5) + 3;
		}

		return true;
	}

	internal static string NormalizeString(string input) {
		return input.ToUpperInvariant();
	}

	internal static byte[] EncryptStream(Stream stream, uint seed1, int offset, int length) {
		byte[] data = new byte[length];
		stream.Seek(offset, SeekOrigin.Begin);
		if (stream.Read(data, 0, length) != length) {
			throw new Exception("Insufficient data or invalid data length");
		}

		EncryptBlock(data, seed1);
		return data;
	}

	internal static void EncryptBlock(byte[] data, uint seed1) {
		uint seed2 = 0xeeeeeeee;

		for (int i = 0; i < data.Length - 3; i += 4) {
			seed2 += Buffer[0x400 + (seed1 & 0xff)];

			uint unencrypted = BitConverter.ToUInt32(data, i);
			uint result = unencrypted ^ seed1 + seed2;

			seed1 = (~seed1 << 21) + 0x11111111 | seed1 >> 11;
			seed2 = unencrypted + seed2 + (seed2 << 5) + 3;

			data[i + 0] = (byte)(result & 0xff);
			data[i + 1] = (byte)(result >> 8 & 0xff);
			data[i + 2] = (byte)(result >> 16 & 0xff);
			data[i + 3] = (byte)(result >> 24 & 0xff);
		}
	}

	internal static void EncryptBlock(uint[] data, uint seed1) {
		uint seed2 = 0xeeeeeeee;

		for (int i = 0; i < data.Length; i++) {
			seed2 += Buffer[0x400 + (seed1 & 0xff)];

			uint unencrypted = data[i];
			uint result = unencrypted ^ seed1 + seed2;

			seed1 = (~seed1 << 21) + 0x11111111 | seed1 >> 11;
			seed2 = unencrypted + seed2 + (seed2 << 5) + 3;

			data[i] = result;
		}
	}

	internal static void DecryptBlock(byte[] data, uint seed1) {
		uint seed2 = 0xeeeeeeee;

		for (int i = 0; i < data.Length - 3; i += 4) {
			seed2 += Buffer[0x400 + (seed1 & 0xff)];

			uint result = BitConverter.ToUInt32(data, i);
			result ^= seed1 + seed2;

			seed1 = (~seed1 << 21) + 0x11111111 | seed1 >> 11;
			seed2 = result + seed2 + (seed2 << 5) + 3;

			data[i + 0] = (byte)(result & 0xff);
			data[i + 1] = (byte)(result >> 8 & 0xff);
			data[i + 2] = (byte)(result >> 16 & 0xff);
			data[i + 3] = (byte)(result >> 24 & 0xff);
		}
	}

	internal static void DecryptBlock(uint[] data, uint seed1) {
		uint seed2 = 0xeeeeeeee;

		for (int i = 0; i < data.Length; i++) {
			seed2 += Buffer[0x400 + (seed1 & 0xff)];
			uint result = data[i];
			result ^= seed1 + seed2;

			seed1 = (~seed1 << 21) + 0x11111111 | seed1 >> 11;
			seed2 = result + seed2 + (seed2 << 5) + 3;
			data[i] = result;
		}
	}

	internal static bool DetectFileSeed(uint value0, uint value1, uint decrypted, out uint detectedSeed) {
		uint temp = (value0 ^ decrypted) - 0xeeeeeeee;

		for (int i = 0; i < 0x100; i++) {
			uint seed1 = temp - Buffer[0x400 + i];
			uint seed2 = 0xeeeeeeee + Buffer[0x400 + (seed1 & 0xff)];
			uint result = value0 ^ seed1 + seed2;

			if (result != decrypted) {
				continue;
			}

			detectedSeed = seed1;

			seed1 = (~seed1 << 21) + 0x11111111 | seed1 >> 11;
			seed2 = result + seed2 + (seed2 << 5) + 3;

			seed2 += Buffer[0x400 + (seed1 & 0xff)];
			result = value1 ^ seed1 + seed2;

			if ((result & 0xfffc0000) == 0) {
				return true;
			}
		}

		detectedSeed = 0;
		return false;
	}

	internal static IEnumerable<uint> DetectFileSeeds(uint value0, uint decrypted) {
		uint temp = (value0 ^ decrypted) - 0xeeeeeeee;

		for (int i = 0; i < 0x100; i++) {
			uint seed1 = temp - Buffer[0x400 + i];
			uint seed2 = 0xeeeeeeee + Buffer[0x400 + (seed1 & 0xff)];
			uint result = value0 ^ seed1 + seed2;

			if (result == decrypted) {
				yield return seed1;
			}
		}
	}
}