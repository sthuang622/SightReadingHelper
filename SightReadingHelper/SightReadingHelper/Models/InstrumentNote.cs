namespace SightReadingHelper.Models;

public class InstrumentNote
{
    public string NoteName { get; set; } = string.Empty;

    public int MidiNote { get; set; }

    public double FrequencyHz { get; set; }
}
