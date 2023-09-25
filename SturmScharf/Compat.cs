#if NET40
using System.IO;
using System.Text;

namespace SturmScharf.Compat;

public class BinaryWriter : System.IO.BinaryWriter {
	private readonly bool _leaveOpen;

	public BinaryWriter(Stream output) : base(output) { }

	public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
		: base(output, encoding) {
		this._leaveOpen = leaveOpen;
	}

	protected override void Dispose(bool disposing) {
		if (disposing)
			if (!_leaveOpen)
				base.Dispose(disposing);
	}
}

public class BinaryReader : System.IO.BinaryReader {
	private readonly bool _leaveOpen;
	private bool _disposed;

	public BinaryReader(Stream input) : base(input) { }

	public BinaryReader(Stream input, Encoding encoding, bool leaveOpen)
		: base(input, encoding) {
		this._leaveOpen = leaveOpen;
	}

	protected override void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing && !_leaveOpen)
				base.Dispose(disposing);
			_disposed = true;
		}
	}
}

public static class BitOperations {
	public static uint RotateLeft(uint value, int count)
		=> (value << count) | (value >> (32 - count));
}

public static class HashCode {
	public static int Combine<T1>(T1 value1) {
		uint hc1 = (uint)(value1?.GetHashCode() ?? 0);

		uint hash = MixEmptyState();
		hash += 4;

		hash = QueueRound(hash, hc1);

		hash = MixFinal(hash);
		return (int)hash;
	}

	public static int Combine<T1, T2>(T1 value1, T2 value2) {
		uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
		uint hc2 = (uint)(value2?.GetHashCode() ?? 0);

		uint hash = MixEmptyState();
		hash += 8;

		hash = QueueRound(hash, hc1);
		hash = QueueRound(hash, hc2);

		hash = MixFinal(hash);
		return (int)hash;
	}

	private static uint MixEmptyState() => HashCodeSeed;

	private static uint QueueRound(uint hash, uint value) {
		hash += value;
		hash *= Prime1;
		return BitOperations.RotateLeft(hash, 13) * Prime2;
	}

	private static uint MixFinal(uint hash) {
		hash ^= hash >> 16;
		hash *= Prime3;
		hash ^= hash >> 13;
		hash *= Prime4;
		hash ^= hash >> 16;
		return hash;
	}

	// Define suitable prime numbers and HashCodeSeed
	private const uint Prime1 = 2654435761U;
	private const uint Prime2 = 2246822519U;
	private const uint Prime3 = 3266489917U;
	private const uint Prime4 = 668265263U;
	private const int HashCodeSeed = 0; // FIXME: Should we use random as in Runtime?
}
#endif