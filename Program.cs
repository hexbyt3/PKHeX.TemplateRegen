using PKHeX.TemplateRegen.Forms;

namespace PKHeX.TemplateRegen;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Enable visual styles for modern UI
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Set high DPI awareness for better display on modern monitors
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        // Check for single instance
        using var mutex = new Mutex(true, "PKHeX.TemplateRegen.SingleInstance", out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("PKHeX Template Regenerator is already running!",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Handle unhandled exceptions
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Run the application
        Application.Run(new MainForm());

        // Cleanup
        AppLogManager.Dispose();
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        AppLogManager.LogError("Unhandled thread exception", e.Exception);

        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue running.",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppLogManager.LogError("Unhandled exception", ex);

            MessageBox.Show(
                $"A critical error occurred:\n\n{ex.Message}\n\nThe application will terminate.",
                "Critical Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
