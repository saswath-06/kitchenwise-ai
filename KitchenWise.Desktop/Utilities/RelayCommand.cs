using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KitchenWise.Desktop.Utilities
{
    /// <summary>
    /// A command implementation that relays its functionality to delegates
    /// Used for binding button clicks and other UI actions to ViewModel methods
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of RelayCommand
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked</param>
        /// <param name="canExecute">Optional function to determine if the command can execute</param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Event raised when the CanExecute state changes
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute
        /// </summary>
        /// <param name="parameter">Command parameter (not used in this implementation)</param>
        /// <returns>True if the command can execute, false otherwise</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="parameter">Command parameter (not used in this implementation)</param>
        public void Execute(object? parameter)
        {
            try
            {
                _execute();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"[ERROR] RelayCommand execution failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");

                // Re-throw the exception so the UI can handle it appropriately
                throw;
            }
        }

        /// <summary>
        /// Manually triggers the CanExecuteChanged event
        /// Call this when conditions that affect CanExecute have changed
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Generic version of RelayCommand that accepts a parameter
    /// </summary>
    /// <typeparam name="T">Type of the command parameter</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of RelayCommand with parameter
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked</param>
        /// <param name="canExecute">Optional function to determine if the command can execute</param>
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Event raised when the CanExecute state changes
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if the command can execute, false otherwise</returns>
        public bool CanExecute(object? parameter)
        {
            // Convert parameter to the expected type
            if (parameter is T typedParameter)
            {
                return _canExecute?.Invoke(typedParameter) ?? true;
            }

            // Handle null parameter for nullable types
            if (parameter == null && default(T) == null)
            {
                return _canExecute?.Invoke(default(T)) ?? true;
            }

            return false;
        }

        /// <summary>
        /// Executes the command with the provided parameter
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        public void Execute(object? parameter)
        {
            try
            {
                // Convert parameter to the expected type
                if (parameter is T typedParameter)
                {
                    _execute(typedParameter);
                }
                else if (parameter == null && default(T) == null)
                {
                    _execute(default(T));
                }
                else
                {
                    throw new ArgumentException($"Parameter must be of type {typeof(T).Name}", nameof(parameter));
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"[ERROR] RelayCommand<{typeof(T).Name}> execution failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");

                // Re-throw the exception so the UI can handle it appropriately
                throw;
            }
        }

        /// <summary>
        /// Manually triggers the CanExecuteChanged event
        /// Call this when conditions that affect CanExecute have changed
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Async version of RelayCommand for operations that need to await
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of AsyncRelayCommand
        /// </summary>
        /// <param name="execute">The async action to execute when the command is invoked</param>
        /// <param name="canExecute">Optional function to determine if the command can execute</param>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Indicates if the command is currently executing
        /// </summary>
        public bool IsExecuting => _isExecuting;

        /// <summary>
        /// Event raised when the CanExecute state changes
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute
        /// Commands cannot execute while already executing (prevents double-execution)
        /// </summary>
        /// <param name="parameter">Command parameter (not used in this implementation)</param>
        /// <returns>True if the command can execute, false otherwise</returns>
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        /// <summary>
        /// Executes the async command
        /// </summary>
        /// <param name="parameter">Command parameter (not used in this implementation)</param>
        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Executes the async command and returns the task
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>Task representing the async operation</returns>
        public async Task ExecuteAsync(object? parameter)
        {
            if (_isExecuting)
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"[ERROR] AsyncRelayCommand execution failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");

                // Note: In async void methods, exceptions should be handled here
                // rather than re-thrown as they won't be caught by the caller
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Manually triggers the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}