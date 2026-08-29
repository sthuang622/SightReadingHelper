using System.Text;
using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class MusicXmlService
{
    public string GenerateExerciseMusicXml(PracticeExercise exercise)
    {
        var divisions = 1;
        var beatsPerMeasure = 4;
        var beatType = 4;
        var measureCount = (int)Math.Ceiling(exercise.Notes.Count / (double)beatsPerMeasure);
        var builder = new StringBuilder();

        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
        builder.AppendLine("<score-partwise version=\"3.1\">");
        builder.AppendLine("  <part-list>");
        builder.AppendLine("    <score-part id=\"P1\">");
        builder.AppendLine($"      <part-name>{exercise.InstrumentName}</part-name>");
        builder.AppendLine("    </score-part>");
        builder.AppendLine("  </part-list>");
        builder.AppendLine("  <part id=\"P1\">");

        for (var measureIndex = 0; measureIndex < measureCount; measureIndex++)
        {
            builder.AppendLine($"    <measure number=\"{measureIndex + 1}\">");

            if (measureIndex == 0)
            {
                builder.AppendLine("      <attributes>");
                builder.AppendLine($"        <divisions>{divisions}</divisions>");
                builder.AppendLine("        <key><fifths>0</fifths></key>");
                builder.AppendLine($"        <time><beats>{beatsPerMeasure}</beats><beat-type>{beatType}</beat-type></time>");
                builder.AppendLine($"        {GetClefXml(exercise.Clef)}");
                builder.AppendLine("      </attributes>");
            }

            foreach (var note in exercise.Notes.Skip(measureIndex * beatsPerMeasure).Take(beatsPerMeasure))
            {
                builder.AppendLine("      <note>");
                builder.AppendLine($"        {GetPitchXml(note.DisplayMidiNote)}");
                builder.AppendLine("        <duration>1</duration>");
                builder.AppendLine("        <type>quarter</type>");
                builder.AppendLine("      </note>");
            }

            builder.AppendLine("    </measure>");
        }

        builder.AppendLine("  </part>");
        builder.AppendLine("</score-partwise>");

        return builder.ToString();
    }

    private static string GetClefXml(string clef)
    {
        return clef.ToLowerInvariant() switch
        {
            "alto clef" => "<clef><sign>C</sign><line>3</line></clef>",
            "bass clef" => "<clef><sign>F</sign><line>4</line></clef>",
            _ => "<clef><sign>G</sign><line>2</line></clef>"
        };
    }

    private static string GetPitchXml(int midiNote)
    {
        var noteIndex = ((midiNote % 12) + 12) % 12;
        var octave = (midiNote / 12) - 1;
        var names = new (string Step, int Alter)[]
        {
            ("C", 0), ("C", 1), ("D", 0), ("D", 1), ("E", 0), ("F", 0),
            ("F", 1), ("G", 0), ("G", 1), ("A", 0), ("A", 1), ("B", 0)
        };
        var pitch = names[noteIndex];

        return pitch.Alter == 0
            ? $"<pitch><step>{pitch.Step}</step><octave>{octave}</octave></pitch>"
            : $"<pitch><step>{pitch.Step}</step><alter>{pitch.Alter}</alter><octave>{octave}</octave></pitch>";
    }
}
