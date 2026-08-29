namespace SightReadingHelper.Models;

public class NoteCalibration
{
    public string NoteName { get; set; } = string.Empty;

    public int MidiNote { get; set; }

    public double ExpectedFrequencyHz { get; set; }

    public double MeasuredFrequencyHz { get; set; }

    public double CentsOffset { get; set; }
}
