using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Input_Assistant;

namespace Input_Assistant.Tests
{
    /// <summary>
    /// Contains unit tests for the SpeechToTextService class.
    /// </summary>
    public class SpeechToTextServiceTests : IDisposable
    {
        private readonly SpeechToTextService _service;

        /// <summary>
        /// Initializes a new instance of SpeechToTextService using a dummy API key.
        /// </summary>
        public SpeechToTextServiceTests()
        {
            // Use a dummy API key because real WebSocket connection is not established in tests.
            _service = new SpeechToTextService("dummy-api-key");
        }

        /// <summary>
        /// Disposes the SpeechToTextService instance.
        /// </summary>
        public void Dispose()
        {
            _service.Dispose();
        }

        /// <summary>
        /// Tests that ProcessTranscriptionResponse raises the OnTranscriptionReceived event
        /// when provided with a valid JSON payload that contains a "text" property.
        /// </summary>
        [Fact]
        public void ProcessTranscriptionResponse_ValidJson_RaisesEvent()
        {
            // Arrange
            string testText = "Hello, world";
            string jsonPayload = $"{{\"text\": \"{testText}\"}}";
            string? received = null;
            _service.OnTranscriptionReceived += (sender, text) =>
            {
                received = text;
            };

            // Act: Invoke the private ProcessTranscriptionResponse method via reflection
            MethodInfo? method = typeof(SpeechToTextService)
                .GetMethod("ProcessTranscriptionResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(_service, new object[] { jsonPayload });

            // Assert: the event should be raised with the correct transcription text.
            Assert.Equal(testText, received);
        }

        /// <summary>
        /// Tests that ProcessTranscriptionResponse does not raise an event and handles errors gracefully
        /// when provided with an invalid JSON string.
        /// </summary>
        [Fact]
        public void ProcessTranscriptionResponse_InvalidJson_NoEvent()
        {
            // Arrange: use an invalid JSON format.
            string invalidJson = "not a json";
            bool eventRaised = false;
            _service.OnTranscriptionReceived += (sender, text) =>
            {
                eventRaised = true;
            };

            // Act: invoke the private method using reflection
            MethodInfo? method = typeof(SpeechToTextService)
                .GetMethod("ProcessTranscriptionResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Expect no exception to propagate, even if the JSON is bad.
            Exception? ex = Record.Exception(() => method!.Invoke(_service, new object[] { invalidJson }));
            Assert.Null(ex);

            // Assert: the event should not be raised.
            Assert.False(eventRaised);
        }

        /// <summary>
        /// Tests that ProcessTranscriptionResponse does not raise an event when the JSON payload
        /// does not contain a "text" property.
        /// </summary>
        [Fact]
        public void ProcessTranscriptionResponse_NoTextProperty_NoEvent()
        {
            // Arrange: JSON with no "text" property.
            string jsonPayload = "{\"other\": \"data\"}";
            bool eventRaised = false;
            _service.OnTranscriptionReceived += (sender, text) =>
            {
                eventRaised = true;
            };

            // Act: invoke the private method via reflection.
            MethodInfo? method = typeof(SpeechToTextService)
                .GetMethod("ProcessTranscriptionResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(_service, new object[] { jsonPayload });

            // Assert: no event is raised.
            Assert.False(eventRaised);
        }

        /// <summary>
        /// Tests that calling StopListeningAsync on an unstarted service does not throw an exception.
        /// </summary>
        [Fact]
        public async Task StopListeningAsync_BeforeStarting_DoesNotThrow()
        {
            // Arrange: Service has not been started (so _isListening is false).
            // Act & Assert: StopListeningAsync completes without throwing.
            var exception = await Record.ExceptionAsync(() => _service.StopListeningAsync());
            Assert.Null(exception);
        }
    }
}