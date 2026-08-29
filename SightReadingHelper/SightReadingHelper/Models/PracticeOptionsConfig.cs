namespace SightReadingHelper.Models;

public class PracticeOptionsConfig
{
    public List<int> ExerciseLengths { get; set; } = [5, 10, 20];

    public List<int> ToleranceCents { get; set; } = [50, 30, 15];

    public List<int> BeatTempoBpms { get; set; } = [60, 80, 100, 120];

    public List<string> MaxStringJumpLabels { get; set; } =
    [
        "Same string only",
        "Adjacent strings only",
        "Skip one string",
        "Any string"
    ];
}
