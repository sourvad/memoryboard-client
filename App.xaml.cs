using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Memoryboard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Handle exceptions from UI thread
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Handle exceptions from background threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception, show an error dialog, etc.
            // For example, you could log to a file or use a logging library like log4net or NLog.
            // LogError(e.Exception); // Assuming you have a LogError method

            MessageBox.Show("Memoryboard - An unhandled exception just occurred: " + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevents the application from crashing immediately

            // Optionally, close the application gracefully
            Current.Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // This event handler will catch non-UI thread exceptions
            if (e.ExceptionObject is Exception exception)
            {
                // LogError(exception); // Log the error as needed
                MessageBox.Show("Memoryboard - A background exception just occurred: " + exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Current.Shutdown();
        }
    }

}
