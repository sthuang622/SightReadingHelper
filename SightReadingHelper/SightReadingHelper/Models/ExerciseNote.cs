namespace SightReadingHelper.Models;

public class ExerciseNote
{
    public int SequenceNumber { get; set; }

    public int MidiNote { get; set; }

    public string NoteName { get; set; } = string.Empty;

    public int DisplayMidiNote { get; set; }

    public string DisplayNoteName { get; set; } = string.Empty;

    public double FrequencyHz { get; set; }

    public double BeatDuration { get; set; } = 1;

    public int BaseNoteIndex { get; set; }

    public string BaseNoteName { get; set; } = string.Empty;
}
