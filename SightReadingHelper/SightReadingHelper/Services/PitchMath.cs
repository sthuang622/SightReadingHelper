using System.Globalization;

namespace SightReadingHelper.Services;

public static class PitchMath
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    public static double CalculateCentsDifference(double detectedFrequencyHz, double expectedFrequencyHz)
    {
        if (detectedFrequencyHz <= 0 || expectedFrequencyHz <= 0)
        {
            return 0;
        }

        return 1200d * Math.Log2(detectedFrequencyHz / expectedFrequencyHz);
    }

    public static double MidiToFrequency(int midiNote)
    {
        return 440d * Math.Pow(2d, (midiNote - 69) / 12d);
    }

    public static int FrequencyToMidi(double frequencyHz)
    {
        return (int)Math.Round(69d + (12d * Math.Log2(frequencyHz / 440d)));
    }

    public static string MidiToNoteName(int midiNote)
    {
        var noteIndex = ((midiNote % 12) + 12) % 12;
        var octave = (midiNote / 12) - 1;
        return string.Create(CultureInfo.InvariantCulture, $"{NoteNames[noteIndex]}{octave}");
    }

    public static string ToDisplayNoteName(int midiNote, bool includeOctave = false)
    {
        return ToDisplayNoteName(MidiToNoteName(midiNote), includeOctave);
    }

    public static string ToDisplayNoteName(string noteName, bool includeOctave = false)
    {
        if (string.IsNullOrWhiteSpace(noteName))
        {
            return string.Empty;
        }

        var octaveStart = noteName.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
        var pitchClass = octaveStart < 0 ? noteName : noteName[..octaveStart];
        var octave = octaveStart < 0 ? string.Empty : noteName[octaveStart..];
        var displayName = pitchClass.Replace("#", "♯", StringComparison.Ordinal);

        return includeOctave ? $"{displayName}{octave}" : displayName;
    }

    public static string ToStringLabel(string noteName)
    {
        return $"{ToDisplayNoteName(noteName)} string";
    }
}
