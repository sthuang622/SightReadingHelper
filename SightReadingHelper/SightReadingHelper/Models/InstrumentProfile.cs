namespace SightReadingHelper.Models;

public class InstrumentProfile
{
    public string InstrumentName { get; set; } = string.Empty;

    public string InstrumentType { get; set; } = "string";

    public string DefaultClef { get; set; } = string.Empty;

    public int SoundingTransposeSemitones { get; set; }

    public string BaseNoteGroupLabel { get; set; } = "tuning notes";

    public string BaseNoteItemSuffix { get; set; } = "tuning note";

    public int LowestMidiNote { get; set; }

    public int HighestMidiNote { get; set; }

    public string LowestNoteName { get; set; } = string.Empty;

    public string HighestNoteName { get; set; } = string.Empty;

    public int BeginnerHighestMidiNote { get; set; }

    public string BeginnerHighestNoteName { get; set; } = string.Empty;

    public List<int> BeginnerExcludedSemitoneOffsets { get; set; } = [];

    public CalibrationProfile? Calibration { get; set; }

    public List<InstrumentNote> BaseNotes { get; set; } = [];
}
