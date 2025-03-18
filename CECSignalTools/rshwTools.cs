using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CECSignalTools;

// Code from the RR Engine
[System.Serializable]
public class rshwFormat
{
    public rshwFormat.RRMetaData MetaData { get; set; }

    // Token: 0x17000059 RID: 89
    // (get) Token: 0x0600031F RID: 799 RVA: 0x00004758 File Offset: 0x00002958
    // (set) Token: 0x06000320 RID: 800 RVA: 0x00004760 File Offset: 0x00002960
    public bool NewRshw { get; set; }

    // Token: 0x1700005A RID: 90
    // (get) Token: 0x06000321 RID: 801 RVA: 0x00004769 File Offset: 0x00002969
    // (set) Token: 0x06000322 RID: 802 RVA: 0x00004771 File Offset: 0x00002971
    public byte[] audioData { get; set; }

    // Token: 0x1700005B RID: 91
    // (get) Token: 0x06000323 RID: 803 RVA: 0x0000477A File Offset: 0x0000297A
    // (set) Token: 0x06000324 RID: 804 RVA: 0x00004782 File Offset: 0x00002982
    public int[] signalData { get; set; }

    // Token: 0x1700005C RID: 92
    // (get) Token: 0x06000325 RID: 805 RVA: 0x0000478B File Offset: 0x0000298B
    // (set) Token: 0x06000326 RID: 806 RVA: 0x00004793 File Offset: 0x00002993
    public rshwFormat.ExistingStageRR[] ExistingStage { get; set; }

    // Token: 0x1700005D RID: 93
    // (get) Token: 0x06000327 RID: 807 RVA: 0x0000479C File Offset: 0x0000299C
    // (set) Token: 0x06000328 RID: 808 RVA: 0x000047A4 File Offset: 0x000029A4
    public rshwFormat.StudioCRR[] StudioC { get; set; }

    // Token: 0x1700005E RID: 94
    // (get) Token: 0x06000329 RID: 809 RVA: 0x000047AD File Offset: 0x000029AD
    // (set) Token: 0x0600032A RID: 810 RVA: 0x000047B5 File Offset: 0x000029B5
    public bool StudioCConversion { get; set; }

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
    
    [Serializable]
	public struct RRMetaData
	{
		// Token: 0x040004AF RID: 1199
		public string showtapeName;

		// Token: 0x040004B0 RID: 1200
		public int stageType;

		// Token: 0x040004B1 RID: 1201
		public int subType;

		// Token: 0x040004B2 RID: 1202
		public float framerate;
	}

	// Token: 0x020000A7 RID: 167
	[Serializable]
	public struct ExistingStageRR
	{
		// Token: 0x040004B3 RID: 1203
		public int[] birthdayData;

		// Token: 0x040004B4 RID: 1204
		public rshwFormat.LCDData[] LCD;

		// Token: 0x040004B5 RID: 1205
		public rshwFormat.MarkerData[] Markers;
	}

	// Token: 0x020000A8 RID: 168
	[Serializable]
	public struct StudioCRR
	{
		// Token: 0x040004B6 RID: 1206
		public rshwFormat.StudioCMetaData SCMetaData;

		// Token: 0x040004B7 RID: 1207
		public int[] StudiosignalData;

		// Token: 0x040004B8 RID: 1208
		public rshwFormat.DMX[] DMXData;

		// Token: 0x040004B9 RID: 1209
		public rshwFormat.MarkerData[] SCMarkers;
	}

	// Token: 0x020000A9 RID: 169
	[Serializable]
	public struct RshwSignals
	{
		// Token: 0x040004BA RID: 1210
		public int[] Signals;
	}

	// Token: 0x020000AA RID: 170
	[Serializable]
	public struct LCDData
	{
		// Token: 0x040004BB RID: 1211
		public int frameNumber;

		// Token: 0x040004BC RID: 1212
		public string title;

		// Token: 0x040004BD RID: 1213
		public string line1;

		// Token: 0x040004BE RID: 1214
		public string line2;
	}

	// Token: 0x020000AB RID: 171
	[Serializable]
	public struct MarkerData
	{
		// Token: 0x040004BF RID: 1215
		public int frameNumber;

		// Token: 0x040004C0 RID: 1216
		public string command;

		// Token: 0x040004C1 RID: 1217
		public string[] data;
	}

	// Token: 0x020000AC RID: 172
	[Serializable]
	public struct StudioCMetaData
	{
		// Token: 0x040004C2 RID: 1218
		public int sectionType;

		// Token: 0x040004C3 RID: 1219
		public string sectionName;
	}

	// Token: 0x020000AD RID: 173
	[Serializable]
	public struct DMX
	{
		// Token: 0x040004C4 RID: 1220
		public int frameNumber;

		// Token: 0x040004C5 RID: 1221
		public short channelNumber;

		// Token: 0x040004C6 RID: 1222
		public byte data;
	}
}

[Serializable]
public class rshwFile
{
	// Token: 0x17000054 RID: 84
	// (get) Token: 0x06000312 RID: 786 RVA: 0x00004703 File Offset: 0x00002903
	// (set) Token: 0x06000313 RID: 787 RVA: 0x0000470B File Offset: 0x0000290B
	public byte[] audioData { get; set; }

	// Token: 0x17000055 RID: 85
	// (get) Token: 0x06000314 RID: 788 RVA: 0x00004714 File Offset: 0x00002914
	// (set) Token: 0x06000315 RID: 789 RVA: 0x0000471C File Offset: 0x0000291C
	public int[] signalData { get; set; }

	// Token: 0x17000056 RID: 86
	// (get) Token: 0x06000316 RID: 790 RVA: 0x00004725 File Offset: 0x00002925
	// (set) Token: 0x06000317 RID: 791 RVA: 0x0000472D File Offset: 0x0000292D
	public float[] dmxData { get; set; }

	// Token: 0x17000057 RID: 87
	// (get) Token: 0x06000318 RID: 792 RVA: 0x00004736 File Offset: 0x00002936
	// (set) Token: 0x06000319 RID: 793 RVA: 0x0000473E File Offset: 0x0000293E
	public float[] avData { get; set; }

	// Token: 0x0600031A RID: 794 RVA: 0x0001F66C File Offset: 0x0001D86C
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
        switch (typeName)
        {
            case "rshwFormat":
                return typeof(rshwFormat);
            case "rshwFile":
                return typeof(rshwFile);
            case "rshwFormat+RRMetaData":
                return typeof(rshwFormat.RRMetaData);
            case "rshwFormat+ExistingStageRR":
                return typeof(rshwFormat.ExistingStageRR);
            case "rshwFormat+StudioCRR":
                return typeof(rshwFormat.StudioCRR);
            case "rshwFormat+RshwSignals":
                return typeof(rshwFormat.RshwSignals);
            case "rshwFormat+LCDData":
                return typeof(rshwFormat.LCDData);
            case "rshwFormat+MarkerData":
                return typeof(rshwFormat.MarkerData);
            case "rshwFormat+StudioCMetaData":
                return typeof(rshwFormat.StudioCMetaData);
            case "rshwFormat+DMX":
                return typeof(rshwFormat.DMX);
            default:
                return Type.GetType($"{typeName}, {assemblyName}");
        }
    }
}