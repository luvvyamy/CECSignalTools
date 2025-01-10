using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CECSignalTools;

// Code from the RR Engine
[System.Serializable]
public class rshwFormat
{
    public byte[] audioData { get; set; }
    public int[] signalData { get; set; }
    public byte[] videoData { get; set; }

    public void Save(string filePath)
    {
        var formatter = new BinaryFormatter { Binder = new CustomBinder() };
        using var stream = File.Open(filePath, FileMode.Create);
        formatter.Serialize(stream, this);
        Console.WriteLine(stream.Length);
    }

    public byte[] ConvertToBytes()
    {
        var formatter = new BinaryFormatter { Binder = new CustomBinder() };
        using (var stream = new MemoryStream())
        {
            formatter.Serialize(stream, this);
            return stream.ToArray();
        }
    }
    public static rshwFormat ReadFromFile(string filepath)
    {
        var formatter = new BinaryFormatter { Binder = new CustomBinder() };
        using (var stream = File.OpenRead(filepath))
        {
            if (stream.Length != 0)
            {
                stream.Position = 0;
                try
                {
                    return (rshwFormat)formatter.Deserialize(stream);
                }
                catch (System.Exception error)
                {
                    Console.WriteLine("ERROR READING FILE!");
                    Console.WriteLine(error);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}

internal class CustomBinder : SerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        if (typeName == "rshwFormat")
        {
            return typeof(rshwFormat);
        }
        return Type.GetType($"{typeName}, {assemblyName}");
    }
}