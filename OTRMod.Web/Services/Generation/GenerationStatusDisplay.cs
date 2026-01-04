namespace OTRMod.Web.Services.Generation;

/// <summary>
/// Handles display logic for generation status.
/// Following Single Responsibility - only handles display concerns.
/// </summary>
public sealed class GenerationStatusDisplay : IGenerationStatusDisplay
{
    public string GetAlertClass(GenerationStatus status) => status switch
    {
        GenerationStatus.Idle => "alert-primary",
        GenerationStatus.Ready => "alert-info",
        GenerationStatus.Loading => "alert-secondary",
        GenerationStatus.Generating => "alert-secondary",
        GenerationStatus.Completed => "alert-success",
        GenerationStatus.Error => "alert-danger",
        _ => "alert-secondary"
    };

    public string GetMessageKey(GenerationStatus status) => status switch
    {
        GenerationStatus.Idle => "info_not_sel",
        GenerationStatus.Ready => "info_sel",
        GenerationStatus.Loading => "loading",
        GenerationStatus.Generating => "info_wait",
        GenerationStatus.Completed => "info_fin",
        GenerationStatus.Error => "error",
        _ => "info_not_sel"
    };

    public object[]? GetMessageParams(GenerationState state)
    {
        if (state.Status == GenerationStatus.Error && !string.IsNullOrEmpty(state.ErrorMessage))
        {
            return new object[] { state.ErrorMessage };
        }
        return null;
    }
}
