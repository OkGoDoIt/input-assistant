using System;
using System.IO;
using Xunit;


namespace Input_Assistant.Tests;



/// <summary>
/// Contains unit tests for the ConfigManager class.
/// </summary>
public class ConfigManagerTests
{
	/// <summary>
	/// Generates a unique temporary directory path for testing and assigns it to ConfigManager.PreferencesDirectory.
	/// </summary>
	/// <returns>The path to the temporary directory.</returns>
	private string SetupTemporaryPreferencesDirectory()
	{
		string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InputAssistantTests_" + Guid.NewGuid());
		ConfigManager.PreferencesDirectory = tempDir;
		return tempDir;
	}

	/// <summary>
	/// Tests that when the API key is provided as a command-line argument, the configuration is loaded correctly.
	/// </summary>
	[Fact]
	public void LoadConfig_WithCommandLineArgument_ReturnsApiKey()
	{
		string tempDir = SetupTemporaryPreferencesDirectory();
		try
		{
			// Arrange: Define a test API key and set it in the command-line arguments.
			string testApiKey = "test-api-key";
			string[] args = new string[] { "--openai-key", testApiKey };

			// Act: Load the configuration using the command-line arguments.
			AppConfig config = ConfigManager.LoadConfig(args);

			// Assert: The loaded API key should match the test API key provided in the arguments.
			Assert.Equal(testApiKey, config.OpenAIApiKey);
		}
		finally
		{
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}

	/// <summary>
	/// Tests that when the API key is provided via an environment variable, the configuration is loaded correctly.
	/// </summary>
	[Fact]
	public void LoadConfig_WithEnvVariable_ReturnsApiKey()
	{
		string tempDir = SetupTemporaryPreferencesDirectory();
		try
		{
			// Arrange: Set a test API key in the environment variable and provide no command-line arguments.
			string testApiKey = "env-test-api-key";
			Environment.SetEnvironmentVariable("OPENAI_API_KEY", testApiKey);
			string[] args = new string[0];

			// Act: Load the configuration using the empty command-line arguments.
			AppConfig config = ConfigManager.LoadConfig(args);

			// Assert: The loaded API key should match the test API key from the environment variable.
			Assert.Equal(testApiKey, config.OpenAIApiKey);
		}
		finally
		{
			// Cleanup environment variable
			Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}

	/// <summary>
	/// Tests that when neither a command-line argument nor an environment variable is provided, an ArgumentException is thrown.
	/// </summary>
	[Fact]
	public void LoadConfig_WithNoApiKey_ThrowsArgumentException()
	{
		string tempDir = SetupTemporaryPreferencesDirectory();
		try
		{
			// Arrange: Ensure that no API key is set in the environment and provide empty command-line arguments.
			Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
			string[] args = new string[0];

			// Act & Assert: Expect an ArgumentException to be thrown when attempting to load the configuration.
			Assert.Throws<ArgumentException>(() =>
			{
				ConfigManager.LoadConfig(args);
			});
		}
		finally
		{
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}

	/// <summary>
	/// Tests that the command-line argument takes precedence over the environment variable when both are provided.
	/// </summary>
	[Fact]
	public void LoadConfig_CommandLineHasPrecedenceOverEnvVariable()
	{
		string tempDir = SetupTemporaryPreferencesDirectory();
		try
		{
			// Arrange: Set both a command-line API key and an environment variable API key.
			string envApiKey = "env-api-key";
			string argApiKey = "arg-api-key";
			Environment.SetEnvironmentVariable("OPENAI_API_KEY", envApiKey);
			string[] args = new string[] { "--openai-key", argApiKey };

			// Act: Load the configuration using the provided arguments.
			AppConfig config = ConfigManager.LoadConfig(args);

			// Assert: The API key from the command-line should take precedence over the environment variable.
			Assert.Equal(argApiKey, config.OpenAIApiKey);
		}
		finally
		{
			// Cleanup environment variable
			Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}

	/// <summary>
	/// Tests that saving preferences and then loading them returns the correct configuration.
	/// </summary>
	[Fact]
	public void SaveAndLoadPreferences_ReturnsCorrectConfig()
	{
		string tempDir = SetupTemporaryPreferencesDirectory();
		try
		{
			string configFile = Path.Combine(tempDir, "config.json");
			if (File.Exists(configFile))
			{
				File.Delete(configFile);
			}

			string testApiKey = "saved-key";
			AppConfig configToSave = new AppConfig { OpenAIApiKey = testApiKey };

			// Act: Save the configuration then load it back.
			ConfigManager.SavePreferences(configToSave);
			AppConfig? loadedConfig = ConfigManager.LoadSavedPreferences();

			// Assert: The loaded configuration should match the saved API key.
			Assert.NotNull(loadedConfig);
			Assert.Equal(testApiKey, loadedConfig!.OpenAIApiKey);
		}
		finally
		{
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}

	/// <summary>
	/// Tests that loading saved preferences when no configuration file exists returns null.
	/// </summary>
	[Fact]
	public void LoadSavedPreferences_NoFile_ReturnsNull()
	{
		string tempDir = SetupTemporaryPreferencesDirectory();
		try
		{
			string configFile = Path.Combine(tempDir, "config.json");
			if (File.Exists(configFile))
			{
				File.Delete(configFile);
			}

			// Act: Attempt to load saved preferences.
			AppConfig? loadedConfig = ConfigManager.LoadSavedPreferences();

			// Assert: The result should be null since no file exists.
			Assert.Null(loadedConfig);
		}
		finally
		{
			if (Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, true);
			}
		}
	}
}
