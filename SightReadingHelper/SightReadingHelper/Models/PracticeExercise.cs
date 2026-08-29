namespace SightReadingHelper.Models;

public class PracticeExercise
{
    public string InstrumentName { get; set; } = string.Empty;

    public string Clef { get; set; } = string.Empty;

    public int SoundingTransposeSemitones { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public List<ExerciseNote> Notes { get; set; } = [];

    public string MusicXml { get; set; } = string.Empty;
}
