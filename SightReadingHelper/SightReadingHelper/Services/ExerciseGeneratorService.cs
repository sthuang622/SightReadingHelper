using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class ExerciseGeneratorService
{
    private readonly MusicXmlService _musicXmlService;

    public ExerciseGeneratorService(MusicXmlService musicXmlService)
    {
        _musicXmlService = musicXmlService;
    }

    public PracticeExercise GenerateExercise(InstrumentProfile instrument, PracticeSettings settings)
    {
        var upperMidiNote = settings.UseBeginnerRange && instrument.BeginnerHighestMidiNote > 0
            ? instrument.BeginnerHighestMidiNote
            : instrument.HighestMidiNote;

        var notes = new List<ExerciseNote>();
        var allowedBaseNoteNames = new HashSet<string>(
            settings.AllowedBaseNoteNames ?? [],
            StringComparer.OrdinalIgnoreCase);

        var possibleNotes = Enumerable
            .Range(instrument.LowestMidiNote, upperMidiNote - instrument.LowestMidiNote + 1)
            .Select(midiNote => CreateExerciseNote(midiNote, 0, instrument))
            .Where(note => IsAllowedAccidental(note, instrument, settings))
            .Where(note => allowedBaseNoteNames.Count == 0 || allowedBaseNoteNames.Contains(note.BaseNoteName))
            .ToList();

        if (possibleNotes.Count == 0)
        {
            possibleNotes = Enumerable
                .Range(instrument.LowestMidiNote, upperMidiNote - instrument.LowestMidiNote + 1)
                .Select(midiNote => CreateExerciseNote(midiNote, 0, instrument))
                .Where(note => IsAllowedAccidental(note, instrument, settings))
                .ToList();
        }

        for (var index = 0; index < settings.ExerciseLength; index++)
        {
            var candidates = GetCandidatesForNextNote(possibleNotes, notes.LastOrDefault(), settings.MaxBaseNoteJump ?? 1);
            var selectedNote = candidates[Random.Shared.Next(candidates.Count)];
            selectedNote.SequenceNumber = index + 1;
            selectedNote.BeatDuration = GetGeneratedBeatDuration(settings);
            notes.Add(selectedNote);
        }

        var exercise = new PracticeExercise
        {
            InstrumentName = instrument.InstrumentName,
            Clef = instrument.DefaultClef,
            SoundingTransposeSemitones = instrument.SoundingTransposeSemitones,
            GeneratedAtUtc = DateTime.UtcNow,
            Notes = notes
        };

        exercise.MusicXml = _musicXmlService.GenerateExerciseMusicXml(exercise);
        return exercise;
    }

    public PracticeExercise GenerateMusicXmlLoop(InstrumentProfile instrument, string musicXml)
    {
        var importedNotes = _musicXmlService.ParseNotes(musicXml)
            .Where(note => note.DisplayMidiNote >= instrument.LowestMidiNote && note.DisplayMidiNote <= instrument.HighestMidiNote)
            .ToList();

        if (importedNotes.Count == 0)
        {
            throw new InvalidOperationException("No playable notes were found in the MusicXML for the selected instrument.");
        }

        var notes = importedNotes
            .Select((note, index) => CreateExerciseNote(note.DisplayMidiNote, index + 1, instrument, note.BeatDuration))
            .ToList();

        return new PracticeExercise
        {
            InstrumentName = instrument.InstrumentName,
            Clef = instrument.DefaultClef,
            SoundingTransposeSemitones = instrument.SoundingTransposeSemitones,
            GeneratedAtUtc = DateTime.UtcNow,
            Notes = notes,
            MusicXml = musicXml
        };
    }

    private static List<ExerciseNote> GetCandidatesForNextNote(
        IReadOnlyList<ExerciseNote> possibleNotes,
        ExerciseNote? previousNote,
        int maxBaseNoteJump)
    {
        if (previousNote is null)
        {
            return possibleNotes.Select(CloneNote).ToList();
        }

        var candidates = possibleNotes
            .Where(note => Math.Abs(note.BaseNoteIndex - previousNote.BaseNoteIndex) <= maxBaseNoteJump)
            .Select(CloneNote)
            .ToList();

        return candidates.Count > 0
            ? candidates
            : possibleNotes.Select(CloneNote).ToList();
    }

    private static ExerciseNote CreateExerciseNote(
        int midiNote,
        int sequenceNumber,
        InstrumentProfile instrument,
        double beatDuration = 1)
    {
        var baseNoteIndex = GetBaseNoteIndex(midiNote, instrument.BaseNotes);
        var baseNote = instrument.BaseNotes[baseNoteIndex];
        var soundingMidiNote = midiNote + instrument.SoundingTransposeSemitones;

        return new ExerciseNote
        {
            SequenceNumber = sequenceNumber,
            MidiNote = soundingMidiNote,
            NoteName = PitchMath.MidiToNoteName(soundingMidiNote),
            DisplayMidiNote = midiNote,
            DisplayNoteName = PitchMath.MidiToNoteName(midiNote),
            FrequencyHz = PitchMath.MidiToFrequency(soundingMidiNote),
            BeatDuration = beatDuration,
            BaseNoteIndex = baseNoteIndex,
            BaseNoteName = baseNote.NoteName
        };
    }

    private static ExerciseNote CloneNote(ExerciseNote note)
    {
        return new ExerciseNote
        {
            SequenceNumber = note.SequenceNumber,
            MidiNote = note.MidiNote,
            NoteName = note.NoteName,
            DisplayMidiNote = note.DisplayMidiNote,
            DisplayNoteName = note.DisplayNoteName,
            FrequencyHz = note.FrequencyHz,
            BeatDuration = note.BeatDuration,
            BaseNoteIndex = note.BaseNoteIndex,
            BaseNoteName = note.BaseNoteName
        };
    }

    private static double GetGeneratedBeatDuration(PracticeSettings settings)
    {
        var durations = settings.GeneratedBeatDurations
            .Where(duration => duration > 0)
            .ToList();

        return durations.Count == 0
            ? 1
            : durations[Random.Shared.Next(durations.Count)];
    }

    private static bool IsAllowedAccidental(
        ExerciseNote note,
        InstrumentProfile instrument,
        PracticeSettings settings)
    {
        if (!note.DisplayNoteName.Contains('#', StringComparison.Ordinal))
        {
            return true;
        }

        if (!settings.AllowSharps)
        {
            return false;
        }

        if (settings.UseBeginnerRange && IsBeginnerExcludedNote(note, instrument))
        {
            return false;
        }

        return !settings.AvoidOpenStringSharps || !IsOpenStringSharp(note, instrument);
    }

    private static bool IsOpenStringSharp(ExerciseNote note, InstrumentProfile instrument)
    {
        if (note.BaseNoteIndex < 0 || note.BaseNoteIndex >= instrument.BaseNotes.Count)
        {
            return false;
        }

        var baseNote = instrument.BaseNotes[note.BaseNoteIndex];
        return note.DisplayMidiNote == baseNote.MidiNote + 1;
    }

    private static bool IsBeginnerExcludedNote(ExerciseNote note, InstrumentProfile instrument)
    {
        if (note.BaseNoteIndex < 0 || note.BaseNoteIndex >= instrument.BaseNotes.Count)
        {
            return false;
        }

        var baseNote = instrument.BaseNotes[note.BaseNoteIndex];
        var semitonesAboveOpenString = note.DisplayMidiNote - baseNote.MidiNote;

        return instrument.BeginnerExcludedSemitoneOffsets.Contains(semitonesAboveOpenString);
    }

    private static int GetBaseNoteIndex(int midiNote, IReadOnlyList<InstrumentNote> baseNotes)
    {
        var orderedBaseNotes = baseNotes
            .Select((note, index) => new { note.MidiNote, Index = index })
            .OrderBy(note => note.MidiNote)
            .ToList();

        var baseNoteIndex = orderedBaseNotes[0].Index;

        foreach (var baseNote in orderedBaseNotes)
        {
            if (midiNote < baseNote.MidiNote)
            {
                break;
            }

            baseNoteIndex = baseNote.Index;
        }

        return baseNoteIndex;
    }
}
