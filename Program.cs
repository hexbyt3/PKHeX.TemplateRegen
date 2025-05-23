using PKHeX.TemplateRegen.Forms;

namespace PKHeX.TemplateRegen;

internal static class Program
{
    private static MainForm? _mainForm;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        using var mutex = new Mutex(true, "PKHeX.TemplateRegen.SingleInstance", out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("PKHeX Template Regenerator is already running!",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        try
        {
            _mainForm = new MainForm();
            Application.Run(_mainForm);
        }
        finally
        {
            Cleanup();
        }
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

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        Cleanup();
    }

    private static void Cleanup()
    {
        try
        {
            _mainForm?.Dispose();
            _mainForm = null;
            AppLogManager.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
