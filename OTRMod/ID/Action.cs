namespace OTRMod.ID;

public enum Action {
	/// <summary>
	/// Get Raw Data from Image ("var" start(h) length(h)).
	/// </summary>
	Get,

	/// <summary>
	/// Replace All in Variable ("var" oldBytes(h) newBytes(h)).
	/// </summary>
	Rep,

	/// <summary>
	/// Set or Update value for operation.
	/// </summary>
	Set,

	/// <summary>
	/// Merge Content Data and Table ("msgVar1" "tblVar2" addChars3 "fileName4").
	/// </summary>
	Mrg,

	/// <summary>
	/// Set New Directory to Save.
	/// </summary>
	Dir,

	/// <summary>
	/// Save Raw Data from Variable ("var1" "fileName2").
	/// </summary>
	Sav,

	/// <summary>
	/// Automatic Export.
	/// </summary>
	Exp,
}