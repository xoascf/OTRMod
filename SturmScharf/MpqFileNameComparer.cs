namespace SturmScharf;

internal sealed class MpqFileNameComparer : MpqFileComparer {
	private readonly bool _ignoreLocale;

	internal MpqFileNameComparer(bool ignoreLocale) {
		_ignoreLocale = ignoreLocale;
	}

	public override int Compare(MpqFile? x, MpqFile? y) {
		if (ReferenceEquals(x, y))
			return 0;

		if (x is null || x is MpqOrphanedFile)
			return -1;

		if (y is null || y is MpqOrphanedFile)
			return 1;

		if (x.Name == y.Name)
			return _ignoreLocale ? 0 : x.Locale.CompareTo(y.Locale);

		if (x is MpqKnownFile file1 && y is MpqKnownFile file2)
			return string.Compare(file1.FileName, file2.FileName, StringComparison.OrdinalIgnoreCase);

		return x.Name.CompareTo(y.Name);
	}

	public override bool Equals(MpqFile? x, MpqFile? y) => Compare(x, y) == 0;

	public override int GetHashCode(MpqFile? mpqFile) => mpqFile is null
			? throw new ArgumentNullException(nameof(mpqFile))
			: _ignoreLocale ? HashCode.Combine(mpqFile.Name) : mpqFile.GetHashCode();
}