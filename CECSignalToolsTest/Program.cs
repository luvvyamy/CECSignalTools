// See https://aka.ms/new-console-template for more information

using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using CECSignalTools;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

var file_name = args[1];
var mode = args[0];

if (mode == "convert-to-rshw")
{
    if (!File.Exists(file_name + ".cshw") || !File.Exists(file_name + ".wav"))
    {
        Console.WriteLine("Files do not exist");
        return;
    }
    
    Console.WriteLine("Converting...");

    var originalShow = rshwFormat.ReadFromFile(file_name + ".cshw");

    var originalRshwBytes = File.ReadAllBytes(file_name + ".cshw");
    var header = originalRshwBytes[0..SignalRshwConversor.RIFFAt(originalRshwBytes)];

    var cyberSignals = WavFileSignalReader.ReadSignalsFromWavFile(file_name + ".wav");

    var signals = SignalRshwConversor.ConvertCyberstarSignalsToRshwSignals(cyberSignals);

    var rshw = new rshwFormat
    {
        audioData = originalShow.audioData,
        signalData = signals
    };

    var convertedCshwBytes = rshw.ConvertToBytes();
    header = header.Concat(convertedCshwBytes.Skip(SignalRshwConversor.RIFFAt(convertedCshwBytes))).ToArray();
    File.WriteAllBytes(file_name + "-converted.cshw", header);
    Console.WriteLine($"Done! File saved to {file_name}-converted.cshw");
}
else if (mode == "convert-to-signals")
{
    if (!File.Exists(file_name + ".cshw"))
    {
        Console.WriteLine("File does not exist.");
        return;
    }

    var rshwFile = rshwFormat.ReadFromFile(file_name + ".cshw");

    SignalRshwConversor.ConvertRshwSingalsToCyberstarSignals(rshwFile.signalData, file_name);
    File.WriteAllBytes(file_name + "_audio.wav", rshwFile.audioData);
}


