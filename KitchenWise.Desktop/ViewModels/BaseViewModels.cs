using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KitchenWise.Desktop.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels providing INotifyPropertyChanged implementation
    /// and common functionality for MVVM pattern
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _busyMessage = string.Empty;
        private bool _hasErrors;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Indicates if the ViewModel is currently performing an operation
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnBusyStateChanged();
                }
            }
        }

        /// <summary>
        /// Message to display when IsBusy is true
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        /// <summary>
        /// Indicates if the ViewModel has validation or operation errors
        /// </summary>
        public bool HasErrors
        {
            get => _hasErrors;
            set
            {
                if (SetProperty(ref _hasErrors, value))
                {
                    OnErrorStateChanged();
                }
            }
        }

        /// <summary>
        /// Error message to display to the user
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Name of the property (automatically provided)</param>
        /// <returns>True if the value changed, false otherwise</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Called when the IsBusy state changes
        /// Override in derived classes to respond to busy state changes
        /// </summary>
        protected virtual void OnBusyStateChanged()
        {
            // Override in derived classes if needed
        }

        /// <summary>
        /// Called when the HasErrors state changes
        /// Override in derived classes to respond to error state changes
        /// </summary>
        protected virtual void OnErrorStateChanged()
        {
            // Override in derived classes if needed
        }

        /// <summary>
        /// Sets the busy state with an optional message
        /// </summary>
        /// <param name="isBusy">Whether the operation is in progress</param>
        /// <param name="message">Message to display while busy</param>
        protected void SetBusyState(bool isBusy, string message = "")
        {
            BusyMessage = message;
            IsBusy = isBusy;
        }

        /// <summary>
        /// Sets the error state with an optional error message
        /// </summary>
        /// <param name="hasError">Whether there is an error</param>
        /// <param name="errorMessage">Error message to display</param>
        protected void SetErrorState(bool hasError, string errorMessage = "")
        {
            ErrorMessage = errorMessage;
            HasErrors = hasError;
        }

        /// <summary>
        /// Clears any error state
        /// </summary>
        protected void ClearErrors()
        {
            SetErrorState(false, string.Empty);
        }

        /// <summary>
        /// Handles exceptions consistently across ViewModels
        /// </summary>
        /// <param name="ex">Exception that occurred</param>
        /// <param name="userMessage">User-friendly message to display</param>
        protected void HandleException(Exception ex, string userMessage = "An error occurred")
        {
            // Log the exception
            Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");

            // Set error state for UI
            SetErrorState(true, userMessage);

            // Clear busy state
            IsBusy = false;
        }

        /// <summary>
        /// Validates the current state of the ViewModel
        /// Override in derived classes to implement custom validation
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public virtual bool Validate()
        {
            ClearErrors();
            return true;
        }

        /// <summary>
        /// Called when the ViewModel is being disposed or cleaned up
        /// Override in derived classes to clean up resources
        /// </summary>
        public virtual void Cleanup()
        {
            // Override in derived classes for cleanup
            Console.WriteLine($"Cleaning up {GetType().Name}");
        }

        /// <summary>
        /// Called when the ViewModel is activated/loaded
        /// Override in derived classes to initialize data
        /// </summary>
        public virtual void OnActivated()
        {
            // Override in derived classes for initialization
            Console.WriteLine($"Activated {GetType().Name}");
        }

        /// <summary>
        /// Called when the ViewModel is deactivated/unloaded
        /// Override in derived classes to save state or clean up
        /// </summary>
        public virtual void OnDeactivated()
        {
            // Override in derived classes for cleanup
            Console.WriteLine($"Deactivated {GetType().Name}");
        }

        /// <summary>
        /// Creates a formatted log message for debugging
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="level">Log level (INFO, WARN, ERROR)</param>
        protected void Log(string message, string level = "INFO")
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"[{timestamp}] [{level}] {GetType().Name}: {message}");
        }

        /// <summary>
        /// Logs debug information (only in debug builds)
        /// </summary>
        /// <param name="message">Debug message</param>
        [System.Diagnostics.Conditional("DEBUG")]
        protected void LogDebug(string message)
        {
            Log(message, "DEBUG");
        }

        /// <summary>
        /// Safely invokes an action and handles any exceptions
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="errorMessage">Error message if action fails</param>
        protected void SafeExecute(Action action, string errorMessage = "Operation failed")
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                HandleException(ex, errorMessage);
            }
        }

        /// <summary>
        /// Refreshes the ViewModel data
        /// Override in derived classes to implement data refresh logic
        /// </summary>
        public virtual void Refresh()
        {
            LogDebug("Refresh requested");
            // Override in derived classes
        }
    }
}