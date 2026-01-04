namespace OTRMod.Web.Services.Generation;

/// <summary>
/// Interface for managing generation state.
/// Following Interface Segregation Principle - clients only see what they need.
/// </summary>
public interface IGenerationStateManager
{
    /// <summary>Current generation state (read-only view).</summary>
    GenerationState State { get; }

    /// <summary>Event raised when state changes.</summary>
    event Action? OnStateChanged;

    /// <summary>Transition to idle state.</summary>
    void SetIdle();

    /// <summary>Transition to ready state when inputs are valid.</summary>
    void SetReady();

    /// <summary>Transition to loading state.</summary>
    void SetLoading(string? operation = null);

    /// <summary>Transition to generating state.</summary>
    void SetGenerating(string? operation = null);

    /// <summary>Update progress during generation.</summary>
    void UpdateProgress(double progress);

    /// <summary>Transition to completed state.</summary>
    void SetCompleted();

    /// <summary>Transition to error state.</summary>
    void SetError(string message);

    /// <summary>Transition to error state from exception.</summary>
    void SetError(Exception exception);
}
