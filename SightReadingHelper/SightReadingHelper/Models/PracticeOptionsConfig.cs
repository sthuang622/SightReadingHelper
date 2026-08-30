namespace SightReadingHelper.Models;

public class PracticeOptionsConfig
{
    public List<int> ExerciseLengths { get; set; } = [5, 10, 20];

    public List<int> ToleranceCents { get; set; } = [50, 30, 15];

    public List<int> BeatTempoBpms { get; set; } = [30, 60, 90, 120];

    public Dictionary<string, List<string>> MaxTuningNoteJumpLabelsByInstrumentType { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] =
        [
            "Same string only",
            "Adjacent strings only",
            "Skip one string",
            "Any string"
        ],
        ["brass"] =
        [
            "Same tuning note only",
            "Nearby tuning notes only",
            "Wider tuning-note jumps",
            "Any tuning note"
        ],
        ["woodwind"] =
        [
            "Same tuning note only",
            "Nearby tuning notes only",
            "Wider tuning-note jumps",
            "Any tuning note"
        ],
        ["percussion"] =
        [
            "Same target only",
            "Nearby targets only",
            "Wider target jumps",
            "Any target"
        ]
    };
}
