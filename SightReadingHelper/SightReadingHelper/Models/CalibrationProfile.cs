namespace SightReadingHelper.Models;

public class CalibrationProfile
{
    public string InstrumentName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public List<NoteCalibration> Notes { get; set; } = [];
}
