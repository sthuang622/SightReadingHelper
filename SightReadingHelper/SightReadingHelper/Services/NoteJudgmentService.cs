using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class NoteJudgmentService
{
    public NoteJudgmentResult Judge(
        ExerciseNote expectedNote,
        PitchDetectionResult detectedPitch,
        int toleranceCents)
    {
        if (!detectedPitch.HasPitch)
        {
            return new NoteJudgmentResult
            {
                Judgment = NoteJudgmentType.NoNoteDetected,
                Message = "No note detected. Play the target note again.",
                ShouldAdvance = false
            };
        }

        var expectedDisplayName = PitchMath.ToDisplayNoteName(expectedNote.DisplayNoteName);
        var detectedDisplayName = PitchMath.ToDisplayNoteName(detectedPitch.ClosestNoteName);

        if (detectedPitch.ClosestMidiNote != expectedNote.MidiNote)
        {
            return new NoteJudgmentResult
            {
                Judgment = NoteJudgmentType.WrongNote,
                Message = $"Wrong note. Expected {expectedDisplayName}, heard {detectedDisplayName}.",
                DetectedNoteName = detectedPitch.ClosestNoteName,
                ShouldAdvance = false
            };
        }

        var centsDifference = PitchMath.CalculateCentsDifference(detectedPitch.FrequencyHz, expectedNote.FrequencyHz);

        if (Math.Abs(centsDifference) <= toleranceCents)
        {
            return new NoteJudgmentResult
            {
                Judgment = NoteJudgmentType.Correct,
                Message = $"Correct. {expectedDisplayName} is within {Math.Abs(centsDifference):0} cents.",
                DetectedNoteName = detectedPitch.ClosestNoteName,
                ShouldAdvance = true
            };
        }

        var judgment = centsDifference > 0 ? NoteJudgmentType.TooSharp : NoteJudgmentType.TooFlat;
        var direction = centsDifference > 0 ? "sharp" : "flat";

        return new NoteJudgmentResult
        {
            Judgment = judgment,
            Message = $"{expectedDisplayName} is {Math.Abs(centsDifference):0} cents too {direction}.",
            DetectedNoteName = detectedPitch.ClosestNoteName,
            ShouldAdvance = false
        };
    }

}
