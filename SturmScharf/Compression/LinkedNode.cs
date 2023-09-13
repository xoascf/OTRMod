namespace SturmScharf.Compression;
/// <summary>
/// A node which is both hierachcical (parent/child) and doubly linked (next/prev).
/// </summary>
internal sealed class LinkedNode {
	/// <summary>
	/// Initializes a new instance of the <see cref="LinkedNode" /> class.
	/// </summary>
	internal LinkedNode(int decompressedVal, int weight) {
		DecompressedValue = decompressedVal;
		Weight = weight;
	}

	internal int DecompressedValue { get; }

	internal int Weight { get; set; }

	internal LinkedNode? Next { get; set; }

	internal LinkedNode? Prev { get; set; }

	internal LinkedNode? Parent { get; set; }

	internal LinkedNode? Child0 { get; set; }

	internal LinkedNode? Child1 => Child0?.Prev;

	internal LinkedNode Insert(LinkedNode other) {
		if (other.Weight <= Weight) {
			if (Next != null) {
				Next.Prev = other;
				other.Next = Next;
			}

			Next = other;
			other.Prev = this;
			return other;
		}

		if (Prev == null) {
			other.Prev = null;
			Prev = other;
			other.Next = this;
		}
		else {
			Prev.Insert(other);
		}

		return this;
	}
}