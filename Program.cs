using System;

namespace Input_Assistant
{
    internal class Program
    {
        /// <summary>
        /// Entry point for the Input Assistant application.
        /// </summary>
        /// <param name="args">Command-line arguments (optionally containing the API key).</param>
        static void Main(string[] args)
        {
            // Load configuration (e.g., API key) from command-line args or environment variables.
            AppConfig config = ConfigManager.LoadConfig(args);

            // Initialize services
            var keyboardHook = new KeyboardHookManager();
            var voiceCommandProcessor = new VoiceCommandProcessor();
            var speechService = new SpeechToTextService(config.OpenAIApiKey);
            var inputSimulator = new InputSimulator();
            var screenshotHelper = new ScreenshotHelper();
            var statsManager = new StatisticsManager();

            // TODO: Wire up event handlers between the modules.
            // For example, when a hotkey is pressed via the keyboard hook,
            // activate the speech service or process new voice commands.

            Console.WriteLine("Input Assistant started. Press any key to exit...");
            Console.ReadKey();

            // Shutdown services gracefully when exiting.
            keyboardHook.Stop();
            speechService.StopListeningAsync().RunSynchronously();
        }
    }
}
