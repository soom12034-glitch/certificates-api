using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class AsyncRelayCommand : ICommand
{
    readonly Func<object?, Task> _execute;
    readonly Func<object?, bool>? _canExecute;
    bool _isExecuting;

    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting)
            return;
        if (!CanExecute(parameter))
            return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            try
            {
                BlueMax.Presentation.Wpf.App.Log($"[AsyncRelayCommand] Command failed: {ex}");
            }
            catch
            {
            }
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
