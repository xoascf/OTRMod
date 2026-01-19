namespace OTRMod.Web.Services.Generation;

/// <summary>
/// Manages generation state transitions.
/// Following Single Responsibility - only manages state transitions.
/// </summary>
public sealed class GenerationStateManager : IGenerationStateManager {
	private readonly GenerationState _state = new();

	public GenerationState State => _state;

	public event Action? OnStateChanged;

	public void SetIdle() {
		_state.SetIdle();
		NotifyStateChanged();
	}

	public void SetReady() {
		_state.SetReady();
		NotifyStateChanged();
	}

	public void SetLoading(string? operation = null) {
		_state.SetLoading(operation);
		NotifyStateChanged();
	}

	public void SetGenerating(string? operation = null) {
		_state.SetGenerating(operation);
		NotifyStateChanged();
	}

	public void UpdateProgress(double progress) {
		_state.SetProgress(progress);
		NotifyStateChanged();
	}

	public void SetCompleted() {
		_state.SetCompleted();
		NotifyStateChanged();
	}

	public void SetError(string message) {
		_state.SetError(message);
		NotifyStateChanged();
	}

	public void SetError(Exception exception) {
		_state.SetError(exception.ToString());
		NotifyStateChanged();
	}

	private void NotifyStateChanged() => OnStateChanged?.Invoke();
}