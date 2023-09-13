﻿using System.Collections;
using System.Collections.Generic;

namespace SturmScharf;

public abstract class MpqFileComparer : IComparer, IEqualityComparer, IComparer<MpqFile?>,
	IEqualityComparer<MpqFile?> {
	private static readonly Lazy<MpqFileComparer> _defaultComparer = new(() => new MpqFileNameComparer(false));

	private static readonly Lazy<MpqFileComparer> _defaultIgnoreLocaleComparer =
		new(() => new MpqFileNameComparer(true));

	public static MpqFileComparer Default => _defaultComparer.Value;

	public static MpqFileComparer DefaultIgnoreLocale => _defaultIgnoreLocaleComparer.Value;

	public int Compare(object? x, object? y) {
		if (x == y) {
			return 0;
		}

		if (x is null) {
			return -1;
		}

		if (y is null) {
			return 1;
		}

		if (x is MpqFile file1 && y is MpqFile file2) {
			return Compare(file1, file2);
		}

		if (x is IComparable comparable) {
			return comparable.CompareTo(y);
		}

		throw new ArgumentException($"Argument must implement {nameof(IComparable)}.", nameof(x));
	}

	public abstract int Compare(MpqFile? x, MpqFile? y);

	public new bool Equals(object x, object y) {
		if (x == y) {
			return true;
		}

		if (x is null || y is null) {
			return false;
		}

		if (x is MpqFile file1 && y is MpqFile file2) {
			return Equals(file1, file2);
		}

		return x.Equals(y);
	}

	public int GetHashCode(object obj) {
		if (obj is null) {
			throw new ArgumentNullException(nameof(obj));
		}

		if (obj is MpqFile mpqFile) {
			return GetHashCode(mpqFile);
		}

		return obj.GetHashCode();
	}

	public abstract bool Equals(MpqFile? x, MpqFile? y);

	public abstract int GetHashCode(MpqFile mpqFile);
}