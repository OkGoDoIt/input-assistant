using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Input_Assistant
{
    /// <summary>
    /// Service responsible for capturing audio and streaming it to OpenAI's real-time voice API
    /// for transcription. Handles WebSocket connection lifecycle and processes incremental transcriptions.
    /// </summary>
    public class SpeechToTextService : IDisposable
    {
        private readonly string _apiKey;
        private readonly ClientWebSocket _webSocket;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isListening;
        private bool _isDisposed;

        /// <summary>
        /// NAudio component for capturing microphone input.
        /// </summary>
        private WaveInEvent? _waveIn;

        // Event to notify subscribers of new transcription text
        public event EventHandler<string>? OnTranscriptionReceived;

        /// <summary>
        /// Initializes a new instance of the SpeechToTextService.
        /// </summary>
        /// <param name="apiKey">OpenAI API key for authentication</param>
        public SpeechToTextService(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            _isListening = false;
        }

        /// <summary>
        /// Starts the audio capture and streaming process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StartListeningAsync()
        {
            if (_isListening)
                return;

            try
            {
                // Connect to OpenAI's WebSocket endpoint
                var uri = new Uri("wss://api.openai.com/v1/audio/speech");
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");

                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                _isListening = true;

                // Configure the session with required parameters
                var sessionConfig = new
                {
                    model = "whisper-1",
                    input_format = "pcm16",
                    input_sample_rate = 16000,
                    transcription_options = new
                    {
                        language = "en",
                        incremental = true
                    }
                };

                var configJson = JsonSerializer.Serialize(sessionConfig);
                var configBytes = Encoding.UTF8.GetBytes(configJson);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(configBytes),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource.Token);

                // Initialize microphone audio capture using NAudio with PCM16 (16000 Hz, 16-bit, Mono)
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1)
                };

                // Subscribe to the DataAvailable event to stream audio data to the WebSocket
                _waveIn.DataAvailable += WaveIn_DataAvailable;

                // Start recording audio from the microphone
                _waveIn.StartRecording();

                // Start receiving transcriptions in a separate task
                _ = Task.Run(ReceiveTranscriptionsAsync);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start speech-to-text service", ex);
            }
        }

        /// <summary>
        /// Continuously receives and processes transcriptions from the WebSocket connection.
        /// </summary>
        private async Task ReceiveTranscriptionsAsync()
        {
            var buffer = new byte[4096];
            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var jsonResponse = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessTranscriptionResponse(jsonResponse);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await StopListeningAsync();
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no action needed
            }
            catch (Exception ex)
            {
                // Log error and attempt graceful shutdown
                Console.Error.WriteLine($"Error receiving transcriptions: {ex.Message}");
                await StopListeningAsync();
            }
        }

        /// <summary>
        /// Processes the JSON response from OpenAI and raises events for transcribed text.
        /// </summary>
        /// <param name="jsonResponse">The JSON response from the API</param>
        private void ProcessTranscriptionResponse(string jsonResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("text", out var textElement))
                {
                    var transcribedText = textElement.GetString();
                    if (!string.IsNullOrEmpty(transcribedText))
                    {
                        OnTranscriptionReceived?.Invoke(this, transcribedText);
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Error parsing transcription response: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for the DataAvailable event.
        /// Sends captured PCM audio data to the OpenAI WebSocket endpoint.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">Contains audio buffer data</param>
        private async void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded),
                        WebSocketMessageType.Binary,
                        true,
                        _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error sending audio data: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the audio capture and streaming process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StopListeningAsync()
        {
            if (!_isListening)
                return;

            try
            {
                _isListening = false;
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Stopping transcription service",
                        _cancellationTokenSource.Token);
                }

                // If audio capture is active, stop and dispose it
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error stopping transcription service: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes of resources used by the service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _webSocket.Dispose();
                _waveIn?.Dispose();
            }

            _isDisposed = true;
        }
    }
}