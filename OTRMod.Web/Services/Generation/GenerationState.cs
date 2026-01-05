namespace OTRMod.Web.Services.Generation;

/// <summary>
/// Represents the current state of a generation process.
/// Following Single Responsibility Principle - this only holds state data.
/// </summary>
public sealed class GenerationState {
	public GenerationStatus Status { get; private set; } = GenerationStatus.Idle;
	public string? ErrorMessage { get; private set; }
	public double Progress { get; private set; }
	public string? CurrentOperation { get; private set; }

	public bool CanGenerate => Status is GenerationStatus.Ready;
	public bool IsWorking => Status is GenerationStatus.Loading or GenerationStatus.Generating;
	public bool HasError => Status is GenerationStatus.Error;

	internal void SetIdle() {
		Status = GenerationStatus.Idle;
		ErrorMessage = null;
		Progress = 0;
		CurrentOperation = null;
	}

	internal void SetReady() {
		Status = GenerationStatus.Ready;
		ErrorMessage = null;
	}

	internal void SetLoading(string? operation = null) {
		Status = GenerationStatus.Loading;
		CurrentOperation = operation;
		ErrorMessage = null;
	}

	internal void SetGenerating(string? operation = null, double progress = 0) {
		Status = GenerationStatus.Generating;
		CurrentOperation = operation;
		Progress = progress;
		ErrorMessage = null;
	}

	internal void SetProgress(double progress) {
		Progress = Math.Clamp(progress, 0, 100);
	}

	internal void SetCompleted() {
		Status = GenerationStatus.Completed;
		Progress = 100;
		CurrentOperation = null;
	}

	internal void SetError(string message) {
		Status = GenerationStatus.Error;
		ErrorMessage = message;
		CurrentOperation = null;
	}
}

/// <summary>
/// Generation status following Open/Closed Principle - 
/// new states can be added without modifying existing code.
/// </summary>
public enum GenerationStatus {
	/// <summary>Initial state, nothing selected.</summary>
	Idle,

	/// <summary>Required inputs are ready for generation.</summary>
	Ready,

	/// <summary>Loading input files.</summary>
	Loading,

	/// <summary>Processing/generating output.</summary>
	Generating,

	/// <summary>Generation completed successfully.</summary>
	Completed,

	/// <summary>An error occurred.</summary>
	Error
}