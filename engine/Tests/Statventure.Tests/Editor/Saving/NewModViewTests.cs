using Statventure.Audio;
using Statventure.Editor.Saving;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using static Statventure.Tests.Editor.Checks.CheckTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// The New mod dialog (M11-5, M11-15, P25, Q2; Docs/Editor.md, "How it works", step 11): the name focused, the file name
/// following it until edited by hand, what is wrong on its line, that mods load in file-name order; Create (or Enter)
/// asks about unsaved work, then writes the mod into the player's mods folder, opens it and selects its info.
/// </summary>
public class NewModViewTests
{
    [Fact]
    public void NewModAsksTheNamesThenMakesTheModAndSelectsItsInfo()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        editor.Click(editor.View.Button(EditorCommand.NewMod)!);
        var dialog = editor.Saving().NewModShown!;
        Assert.Equal("New mod", editor.Asked().Title);
        Assert.Contains("Mods load in file-name order: where two mods hold the same entry, the one whose file name comes later wins.", editor.Asked().Text);
        Assert.Same(dialog.NameField, editor.Document.FocusedElement);
        Assert.Equal(["Create", "Cancel"], editor.Buttons());
        editor.TypeKeys("Brighter Halls");
        Assert.Equal("brighter-halls.pak", dialog.FileField.Value);
        // Create with a name refused: said on its line, which typing clears.
        dialog.FileField.Value = "Brighter.pak";
        dialog.Act();
        Assert.Equal("That can't be the file's name: 'B' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/').", dialog.Problem);
        Assert.True(dialog.Dialog.IsOpen);
        editor.TypeKeys(" II");
        Assert.Equal("", dialog.Problem);
        Assert.Equal("brighter-halls-ii.pak", dialog.FileField.Value);
        // The file name edited by hand: the name no longer moves it.
        dialog.FileField.Focus();
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.TypeKeys("halls-2.pak");
        Assert.Equal("halls-2.pak", dialog.FileField.Value);
        dialog.NameField.Focus();
        editor.Key("End", "End");
        editor.TypeKeys("!");
        Assert.Equal("halls-2.pak", dialog.FileField.Value);
        // Enter creates.
        editor.Key("Enter", "Enter");
        Assert.False(dialog.Dialog.IsOpen);
        var file = scratch["player/mods/halls-2.pak"];
        Assert.Equal(file, editor.Session.Pack!.File);
        Assert.Equal(new ModInfo { Name = "Brighter Halls II!" }, editor.Session.At(ModInfo.Path).Saved);
        Assert.Equal("Made halls-2.pak in the mods folder.", editor.View.Status);
        Assert.Same(editor.Session.At(ModInfo.Path), editor.Session.Selected);
        Assert.Same(editor.ModInfoForm(), editor.View.Part<Statventure.Editor.Form.FormPart>().OwnForm);
        Assert.Equal("Statventure Editor dev — halls-2.pak", editor.View.Title);
        Assert.Null(editor.Saving().NewModShown);
    }

    [Fact]
    public void NewModOverUnsavedWorkAsksFirstAndCancelMakesNothing()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord("audio/effects/boom", Patch()));
        using var editor = scratch.Editor(mod);
        editor.Session.Change(editor.Session.At("audio/effects/boom"), "MaxVoices", 3);
        // Cancel in the dialog: nothing.
        var cancelled = editor.Run(() => editor.Saving().NewMod());
        editor.TypeKeys("Halls");
        editor.Answer("Cancel");
        Assert.Null(cancelled.Done());
        Assert.Equal(["my-mod.pak"], Directory.GetFiles(scratch.Mods).Select(Path.GetFileName));
        // Create over unsaved work: asked; Cancel there makes nothing either.
        var asked = editor.Run(() => editor.Saving().NewMod());
        editor.TypeKeys("Halls");
        editor.Answer("Create");
        Assert.Equal(("Unsaved changes", "my-mod.pak has changes that aren't saved. Discard them and make the mod halls.pak?"), editor.Asked());
        Assert.Equal(["Save", "Discard", "Cancel"], editor.Buttons());
        editor.Answer("Cancel");
        Assert.Null(asked.Done());
        Assert.False(File.Exists(scratch["player/mods/halls.pak"]));
        Assert.Equal("my-mod.pak", editor.Session.Pack!.Name);
        // Save, then the mod is made.
        var saved = editor.Run(() => editor.Saving().NewMod());
        editor.TypeKeys("Halls");
        editor.Answer("Create");
        editor.Answer("Save");
        Assert.Equal("halls.pak", saved.Done()!.Name);
        Assert.Equal((true, Binary(Patch() with { MaxVoices = 3 })), Contents(mod)["audio/effects/boom"]);
        // A mod of that name made meanwhile (between Create and the question): said, nothing replaced.
        editor.Session.Change(editor.Session.At(ModInfo.Path), "Author", "Me");
        var raced = editor.Run(() => editor.Saving().NewMod());
        editor.TypeKeys("Halls Two");
        editor.Answer("Create");
        var there = scratch.Mod(w => w.Add("notes/a.txt", "A"u8), "halls-two.pak");
        var bytes = File.ReadAllBytes(there);
        editor.Answer("Discard");
        Assert.Equal(("Can't make the mod", "There is already halls-two.pak in the mods folder."), editor.Asked());
        editor.Answer("OK");
        Assert.Null(raced.Done());
        Assert.Equal(bytes, File.ReadAllBytes(there));
        Assert.Equal("halls.pak", editor.Session.Pack!.Name);
    }
}
