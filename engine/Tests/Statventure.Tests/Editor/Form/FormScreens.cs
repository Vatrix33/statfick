using System.Numerics;
using Statventure.Editor.Form;
using Statventure.Records;
using Statventure.Tests.Records;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// The form's screens against Chromium (P36): the form with every control kind and a problem (a <c>test.kinds</c>
/// record, its required text taken out), in two halves (the pane scrolled for the second), and the JSON tab of an
/// instrument of the repo's audio.pak.
/// </summary>
public sealed class FormScreens : IEditorFixtures
{
    /// <summary>The form fixtures' height (a window Chromium's capture harness takes at 1.65 too).</summary>
    public const int ControlsHeight = 1400;

    /// <summary>How far the form pane is scrolled for the sample's second half (less than its whole scroll).</summary>
    public const int SecondHalf = 1020;

    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("form-controls", 1280, ControlsHeight, Controls),
        new("form-controls-end", 1280, ControlsHeight, editor =>
        {
            Controls(editor);
            editor.Document.UpdateLayout();
            var pane = editor.View.Pane(Statventure.Editor.Shell.EditorPane.Form);
            pane.ScrollTop = SecondHalf;
            // Short of the end, so the offset is the same wherever the form's height differs by a pixel.
            Assert.Equal(SecondHalf, pane.ScrollTop);
            Assert.True(pane.ScrollHeight - pane.ClientHeight > SecondHalf + 4);
        }),
        new("form-json", 1280, 800, Json),
    ];

    /// <summary>A <c>test.kinds</c> record with a value in every kind of field and an element or entry in a few lists and maps.</summary>
    public static Kinds Sample() => new()
    {
        Flag = true, Int8 = -12, UInt8 = 40, Int16 = 300, UInt16 = 6000, Int32 = -70000, UInt32 = 4000000000, Int64 = -9000000000,
        UInt64 = 18000000000000000000, Float32 = 0.25f, Float64 = 3.14159, Text = "Hello", Note = "#d77a50", Count = 3, Ratio = 0.75,
        Shade = Shade.Dark, Huge = Huge.Big, Bytes = [1, 2, 3, 4], Ints = [2, -3], Doubles = [0.5], Bools = [true],
        V2 = new(1, 2), V3 = new(0.84f, 0.48f, 0.31f), V4 = new(0.2f, 0.4f, 0.6f, 1), Points = [Vector3.Zero, new(1, 2, 3)],
        Words = ["a"], Names = ["Grim"], Jagged = [[1, 2]], Scores = new() { ["gold"] = 3 }, Pairs = [new("arm", "hand")],
        ByShade = new() { [Shade.Pale] = 0.5 }, Part = new Part { Name = "Grim", Weight = 2 }, Joint = new(1, 2), Point = new(1, 2),
        Shape = new Circle { Radius = 2 },
    };

    static void Controls(EditorHarness editor)
    {
        editor.Session.Types = FormTesting.Types;
        editor.Session.Writable = true;
        editor.Session.ShowEntries([FormTesting.Entry("kinds/sample", Sample())]);
        var entry = editor.Session.Entries[0];
        editor.Session.Select(entry);
        // A problem: the required text not given.
        editor.Session.Commit(entry, entry.Draft!.With("text", DraftValue.Missing));
    }

    static void Json(EditorHarness editor)
    {
        editor.Session.Writable = true;
        var path = "audio/instruments/harp";
        editor.Session.ShowEntries([FormTesting.Entry(path, FormTesting.AudioPack()[path])]);
        editor.Session.Select(editor.Session.Entries[0]);
        editor.Form().ExportPicker = new ScratchExportPicker();
        editor.Form().Tab = FormTab.Json;
    }
}
