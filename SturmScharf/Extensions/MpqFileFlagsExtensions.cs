namespace SturmScharf.Extensions;

public static class MpqFileFlagsExtensions {
	/// <summary>
	/// Returns <see langword="true" /> if <paramref name="mpqFileFlags" /> has both the
	/// <see cref="MpqFileFlags.Encrypted" /> and <see cref="MpqFileFlags.BlockOffsetAdjustedKey" /> flags.
	/// </summary>
	public static bool IsOffsetEncrypted(this MpqFileFlags mpqFileFlags)
		=> mpqFileFlags.HasFlag(MpqFileFlags.Encrypted | MpqFileFlags.BlockOffsetAdjustedKey);
}