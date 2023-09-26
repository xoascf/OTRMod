namespace SturmScharf;

/// <summary>
/// The format version of a MoPaQ file.
/// </summary>
public enum MpqVersion : ushort {
	/// <summary>
	/// The original MPQ format used before WoW First Expansion.
	/// </summary>
	/// <remarks>
	/// Supports archives up to 4GB in size or up to 65536 files.
	/// </remarks>
	Original = 0,

	/// <summary>
	/// Extended format introduced in WoW: The Burning Crusade.
	/// </summary>
	/// <remarks>
	/// Supports archives larger than 4GB.
	/// </remarks>
	BurningCrusade = 1,

	// FIXME: Newer formats aren't fully supported for now...
	CataclysmBeta = 2,
	Cataclysm = 3,
}