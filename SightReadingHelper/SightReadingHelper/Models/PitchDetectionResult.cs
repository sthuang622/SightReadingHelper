namespace SightReadingHelper.Models;

public class PitchDetectionResult
{
    public bool HasPitch { get; set; }

    public double FrequencyHz { get; set; }

    public int ClosestMidiNote { get; set; }

    public string ClosestNoteName { get; set; } = string.Empty;

    public double CentsDifference { get; set; }

    public double Confidence { get; set; }

    public double Volume { get; set; }
}
