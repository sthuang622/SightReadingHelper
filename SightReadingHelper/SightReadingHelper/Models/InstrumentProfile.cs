namespace SightReadingHelper.Models;

public class InstrumentProfile
{
    public string InstrumentName { get; set; } = string.Empty;

    public string DefaultClef { get; set; } = string.Empty;

    public int LowestMidiNote { get; set; }

    public int HighestMidiNote { get; set; }

    public string LowestNoteName { get; set; } = string.Empty;

    public string HighestNoteName { get; set; } = string.Empty;

    public int BeginnerHighestMidiNote { get; set; }

    public string BeginnerHighestNoteName { get; set; } = string.Empty;

    public List<InstrumentNote> BaseNotes { get; set; } = [];
}
