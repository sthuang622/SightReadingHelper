using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class PitchCalibrationService
{
    public PitchDetectionResult ApplyCalibration(
        PitchDetectionResult detectedPitch,
        CalibrationProfile? calibration)
    {
        if (!detectedPitch.HasPitch || calibration?.Notes.Count > 0 is not true)
        {
            return detectedPitch;
        }

        var micOffsetCents = GetCalibrationOffsetCents(detectedPitch, calibration);
        var correctedFrequencyHz = detectedPitch.FrequencyHz / Math.Pow(2d, micOffsetCents / 1200d);
        var correctedMidiNote = PitchMath.FrequencyToMidi(correctedFrequencyHz);

        return new PitchDetectionResult
        {
            HasPitch = true,
            FrequencyHz = correctedFrequencyHz,
            ClosestMidiNote = correctedMidiNote,
            ClosestNoteName = PitchMath.MidiToNoteName(correctedMidiNote),
            Confidence = detectedPitch.Confidence,
            Volume = detectedPitch.Volume
        };
    }

    private static double GetCalibrationOffsetCents(
        PitchDetectionResult detectedPitch,
        CalibrationProfile calibration)
    {
        var orderedNotes = calibration.Notes
            .OrderBy(note => note.MidiNote)
            .ToList();

        var baseStringCalibration = orderedNotes[0];

        foreach (var note in orderedNotes)
        {
            if (detectedPitch.ClosestMidiNote < note.MidiNote)
            {
                break;
            }

            baseStringCalibration = note;
        }

        return baseStringCalibration.CentsOffset;
    }
}
