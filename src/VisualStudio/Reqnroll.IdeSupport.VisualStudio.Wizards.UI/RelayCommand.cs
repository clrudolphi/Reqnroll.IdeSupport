// A portable ICommand implementation that replaces the VS SDK DelegateCommand
// (Microsoft.VisualStudio.PlatformUI.DelegateCommand) for use in the wizard layer.
namespace Reqnroll.IdeSupport.VisualStudio.Wizards.UI;

/// <summary>
/// A portable <see cref="ICommand"/> implementation that delegates <see cref="Execute"/> and
/// <see cref="CanExecute"/> to supplied delegates, re-querying on <see cref="CommandManager.RequerySuggested"/>.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>Creates the command with an execute delegate and an optional can-execute predicate.</summary>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>Raised when the command's <see cref="CommandManager.RequerySuggested"/> should be reevaluated.</summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>Returns whether the command can currently execute, via the supplied predicate (default <c>true</c>).</summary>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>Invokes the command's execute delegate.</summary>
    public void Execute(object? parameter) => _execute(parameter);
}
