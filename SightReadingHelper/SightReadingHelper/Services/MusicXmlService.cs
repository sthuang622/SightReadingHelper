using System.Text;
using System.Globalization;
using System.Xml.Linq;
using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class MusicXmlService
{
    public sealed record ParsedMusicXmlNote(int DisplayMidiNote, double BeatDuration);

    private static readonly Dictionary<string, int> StepOffsets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["C"] = 0,
        ["D"] = 2,
        ["E"] = 4,
        ["F"] = 5,
        ["G"] = 7,
        ["A"] = 9,
        ["B"] = 11
    };

    public string GenerateExerciseMusicXml(PracticeExercise exercise)
    {
        var divisions = 4;
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
                builder.AppendLine($"        <duration>{Math.Max(1, (int)Math.Round(note.BeatDuration * divisions))}</duration>");
                builder.AppendLine($"        <type>{GetNoteType(note.BeatDuration)}</type>");
                builder.AppendLine("      </note>");
            }

            builder.AppendLine("    </measure>");
        }

        builder.AppendLine("  </part>");
        builder.AppendLine("</score-partwise>");

        return builder.ToString();
    }

    public List<int> ParseDisplayMidiNotes(string musicXml)
    {
        return ParseNotes(musicXml)
            .Select(note => note.DisplayMidiNote)
            .ToList();
    }

    public List<ParsedMusicXmlNote> ParseNotes(string musicXml)
    {
        if (string.IsNullOrWhiteSpace(musicXml))
        {
            return [];
        }

        var document = XDocument.Parse(musicXml);
        var parsedNotes = new List<ParsedMusicXmlNote>();
        var divisions = 1d;

        foreach (var measure in document.Descendants().Where(element => element.Name.LocalName == "measure"))
        {
            var divisionText = measure
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "attributes")?
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "divisions")?
                .Value;

            if (double.TryParse(divisionText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDivisions) && parsedDivisions > 0)
            {
                divisions = parsedDivisions;
            }

            foreach (var note in measure.Elements().Where(element => element.Name.LocalName == "note"))
            {
                if (note.Elements().Any(child => child.Name.LocalName == "rest"))
                {
                    continue;
                }

                var midiNote = ParseNoteMidi(note);
                if (midiNote is null)
                {
                    continue;
                }

                parsedNotes.Add(new ParsedMusicXmlNote(
                    midiNote.Value,
                    ParseBeatDuration(note, divisions)));
            }
        }

        return parsedNotes;
    }

    private static int? ParseNoteMidi(XElement note)
    {
        var pitch = note.Elements().FirstOrDefault(element => element.Name.LocalName == "pitch");
        if (pitch is null)
        {
            return null;
        }

        var step = pitch.Elements().FirstOrDefault(element => element.Name.LocalName == "step")?.Value;
        var octaveText = pitch.Elements().FirstOrDefault(element => element.Name.LocalName == "octave")?.Value;

        if (string.IsNullOrWhiteSpace(step)
            || !StepOffsets.TryGetValue(step, out var stepOffset)
            || !int.TryParse(octaveText, out var octave))
        {
            return null;
        }

        var alterText = pitch.Elements().FirstOrDefault(element => element.Name.LocalName == "alter")?.Value;
        var alter = int.TryParse(alterText, out var parsedAlter)
            ? parsedAlter
            : 0;

        return ((octave + 1) * 12) + stepOffset + alter;
    }

    private static double ParseBeatDuration(XElement note, double divisions)
    {
        var durationText = note.Elements().FirstOrDefault(element => element.Name.LocalName == "duration")?.Value;
        if (double.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) && duration > 0)
        {
            return Math.Clamp(duration / divisions, 0.125d, 16d);
        }

        var type = note.Elements().FirstOrDefault(element => element.Name.LocalName == "type")?.Value;
        return type?.ToLowerInvariant() switch
        {
            "whole" => 4,
            "half" => 2,
            "quarter" => 1,
            "eighth" => 0.5d,
            "16th" => 0.25d,
            "32nd" => 0.125d,
            _ => 1
        };
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

    private static string GetNoteType(double beatDuration)
    {
        return beatDuration switch
        {
            >= 4 => "whole",
            >= 2 => "half",
            >= 1 => "quarter",
            >= 0.5d => "eighth",
            >= 0.25d => "16th",
            _ => "32nd"
        };
    }
}
