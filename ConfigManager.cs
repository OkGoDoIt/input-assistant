using System;
using System.IO;
using System.Text.Json;


namespace Input_Assistant
{
    /// <summary>
    /// Represents the application configuration settings loaded from command-line arguments, environment variables, or saved user preferences.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Gets or sets the OpenAI API key.
        /// </summary>
        public string OpenAIApiKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Provides functionality to load and manage configuration settings for the application.
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>
        /// Gets or sets the directory where user preferences are saved. Defaults to the Application Data folder under 'InputAssistant'.
        /// This property can be overridden for testing purposes.
        /// </summary>
        public static string PreferencesDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InputAssistant");

        /// <summary>
        /// Loads the application configuration settings from command-line arguments, environment variables, or saved user preferences.
        /// It first checks for a command-line argument "--openai-key" and, if not provided, checks the "OPENAI_API_KEY" environment variable.
        /// If still not found, it attempts to load saved preferences from disk.
        /// If the API key is provided via command-line, it is saved as a user preference for future sessions.
        /// </summary>
        /// <param name="args">The command-line arguments passed to the application.</param>
        /// <returns>An instance of <see cref="AppConfig"/> containing the loaded configuration settings.</returns>
        /// <exception cref="ArgumentException">Thrown when the OpenAI API key is missing.</exception>
        public static AppConfig LoadConfig(string[] args)
        {
            string? apiKey = null;
            bool isFromCommandLine = false;

            // Parse command-line arguments to find the API key.
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--openai-key", StringComparison.OrdinalIgnoreCase) && (i + 1) < args.Length)
                {
                    apiKey = args[i + 1];
                    isFromCommandLine = true;
                    break;
                }
            }

            // If API key is not provided via command-line, attempt to retrieve it from environment variables.
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            // If API key is still not available, attempt to load saved preferences from disk.
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                AppConfig? savedConfig = LoadSavedPreferences();
                if (savedConfig != null && !string.IsNullOrWhiteSpace(savedConfig.OpenAIApiKey))
                {
                    apiKey = savedConfig.OpenAIApiKey;
                }
            }

            // If the API key is still missing, throw an error.
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("OpenAI API key is missing. Please provide it via the --openai-key command line argument, set the OPENAI_API_KEY environment variable, or save it once provided.");
            }

            // If the API key was provided via command-line, save it as a user preference for future sessions.
            if (isFromCommandLine)
            {
                SavePreferences(new AppConfig { OpenAIApiKey = apiKey });
            }

            // Return the constructed configuration object.
            return new AppConfig { OpenAIApiKey = apiKey };
        }

        /// <summary>
        /// Saves the user preferences to disk so that configuration settings persist across sessions.
        /// The configuration is saved as a JSON file in the directory specified by <see cref="PreferencesDirectory"/>.
        /// </summary>
        /// <param name="config">The configuration object to save.</param>
        public static void SavePreferences(AppConfig config)
        {
            // Use the PreferencesDirectory property for saving preferences.
            string configDir = PreferencesDirectory;
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            string configFile = Path.Combine(configDir, "config.json");

            // Serialize the configuration object to JSON with indentation for readability.
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, json);
        }

        /// <summary>
        /// Loads the saved user preferences from disk if available.
        /// The configuration is expected to be a JSON file stored in the directory specified by <see cref="PreferencesDirectory"/>.
        /// </summary>
        /// <returns>An <see cref="AppConfig"/> object if preferences are found; otherwise, null.</returns>
        public static AppConfig? LoadSavedPreferences()
        {
            string configDir = PreferencesDirectory;
            string configFile = Path.Combine(configDir, "config.json");

            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                return JsonSerializer.Deserialize<AppConfig>(json);
            }
            return null;
        }
    }
}