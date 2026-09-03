namespace SightReadingHelper.Models;

public class PracticeSettings
{
    public string InstrumentName { get; set; } = "Viola";

    public int ExerciseLength { get; set; } = 10;

    public int ToleranceCents { get; set; } = 50;

    public int BeatTempoBpm { get; set; } = 60;

    public List<double> GeneratedBeatDurations { get; set; } = [1];

    public int? MaxBaseNoteJump { get; set; } = 1;

    public List<string> AllowedBaseNoteNames { get; set; } = [];

    public bool UseBeginnerRange { get; set; } = true;

    public bool AllowSharps { get; set; } = true;

    public bool AvoidOpenStringSharps { get; set; } = true;

    public bool ShowNoteName { get; set; } = false;

    public bool UseCustomMusicXmlLoop { get; set; } = false;

    public string CustomMusicXml { get; set; } = string.Empty;
}
