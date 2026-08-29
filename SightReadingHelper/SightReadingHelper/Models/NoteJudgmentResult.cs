namespace SightReadingHelper.Models;

public class NoteJudgmentResult
{
    public NoteJudgmentType Judgment { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool ShouldAdvance { get; set; }

    public string DetectedNoteName { get; set; } = string.Empty;
}
