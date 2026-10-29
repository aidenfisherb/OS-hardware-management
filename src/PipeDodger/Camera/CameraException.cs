namespace PipeDodger.Camera;

/// <summary>A camera problem the user can act on. Message says what happened; Hint says what to try.</summary>
public sealed class CameraException(string message, string hint) : Exception(message)
{
    public string Hint { get; } = hint;
}
