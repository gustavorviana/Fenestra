namespace Fenestra.Core.Models;

/// <summary>
/// Contains the activation arguments and user input values from a toast interaction.
/// </summary>
public class ToastActivatedArgs
{
    /// <summary>
    /// The launch arguments from the clicked button or toast body.
    /// </summary>
    public string Arguments { get; }

    /// <summary>
    /// User input values keyed by input element id.
    /// </summary>
    public IReadOnlyDictionary<string, string> UserInput { get; }

    public ToastActivatedArgs(string arguments, Dictionary<string, string> userInput)
    {
        Arguments = arguments;
        UserInput = userInput;
    }
}
