using Xunit;
using Input_Assistant;

namespace InputAssistant.Tests
{
    /// <summary>
    /// Contains tests for the VoiceCommandProcessor.
    /// </summary>
    public class VoiceCommandProcessorTests
    {
        [Fact]
        public void ProcessCommand_WithEmptyInput_ShouldNotThrowException()
        {
            // Arrange: create an instance of the command processor.
            var processor = new VoiceCommandProcessor();
            string command = string.Empty;

            // Act & Assert: ensure no exceptions occur.
            var exception = Record.Exception(() => processor.ProcessCommand(command));
            Assert.Null(exception);
        }
    }
}