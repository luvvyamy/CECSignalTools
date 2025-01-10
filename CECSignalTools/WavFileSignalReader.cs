using System.Diagnostics.CodeAnalysis;

namespace CECSignalTools;

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class WavFileSignalReader
{
    public static List<ISignal> ReadSignalsFromWavFile(string filePath)
    {
        int bitLength; // The length of a bit in samples
        var data = new List<short>();
        
        // Read bytes and store the left channel
        using (var reader = new WaveFileReader(filePath))
        {
            if (reader.WaveFormat.Channels != 2)
            {
                throw new Exception("Only stereo files are supported.");
            }

            bitLength = reader.WaveFormat.SampleRate / 4800;

            var blockAlign = reader.WaveFormat.BlockAlign;
            var buffer = new byte[blockAlign];
            
            while (reader.Read(buffer, 0, blockAlign) > 0)
            {
                // Extract left channel (first 2 bytes of each block)
                var leftSample = BitConverter.ToInt16(buffer[0..2], 0);
                data.Add(leftSample);
            }
        }
        
        // Skip until signals start and convert all zero values to one (for the Math.Sign function to work properly)
        var skipAmount = data.FindIndex(x => x is > 1000 or < -1000);
        data = data.Skip(skipAmount).Select(x => x == 0 ? (short)1 : x).ToList();
        
        // Detect Polarity Changes
        var polarityChanges = Enumerable.Range(0, data.Count - 1)
            .Where(i => Math.Sign(data[i]) != Math.Sign(data[i + 1])).ToList();

        var bits = new List<int>();
        var timeStamps = new List<int>();
        ushort readingCounter = 0; // Each byte has 9 bits, one for confirmation and the rest with data

        // Read zeros and ones from the polarity changes
        for (var i = 1; i < polarityChanges.Count; i++)
        {
            if (polarityChanges[i] - polarityChanges[i - 1] > bitLength/2 + 2)
            {
                // We ignore all zeros unless we're reading a byte (ALL bytes start with 1)
                if (readingCounter > 0)
                {
                    bits.Add(0);
                    readingCounter++;
                }
            }
            else
            {
                bits.Add(1);
                readingCounter++;
                i += 1;
            }

            if (readingCounter < 9) continue;
            
            // If we finish reading a byte, we will add the timestamp and reset the reading counter
            timeStamps.Add(polarityChanges[i] + skipAmount);
            readingCounter = 0;
        }
        
        // Convert bits to bytes and signal objects and return it
        return ParseBitsToSignals(timeStamps, bits);
    }

    private static List<ISignal> ParseBitsToSignals(List<int> timeStamps, List<int> bits)
    {
        int lastByteInt = 0;
        bool commandMode = false;
        string currentCommand = "";
        int a900CommandCounter = 0;
        string textCommandText = "";

        var signals = new List<ISignal>();
        
        for (var i = 0; i < bits.Count; i += 9)
        {
            if (bits.Count - i < 9) break;

            var chunk = bits.Skip(i + 1).Take(8).Reverse().Select(b => b == 0 ? 1 : 0).ToList();
            chunk[0] = 0;
            var chunkStr = string.Join("", chunk).PadLeft(8, '0');
            if (chunkStr == "01111111") continue;
                
            var currentByte = Convert.ToByte(chunkStr, 2);
            int currentByteInt = currentByte;

            double convertedTimeStamp = timeStamps[i / 9] / 48000f;

            if (currentByteInt != 0)
            {
                if (commandMode && currentByteInt != 36)
                {
                    if (string.IsNullOrEmpty(currentCommand))
                    {
                        currentCommand = ((char)currentByteInt).ToString();
                    }

                    switch (currentCommand)
                    {
                        case "T":
                        case "1":
                        case "2":
                        {
                            if (lastByteInt != 36)
                            {
                                textCommandText += (char)currentByteInt;
                            }

                            break;
                        }
                        case "A":
                        {
                            if (currentByteInt != 65)
                            {
                                textCommandText += (char)currentByteInt;
                                a900CommandCounter++;
                                if (a900CommandCounter == 3)
                                {
                                    a900CommandCounter = 0;
                                    commandMode = false;
                                    currentCommand = "";
                                    signals.Add(new MiscSignal(convertedTimeStamp, MiscSignal.MiscSignalType.A900));
                                }
                            }

                            break;
                        }
                        default:
                        {
                            commandMode = false;
                            switch (currentCommand)
                            {
                                case "S":
                                    signals.Add(new MiscSignal(convertedTimeStamp, MiscSignal.MiscSignalType.ShowStart));
                                    break;
                                case "E":
                                    signals.Add(new MiscSignal(convertedTimeStamp, MiscSignal.MiscSignalType.ShowEnd));
                                    break;
                                case "M":
                                    signals.Add(new MiscSignal(convertedTimeStamp, MiscSignal.MiscSignalType.DisplayMessage));
                                    break;
                                default:
                                    signals.Add(new UnknownCommand(convertedTimeStamp, ((char)currentByteInt).ToString()));
                                    break;
                            }
                            break;
                        }
                    }
                }
                else switch (currentByteInt)
                {
                    case 36:
                        switch (commandMode)
                        {
                            case false when lastByteInt == 36:
                                commandMode = true;
                                currentCommand = "";
                                textCommandText = "";
                                break;
                            case true when lastByteInt != 36:
                                commandMode = false;
                                switch (currentCommand)
                                {
                                    case "T" or "1":
                                        signals.Add(new TextDisplay1Signal(convertedTimeStamp, textCommandText));
                                        break;
                                    case "2":
                                        signals.Add(new TextDisplay2Signal(convertedTimeStamp, textCommandText));
                                        break;
                                }

                                break;
                        }

                        break;
                    case >= 48 and <= 63:
                    {
                        var turnOn = currentByteInt % 2 == 1;
                        var selectedCard = (currentByteInt - 48) / 2;
                        
                        signals.Add(new CardSelectSignal(convertedTimeStamp, (Card)selectedCard, turnOn));
                        break;
                    }
                    case >= 64 and <= 79:
                        signals.Add(new BitSetSignal(convertedTimeStamp, (ushort)(currentByteInt - 64)));
                        break;
                    default:
                        signals.Add(new UnknownSignal(convertedTimeStamp, (char)currentByteInt));
                        break;
                }
            }

            lastByteInt = currentByteInt;
        }

        return signals;
    }
}