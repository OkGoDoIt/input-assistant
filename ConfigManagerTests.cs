using Xunit;
using Input_Assistant;

namespace InputAssistant.Tests
{
    /// <summary>
    /// Contains unit tests for the ConfigManager class.
    /// </summary>
    public class ConfigManagerTests
    {
        [Fact]
        public void LoadConfig_ShouldReturnDefaultApiKey_WhenNoArgumentsProvided()
        {
            // Arrange: no command-line arguments provided.
            string[] args = new string[0];

            // Act: load the configuration
            var config = ConfigManager.LoadConfig(args);

            // Assert: the default API key should be the placeholder.
            Assert.Equal("YOUR_API_KEY", config.ApiKey);
        }
    }
}