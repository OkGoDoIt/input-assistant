# Recording PCM Audio from Microphone in Modern C#

## Overview

To record PCM audio from a user's microphone in modern C#, you can utilize the **NAudio** library, which provides a straightforward interface for audio operations. This guide will walk you through setting up a console application to capture audio input and save it as a WAV file.

## 1. Set Up the Project

### Create a Console Application

- Open Visual Studio and create a new **Console App** project targeting .NET 6.0 or later.

### Install NAudio Package

- In the NuGet Package Manager Console, run:

```bash
Install-Package NAudio
```

## 2. Implement Audio Recording

Below is a complete example demonstrating how to record audio from the default microphone and save it as a WAV file:

```csharp
using System;
using NAudio.Wave;

class Program
{
    static void Main()
    {
        Console.WriteLine("Press 'R' to start recording and 'S' to stop...");

        using var waveIn = new WaveInEvent();
        using var writer = new WaveFileWriter("RecordedAudio.wav", waveIn.WaveFormat);

        waveIn.DataAvailable += (sender, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        };

        waveIn.RecordingStopped += (sender, e) =>
        {
            writer.Dispose();
            waveIn.Dispose();
            Console.WriteLine("Recording stopped.");
        };

        while (true)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.R)
            {
                waveIn.StartRecording();
                Console.WriteLine("Recording started...");
            }
            else if (key == ConsoleKey.S)
            {
                waveIn.StopRecording();
                break;
            }
        }
    }
}
```

## 3. Explanation

- **WaveInEvent:** Captures audio from the microphone.
- **WaveFileWriter:** Writes the recorded audio to a WAV file.
- **DataAvailable Event:** Triggered when there's audio data available; writes data to the file.
- **RecordingStopped Event:** Handles cleanup after recording is stopped.

## 4. Considerations

- **Audio Format:** The default format is typically **44.1 kHz, 16-bit, mono**. Adjust `WaveFormat` as needed.
- **Error Handling:** Implement `try-catch` blocks to handle exceptions appropriately.
- **Dependencies:** Ensure that the **NAudio** library is properly referenced in your project.

By following this guide, you can effectively record PCM audio from the user's microphone in a modern C# application.
