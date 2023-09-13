#if NET40
using System.IO;
using System.Text;

namespace SturmScharf.Compat;

public class BinaryWriter : System.IO.BinaryWriter {
	private readonly bool leaveOpen;

	public BinaryWriter(Stream output) : base(output) { }

	public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
		: base(output, encoding) {
		this.leaveOpen = leaveOpen;
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (!leaveOpen) {
				base.Dispose(disposing);
			}
		}
	}
}

public class BinaryReader : System.IO.BinaryReader {
	private readonly bool leaveOpen;
	private bool _disposed;

	public BinaryReader(Stream input) : base(input) { }

	public BinaryReader(Stream input, Encoding encoding, bool leaveOpen)
		: base(input, encoding) {
		this.leaveOpen = leaveOpen;
	}

	protected override void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing && !leaveOpen) {
				base.Dispose(disposing);
			}
			_disposed = true;
		}
	}
}

public static class HashCode {
	public static int Combine(params object[] objects) {
		if (objects == null) {
			throw new ArgumentNullException(nameof(objects));
		}

		uint hash = MixEmptyState();
		int shift = 0;

		foreach (object obj in objects) {
			if (obj != null) {
				uint hc = (uint)obj.GetHashCode();
				hash = QueueRound(hash, hc << shift);
			}

			shift += 3; // Adjust the shift to prevent collisions
		}

		hash = MixFinal(hash);
		return (int)hash;
	}

	private static uint MixEmptyState() {
		return (uint)HashCodeSeed;
	}

	private static uint QueueRound(uint hash, uint value) {
		hash += value;
		hash *= Prime1;
		return RotateLeft(hash, 13) * Prime2;
	}

	private static uint MixFinal(uint hash) {
		hash ^= hash >> 16;
		hash *= Prime3;
		hash ^= hash >> 13;
		hash *= Prime4;
		hash ^= hash >> 16;
		return hash;
	}

	private static uint RotateLeft(uint value, int count) {
		return (value << count) | (value >> (32 - count));
	}

	// Define suitable prime numbers and HashCodeSeed
	private const uint Prime1 = 2654435761U;
	private const uint Prime2 = 2246822519U;
	private const uint Prime3 = 3266489917U;
	private const uint Prime4 = 668265263U;
	private const int HashCodeSeed = 0; // FIXME: Should we use random as in Runtime?
}
#endif