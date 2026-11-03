using Statventure.Core;
using Statventure.Storage;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Core;

/// <summary>
/// The application's game folder (Docs/Assets.md, step 9; Q16, S9-B): <see cref="Application.Storage"/> for
/// the option's folder or this machine's, its mods mounted in the data, and what the game saved written before
/// <see cref="Application.Run"/> ends: after the game's Unload, and after a failed start or loop. In the
/// non-parallel "Storage log" collection, as the failing runs log errors.
/// </summary>
[Collection("Storage log")]
public class ApplicationStorageTests
{
    [Fact]
    public void TheGameFolderIsTheOptionsElseThisMachines()
    {
        Assert.Equal(GameFolder.ForThisMachine(), Application.StorageFolder(new ApplicationOptions()));
        Assert.Equal("/somewhere/else", Application.StorageFolder(new ApplicationOptions { GameFolder = "/somewhere/else" }));
    }

    [Fact]
    public void TheStorageIsTheGameFolderWhoseModsAreMounted()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        MakePack(folder["player/mods/mod.pak"], Entry("audio/x", "mod"), Entry("audio/y", "mod"));
        folder.Write("dev/audio/y", "development");
        GameStorage? storage = null;
        string[]? mounts = null;
        (string X, string Y)? read = null;
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, folder["player"]);
        var application = new TestApplication(new ApplicationOptions { GameFolder = relative, DevelopmentFolder = folder["dev"], Audio = false })
        {
            OnLoad = a =>
            {
                storage = a.Storage;
                mounts = [.. a.Data.Mounts.Select(m => $"{m.Kind} {m.Path}")];
                read = (a.Data.ReadText("audio/x"), a.Data.ReadText("audio/y"));
            },
        };
        var log = TestApplication.CaptureLog("Storage", application.Run);
        Assert.Equal(folder["player"], storage!.Folder);
        Assert.Equal($"Info: game folder {folder["player"]}", Assert.Single(log));
        // No packs or classes beside the test's executable: the mods, then the development folder.
        Assert.Equal([$"Mods {folder["player/mods"]}", $"Folder {folder["dev"]}"], mounts);
        Assert.Equal(("mod", "development"), read);
        // Gone once the application has ended, and nothing written in the folder.
        Assert.Null((object?)application.Storage);
        Assert.Equal(["mods"], Directory.GetFileSystemEntries(folder["player"]).Select(Path.GetFileName));
    }

    /// <summary>The writer takes a second for the save, so a Run that didn't wait for it would end before the file is there.</summary>
    [Theory]
    [InlineData("unload")]
    [InlineData("load")]
    [InlineData("update")]
    public void WhatTheGameSavedIsWrittenBeforeRunEnds(string when)
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        GameStorage? storage = null;
        void SaveSlowly(TestApplication a)
        {
            storage = a.Storage;
            a.Storage.Writer.BeforeWork = _ => Thread.Sleep(1000);
            a.Storage.Saves.Save("slot", new Part { Name = when });
        }
        var application = new TestApplication(new ApplicationOptions { GameFolder = folder["player"], Audio = false })
        {
            OnLoad = a =>
            {
                if (when != "load")
                    return;
                SaveSlowly(a);
                throw new InvalidOperationException("a start that fails");
            },
            OnUpdate = (a, _) =>
            {
                if (when != "update")
                    return false;
                SaveSlowly(a);
                throw new InvalidOperationException("a loop that fails");
            },
            OnUnload = a =>
            {
                if (when == "unload")
                    SaveSlowly(a);
            },
        };
        if (when == "unload")
            application.Run();
        else
            Assert.StartsWith("a ", Assert.Throws<InvalidOperationException>(application.Run).Message);
        Assert.True(File.Exists(folder["player/saves/slot.dat"]), "the save wasn't written when Run ended");
        using (var check = new GameStorage(folder["player"]))
            Assert.Equal(when, check.Saves.Load<Part>("slot").Value.Name);
        // The storage takes no more.
        Assert.Throws<ObjectDisposedException>(() => storage!.Saves.Save("later", new Part { Name = "too late" }));
    }
}
