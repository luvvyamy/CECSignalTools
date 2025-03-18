using System.Collections;
using System.Text;
using Microsoft.VisualBasic;
using NAudio.Wave;

namespace CECSignalTools;

public static class SignalRshwConversor
{
    public static int[] ConvertCyberstarSignalsToRshwSignals(List<ISignal> cyberSignals)
    {
        var rshwSignals = new List<int>();
        
        var currentBitOffset = 0;
        var currentTimestamp = 0f;
        var activeBits = new HashSet<int>();
        var turnOnBit = false;
        foreach (var signal in cyberSignals)
        {
            if (signal.Timestamp >= currentTimestamp + 1 / 60)
            {
                while (currentTimestamp < signal.Timestamp)
                {
                    rshwSignals.Add(0);
                    foreach (var bit in activeBits)
                    {
                        rshwSignals.Add(bit);
                    }
                    
                    currentTimestamp += (1f / 60);
                }
            }

            if (signal is CardSelectSignal selectSignal)
            {
                currentBitOffset = (int)selectSignal.SelectedCard * 16;
                turnOnBit = selectSignal.TurnOn;
            }
            else if (signal is BitSetSignal bitSetSignal)
            {
                if (turnOnBit)
                    activeBits.Add(currentBitOffset + bitSetSignal.MovementBit + 1);
                else
                    activeBits.Remove(currentBitOffset + bitSetSignal.MovementBit + 1);
            }
        }

        return rshwSignals.ToArray();
    }

    public static void ConvertRshwSingalsToCyberstarSignals(int[] rshwSignals, string outputPath)
    {
        var positiveOne = new short[10]
        {
            6420, 9467, 6868, 8754, 7763,
            -6424, -9466, -6862, -8751, -7759,
        };
        var negativeOne = new short[10]
        {
            -6424, -9466, -6862, -8751, -7759,
            6420, 9467, 6868, 8754, 7763
        };
        var positiveZero = new short[10]
        {
            6420, 9467, 6868, 8754, 7763,
            7764, 8762, 6868, 9460, 6408,
        };
        var negativeZero = new short[10]
        {
            -6424, -9466, -6862, -8751, -7759,
            -7774, -8760, -6878, -9454, -6399
        };

        var samples = new List<short>();
        var samplePositive = true;
        var activeBits = new HashSet<int>();
        var lateBytes = new List<byte>();
        
        var newFrameIndexes = rshwSignals.Select((x, index) => new { x = x, index = index }).Where(x => x.x == 0)
            .Select(x => x.index).ToList();

        var frames = 0;
        for (var i = 0; i < newFrameIndexes.Count - 1; i++)
        {
            frames++;
            // Read the bits from the current frame
            var currentFrameBits = new HashSet<int>();
            
            // Get each bit from the current frame
            for (var j = newFrameIndexes[i] + 1; j < newFrameIndexes[i + 1]; j++)
                currentFrameBits.Add(rshwSignals[j]);
            
            // Get Changes
            var activateBits = currentFrameBits.Except(activeBits);
            var deactivateBits = activeBits.Except(currentFrameBits);
            
            // Convert Changes to bytes
            var frameBytes = lateBytes;
            lateBytes = new List<byte>();
            
            var currentCard = Card.None;
            foreach (var bit in activateBits)
            {
                    var bitCard = (Card)(bit / 16);
                    if (currentCard != bitCard)
                    {
                        frameBytes.Add((byte)(49 + (int)bitCard * 2));
                        currentCard = bitCard;
                    }

                    frameBytes.Add((byte)(63 + bit - (int)bitCard * 16));
            }
            
            currentCard = Card.None;
            foreach (var bit in deactivateBits)
            {
                    var bitCard = (Card)(bit / 16);
                    if (currentCard != bitCard)
                    {
                        frameBytes.Add((byte)(48 + (int)bitCard * 2));
                        currentCard = bitCard;
                    }

                    frameBytes.Add((byte)(63 + bit - (int)bitCard * 16));
            }
            
            // Convert bytes to samples
            var frameSampleCount = 0;
            
            foreach(var frameByte in frameBytes)
            {
                if (frameSampleCount > 68)
                {
                    lateBytes.Add(frameByte);
                    continue;
                }

                samples.AddRange(samplePositive ? positiveZero : negativeZero);
                samples.AddRange(samplePositive ? negativeZero : positiveZero);
                samples.AddRange(samplePositive ? positiveOne : negativeOne);
                frameSampleCount += 3;

                for (var j = 0; j < 8; j++)
                {
                    if ((frameByte & (1 << j)) != 0)
                    {
                        samples.AddRange(samplePositive ? positiveZero : negativeZero);
                        samplePositive = !samplePositive;
                    }
                    else
                        samples.AddRange(samplePositive ? positiveOne : negativeOne);
                    
                    frameSampleCount++;
                }
            }
            for (; frameSampleCount < 80; frameSampleCount++)
            {
                samples.AddRange(samplePositive ? positiveZero : negativeZero);
                samplePositive = !samplePositive;
            }

            // Change current active bits to repeat the process
            activeBits = currentFrameBits;
        }
        
        if (!samplePositive)
            samples.AddRange(negativeZero);
        samplePositive = true;
        
        using WaveFileWriter writer = new(outputPath + ".wav",
            WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 48000, 2, 192000, 4, 16));

        var buffer = new byte[samples.Count * 4];
        int bufferIndex = 0;

        foreach (var sample in samples)
        {
            var bytes = BitConverter.GetBytes(sample);
            buffer[bufferIndex++] = bytes[0];
            buffer[bufferIndex++] = bytes[1];
            buffer[bufferIndex++] = 0;
            buffer[bufferIndex++] = 0;
        }
        
        writer.Write(buffer, 0, buffer.Length);
        
        // Start and end commands
        
        var startString = Encoding.ASCII.GetBytes("$$A900     $$S     $$A900     $$TIVY-ISI-2025-MAR$     $$2CEC-CUSTOM-SHOW$");
        var endString = Encoding.ASCII.GetBytes("$$E     $$M     $$1INTERMEDIO$");
        
        

        using (WaveFileWriter startWriter = new(outputPath + "-start.wav",
                   WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 48000,
                       2, 192000, 4, 16)))
        {
            var startSamples = new List<short>();

            for (var i = 0; i < 2400; i++)
            {
                startSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                startSamples.AddRange(samplePositive ? negativeZero : positiveZero);
            }

            var sampleCount = 0;
            foreach (var startByte in startString)
            {
                if (startByte == (byte)' ')
                {
                    for (var i = 0; i < 50; i++)
                    {
                        startSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                        startSamples.AddRange(samplePositive ? negativeZero : positiveZero);
                        sampleCount += 20;
                    }

                    continue;
                }

                startSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                startSamples.AddRange(samplePositive ? negativeZero : positiveZero);
                startSamples.AddRange(samplePositive ? positiveOne : negativeOne);
                sampleCount += 30;

                for (var i = 0; i < 8; i++)
                {
                    if ((startByte & (1 << i)) != 0)
                    {
                        startSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                        samplePositive = !samplePositive;
                    }
                    else
                        startSamples.AddRange(samplePositive ? positiveOne : negativeOne);

                    sampleCount++;
                }
            }
            
            for (var i = 0; i < 9600 * 4 - sampleCount; i++)
            {
                startSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                samplePositive = !samplePositive;
            }

            if (!samplePositive)
                startSamples.AddRange(negativeZero);
            samplePositive = true;

            buffer = new byte[startSamples.Count * 4];
            bufferIndex = 0;

            foreach (var sample in startSamples)
            {
                var bytes = BitConverter.GetBytes(sample);
                buffer[bufferIndex++] = bytes[0];
                buffer[bufferIndex++] = bytes[1];
                buffer[bufferIndex++] = 0;
                buffer[bufferIndex++] = 0;
            }

            startWriter.Write(buffer, 0, buffer.Length);
        }
        
        using (WaveFileWriter endWriter = new(outputPath + "-end.wav",
                   WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 48000,
                       2, 192000, 4, 16)))
        {
            var endSamples = new List<short>();

            for (var i = 0; i < 2400; i++)
            {
                endSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                endSamples.AddRange(samplePositive ? negativeZero : positiveZero);
            }

            var sampleCount = 0;
            foreach (var startByte in endString)
            {
                if (startByte == (byte)' ')
                {
                    for (var i = 0; i < 50; i++)
                    {
                        endSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                        endSamples.AddRange(samplePositive ? negativeZero : positiveZero);
                        sampleCount += 20;
                    }

                    continue;
                }

                endSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                endSamples.AddRange(samplePositive ? negativeZero : positiveZero);
                endSamples.AddRange(samplePositive ? positiveOne : negativeOne);
                sampleCount += 30;

                for (var i = 0; i < 8; i++)
                {
                    if ((startByte & (1 << i)) != 0)
                    {
                        endSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                        samplePositive = !samplePositive;
                    }
                    else
                        endSamples.AddRange(samplePositive ? positiveOne : negativeOne);

                    sampleCount++;
                }
            }
            
            for (var i = 0; i < 9600 * 4 - sampleCount; i++)
            {
                endSamples.AddRange(samplePositive ? positiveZero : negativeZero);
                samplePositive = !samplePositive;
            }

            if (!samplePositive)
                endSamples.AddRange(negativeZero);
            samplePositive = true;

            buffer = new byte[endSamples.Count * 4];
            bufferIndex = 0;

            foreach (var sample in endSamples)
            {
                var bytes = BitConverter.GetBytes(sample);
                buffer[bufferIndex++] = bytes[0];
                buffer[bufferIndex++] = bytes[1];
                buffer[bufferIndex++] = 0;
                buffer[bufferIndex++] = 0;
            }

            endWriter.Write(buffer, 0, buffer.Length);
        }


    }
    
    // https://stackoverflow.com/questions/283456/byte-array-pattern-search
    public static int RIFFAt(byte[] source)
    {
        var riffBytes = Encoding.ASCII.GetBytes("RIFF");
        for (int i = 0; i < source.Length; i++)
        {
            if (source.Skip(i).Take(4).SequenceEqual(riffBytes))
            {
                return i;
            }
        }

        return -1;
    }
}