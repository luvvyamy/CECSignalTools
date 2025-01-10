namespace CECSignalTools;

public interface ISignal
{
    public double Timestamp { get; }
}

public class CardSelectSignal(double timestamp, Card selectedCard, bool turnOn) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public Card SelectedCard { get; } = selectedCard;
    public bool TurnOn { get; } = turnOn;
}

public class BitSetSignal(double timestamp, ushort movementBit) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public ushort MovementBit { get; } = movementBit;
}

public class TextDisplay1Signal(double timestamp, string text) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public string Text { get; } = text;
}

public class TextDisplay2Signal(double timestamp, string text) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public string Text { get; } = text;
}

public class MiscSignal(double timestamp, MiscSignal.MiscSignalType signalType) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public MiscSignalType SignalType { get; } = signalType;

    public enum MiscSignalType
    {
        ShowStart,
        A900,
        ShowEnd,
        DisplayMessage
    }
}

public class UnknownSignal(double timestamp, char signal) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public char Signal { get; } = signal;
}

public class UnknownCommand(double timestamp, string command) : ISignal
{
    public double Timestamp { get; } = timestamp;
    public string Command { get; } = command;
}