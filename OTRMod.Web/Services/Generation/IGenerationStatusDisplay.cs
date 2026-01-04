namespace OTRMod.Web.Services.Generation;

/// <summary>
/// Interface for generation status display.
/// Follows Dependency Inversion - high-level modules depend on abstractions.
/// </summary>
public interface IGenerationStatusDisplay
{
    /// <summary>Gets the CSS class for the alert display.</summary>
    string GetAlertClass(GenerationStatus status);

    /// <summary>Gets the localization key for the status message.</summary>
    string GetMessageKey(GenerationStatus status);

    /// <summary>Gets additional message parameters (e.g., error message).</summary>
    object[]? GetMessageParams(GenerationState state);
}
