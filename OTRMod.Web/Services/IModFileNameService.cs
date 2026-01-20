namespace OTRMod.Web.Services;

/// <summary>
/// Service for generating appropriate mod file names based on content and settings.
/// </summary>
public interface IModFileNameService {
	/// <summary>
	/// Generates a mod file name based on the message path and output format.
	/// </summary>
	/// <param name="messagePath">The path of the message file in the mod</param>
	/// <param name="outputFormat">The output format (O2R or OTR)</param>
	/// <param name="isOverride">Whether the file is an override file</param>
	/// <returns>A suggested file name for the mod</returns>
	string GenerateFileName(string messagePath, OutputFormat outputFormat, bool isOverride);
}