using SightReadingHelper.Models;
using SightReadingHelper.Services;

namespace SightReadingHelper.Controls;

public class StaffNotationDrawable : IDrawable
{
    private const float StaffTop = 78;
    private const float LineSpacing = 14;
    private const float NoteSpacing = 58;
    private const float FirstNoteX = 82;
    private const float NoteWidth = 21;
    private const float NoteHeight = 14;

    public IReadOnlyList<ExerciseNote> Notes { get; set; } = [];

    public int CurrentNoteIndex { get; set; }

    public int? GhostMidiNote { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#FFFDF8");
        canvas.FillRectangle(dirtyRect);

        DrawCurrentNoteHighlight(canvas);
        DrawStaff(canvas, dirtyRect);
        DrawAltoClef(canvas);

        for (var index = 0; index < Notes.Count; index++)
        {
            DrawNote(canvas, Notes[index], index, index < CurrentNoteIndex);
        }

        DrawGhostNote(canvas);
    }

    public static double GetRequiredWidth(int noteCount)
    {
        return Math.Max(360, FirstNoteX + (noteCount * NoteSpacing) + 60);
    }

    private static void DrawStaff(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Color.FromArgb("#26231E");
        canvas.StrokeSize = 1.4f;

        for (var line = 0; line < 5; line++)
        {
            var y = StaffTop + (line * LineSpacing);
            canvas.DrawLine(20, y, dirtyRect.Width - 20, y);
        }
    }

    private static void DrawAltoClef(ICanvas canvas)
    {
        canvas.FontColor = Color.FromArgb("#26231E");
        canvas.FontSize = 42;
        canvas.DrawString("C", 28, StaffTop - 11, 34, 60, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.StrokeColor = Color.FromArgb("#26231E");
        canvas.StrokeSize = 2;
        canvas.DrawLine(57, StaffTop + (LineSpacing * 2), 70, StaffTop + (LineSpacing * 2));
        canvas.DrawLine(63, StaffTop + LineSpacing, 63, StaffTop + (LineSpacing * 3));
    }

    private void DrawCurrentNoteHighlight(ICanvas canvas)
    {
        if (CurrentNoteIndex < 0 || CurrentNoteIndex >= Notes.Count)
        {
            return;
        }

        var x = FirstNoteX + (CurrentNoteIndex * NoteSpacing);

        canvas.FillColor = Color.FromRgba(245, 211, 111, 90);
        canvas.FillRoundedRectangle(x - 18, StaffTop - 24, 46, (LineSpacing * 4) + 48, 12);
    }

    private static void DrawNote(ICanvas canvas, ExerciseNote note, int index, bool isComplete)
    {
        var x = FirstNoteX + (index * NoteSpacing);
        var staffPosition = GetAltoStaffPosition(note.MidiNote);
        var y = StaffTop + (LineSpacing * 2) - (staffPosition * (LineSpacing / 2));

        DrawLedgerLines(canvas, x, staffPosition);
        DrawAccidental(canvas, note.MidiNote, x, y);

        canvas.FillColor = isComplete ? Color.FromArgb("#1F5C4A") : Color.FromArgb("#26231E");
        canvas.StrokeColor = Color.FromArgb("#26231E");
        canvas.StrokeSize = 1.5f;
        canvas.SaveState();
        canvas.Rotate(-18, x, y);
        canvas.FillEllipse(x - (NoteWidth / 2), y - (NoteHeight / 2), NoteWidth, NoteHeight);
        canvas.RestoreState();

        canvas.StrokeColor = isComplete ? Color.FromArgb("#1F5C4A") : Color.FromArgb("#26231E");
        canvas.StrokeSize = 2;
        canvas.DrawLine(x + 9, y, x + 9, y - 46);
    }

    private void DrawGhostNote(ICanvas canvas)
    {
        if (GhostMidiNote is null || CurrentNoteIndex < 0 || CurrentNoteIndex >= Notes.Count)
        {
            return;
        }

        var x = FirstNoteX + (CurrentNoteIndex * NoteSpacing);
        var staffPosition = GetAltoStaffPosition(GhostMidiNote.Value);
        var y = StaffTop + (LineSpacing * 2) - (staffPosition * (LineSpacing / 2));
        var isOnTarget = GhostMidiNote.Value == Notes[CurrentNoteIndex].MidiNote;

        DrawLedgerLines(canvas, x, staffPosition);
        DrawAccidental(canvas, GhostMidiNote.Value, x, y);

        if (isOnTarget)
        {
            DrawGhostMatchGlow(canvas, x, y);
        }

        canvas.StrokeColor = isOnTarget
            ? Color.FromRgba(247, 180, 43, 230)
            : Color.FromRgba(31, 92, 74, 180);
        canvas.StrokeSize = isOnTarget ? 3.2f : 2.5f;
        canvas.FillColor = isOnTarget
            ? Color.FromRgba(247, 180, 43, 92)
            : Color.FromRgba(31, 92, 74, 72);
        canvas.SaveState();
        canvas.Rotate(-18, x, y);
        canvas.FillEllipse(x - (NoteWidth / 2), y - (NoteHeight / 2), NoteWidth, NoteHeight);
        canvas.DrawEllipse(x - (NoteWidth / 2), y - (NoteHeight / 2), NoteWidth, NoteHeight);
        canvas.RestoreState();
    }

    private static void DrawGhostMatchGlow(ICanvas canvas, float x, float y)
    {
        canvas.FillColor = Color.FromRgba(247, 180, 43, 44);
        canvas.FillEllipse(x - 22, y - 22, 44, 44);

        canvas.StrokeColor = Color.FromRgba(247, 180, 43, 170);
        canvas.StrokeSize = 2;
        canvas.DrawEllipse(x - 18, y - 18, 36, 36);

        canvas.StrokeColor = Color.FromRgba(247, 180, 43, 135);
        canvas.StrokeSize = 1.6f;
        canvas.DrawLine(x, y - 34, x, y - 24);
        canvas.DrawLine(x, y + 24, x, y + 34);
        canvas.DrawLine(x - 34, y, x - 24, y);
        canvas.DrawLine(x + 24, y, x + 34, y);
    }

    private static void DrawLedgerLines(ICanvas canvas, float x, int staffPosition)
    {
        canvas.StrokeColor = Color.FromArgb("#26231E");
        canvas.StrokeSize = 1.3f;

        for (var position = 6; position <= staffPosition; position += 2)
        {
            var y = StaffTop + (LineSpacing * 2) - (position * (LineSpacing / 2));
            canvas.DrawLine(x - 17, y, x + 17, y);
        }

        for (var position = -6; position >= staffPosition; position -= 2)
        {
            var y = StaffTop + (LineSpacing * 2) - (position * (LineSpacing / 2));
            canvas.DrawLine(x - 17, y, x + 17, y);
        }
    }

    private static void DrawAccidental(ICanvas canvas, int midiNote, float x, float y)
    {
        if (!PitchMath.MidiToNoteName(midiNote).Contains('#', StringComparison.Ordinal))
        {
            return;
        }

        canvas.FontColor = Color.FromArgb("#26231E");
        canvas.FontSize = 24;
        canvas.DrawString("#", x - 33, y - 16, 20, 32, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private static int GetAltoStaffPosition(int midiNote)
    {
        var noteName = PitchMath.MidiToNoteName(midiNote);
        var step = noteName[0];
        var octaveText = noteName.Contains('#', StringComparison.Ordinal)
            ? noteName[2..]
            : noteName[1..];
        var octave = int.Parse(octaveText);

        return GetDiatonicIndex(step, octave) - GetDiatonicIndex('C', 4);
    }

    private static int GetDiatonicIndex(char step, int octave)
    {
        var stepOffset = step switch
        {
            'C' => 0,
            'D' => 1,
            'E' => 2,
            'F' => 3,
            'G' => 4,
            'A' => 5,
            'B' => 6,
            _ => 0
        };

        return (octave * 7) + stepOffset;
    }
}
