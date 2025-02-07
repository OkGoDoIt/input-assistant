# Implementation Plan for Windows Realtime Input Assistant

## 1. Overview
The Windows Realtime Input Assistant is a background command-line application built using C# 13 and .NET 9. Its primary purpose is to allow users to interact with their computer using voice commands by:
- Capturing audio and converting it to text using the OpenAI real-time voice API.
- Interpreting voice commands, such as dictation or corrections.
- Simulating keyboard input based on the transcriptions.
- Providing additional context (e.g., screenshots) to help improve transcription accuracy.

## 2. Architecture & Modules
The project is organized into distinct modules, each responsible for a specific aspect of the application. This modular design ensures maintainability, testability, and flexibility as requirements evolve.

### Core Modules/Subsystems

- **Configuration Management**
  - **Files:** `ConfigManager.cs`
  - **Responsibilities:**
    - Load application settings from command-line arguments or environment variables (e.g., OpenAI API key).
    - Store user preferences (for instance, saving the API key between sessions).

- **Speech-To-Text Service**
  - **Files:** `SpeechToTextService.cs`
  - **Responsibilities:**
    - Capture and stream audio to the OpenAI real-time voice API.
    - Process incremental transcriptions and handle connection lifecycle.
    - Support both REST and WebSocket connections.

- **Keyboard Input Handling**
  - **Keyboard Hooks**
    - **Files:** `KeyboardHookManager.cs`
    - **Responsibilities:**
      - Install and manage low-level keyboard hooks (via P/Invoke) to detect hotkey events.
      - Ensure normal keyboard operations remain unaffected.
  - **Input Simulation**
    - **Files:** `InputSimulator.cs`
    - **Responsibilities:**
      - Simulate keyboard input using Windows API functions (e.g., `SendInput` or `keybd_event`) to mimic typing from the transcription.

- **Voice Command Processing**
  - **Files:** `VoiceCommandProcessor.cs`
  - **Responsibilities:**
    - Parse and interpret voice commands (e.g., "type code", "replace XYZ with ABC").
    - Distinguish between plain dictation and commands that require corrective actions.
    - Maintain a cache or log of nonstandard words and user corrections.

- **Context Capture & Correction**
  - **Screenshot Capture**
    - **Files:** `ScreenshotHelper.cs`
    - **Responsibilities:**
      - Capture a screenshot of the active window or the area around the keyboard focus.
      - Annotate screenshots to indicate keyboard focus for sending as context.
  - **Statistics Management**
    - **Files:** `StatisticsManager.cs`
    - **Responsibilities:**
      - Track token usage, costs from the OpenAI API, and aggregate statistics.
      - Provide reports upon user request.

## 3. Implementation Steps

### 3.1. Configuration Management
- **Goal:**
  Retrieve configuration settings, such as the OpenAI API key, from command-line arguments or environment variables.
- **Steps:**
  1. Expand the `ConfigManager.LoadConfig` method to parse command-line parameters.
  2. Check environment variables for an API key if none is provided as an argument.
  3. Instantiate and return an `AppConfig` object with the collected values.
  4. Implement error handling for cases where the API key is missing or invalid.

### 3.2. Develop the Speech-To-Text Service
- **Goal:**
  Establish a real-time audio streaming connection to OpenAI's API and process the transcription results.
- **Steps:**
  1. Initialize HTTP/WebSocket clients during construction.
  2. Use asynchronous programming (`async/await`) to stream audio without blocking the main thread.
  3. Continuously read incremental transcriptions and forward them to the `VoiceCommandProcessor`.
  4. Provide cancellation support to stop transcription cleanly when needed.
  5. Implement robust error handling and retry mechanisms.

### 3.3. Implement Keyboard Input Handling
- **Keyboard Hooks (KeyboardHookManager):**
  - **Goal:**
    Detect activation of special hotkeys (e.g., F12) to trigger different listening modes.
  - **Steps:**
    1. Utilize P/Invoke to call Windows API methods like `SetWindowsHookEx` and `UnhookWindowsHookEx`.
    2. Design callback functions (delegates) to process keyboard events.
    3. Ensure that the hook does not interfere with normal keyboard usage.
- **Input Simulation (InputSimulator):**
  - **Goal:**
    Simulate keystrokes to output transcribed text to the active window.
  - **Steps:**
    1. Implement a method that translates string characters to a series of virtual key codes.
    2. Use Windows API calls (e.g., `SendInput`) to simulate these key presses.
    3. Test various text inputs to ensure reliable replication of natural typing.

### 3.4. Integrate Voice Command Processing
- **Goal:**
  Analyze transcribed text to determine if it contains a command (e.g., "type code" or a correction command) and then execute the corresponding action.
- **Steps:**
  1. Design the command parser logic inside `VoiceCommandProcessor.ProcessCommand`.
  2. Identify keywords/phrases that denote specific actions.
  3. Route plain dictation to the `InputSimulator` and special commands to trigger context capture or corrections.
  4. Build a flexible system to allow expansion of supported voice commands.

### 3.5. Add Context Capture and Correction Capabilities
- **Screenshot Capture (ScreenshotHelper):**
  - **Goal:**
    Provide context to the transcription service by capturing the area surrounding the keyboard focus.
  - **Steps:**
    1. Use .NET's System.Drawing to capture the current screen area.
    2. Implement annotation features to mark the keyboard focus.
    3. Return the annotated image for later processing or forwarding to the API.
- **Statistics Management (StatisticsManager):**
  - **Goal:**
    Track API usage and costs.
  - **Steps:**
    1. Implement methods to record usage details each time the API is called.
    2. Aggregate this data to compile user-friendly reports.
    3. Add persistence for usage data to be retained across application sessions.

## 4. Integration & Workflow Orchestration

### Listening Modes

The system supports two primary listening modes:
- **Always Listening Mode:** The application continuously listens for user speech. It waits for specific trigger phrases (e.g., "type code", "type text") before it starts transcription, ensuring that random conversation is not transcribed.
- **Hotkey Mode:** The application activates only when a designated hotkey (e.g., F12) is pressed. In this mode, holding the key keeps dictation active, and releasing the key (or detecting a pause in speech) stops transcription immediately.

- **Initialization:**
  Start by loading configuration and initializing all modules in the Main method (`Program.cs`). Wire up necessary event handlers between modules.
- **Event Flow Example:**
  - A hotkey is detected via `KeyboardHookManager`.
  - `SpeechToTextService` begins streaming audio.
  - Transcribed audio is passed to `VoiceCommandProcessor`.
  - Depending on the parsed command, either:
    - `InputSimulator` outputs the text in real time, or
    - `ScreenshotHelper` captures context and the command feedback is processed.
- **Shutdown:**
  Ensure all services are gracefully stopped and resources are released when the application exits.

## 5. Error Handling and Logging
- Implement try-catch blocks in critical sections.
- Log errors and events to a file or standard output.

## 6. Testing and Quality Assurance
- Follow the guidelines in the [testing-plan.md](testing-plan.md) file.
- Write unit tests for individual modules.
- Develop integration tests to verify that the modules work together as intended.

## Conclusion
This implementation plan serves as a comprehensive guide for future developers to build upon the Windows Realtime Input Assistant. By following the modular approach and detailed steps outlined above, engineers can ensure the development of a reliable, maintainable, and extensible project.

Happy Coding!