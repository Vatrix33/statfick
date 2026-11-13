using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Tests.Audio;

/// <summary>
/// The game's eight interface sounds (GM1 step 4; Docs/Game/Foundation.md, "The sounds"): their sources in
/// engine/Assets/Audio/effects/, the committed audio.pak holding exactly them, each registered under its name when the
/// sound starts and over within 0.3 s (Q14). How each renders is its fixture against Chromium (AudioFixtures.GameSound).
/// </summary>
public class GameSoundsTests
{
    public static TheoryData<string> Names => new(GameSounds.Names);

    /// <summary>
    /// The sources are the eight, each a playable patch in its JSON form as the packer's extraction writes it, whose
    /// layers all stop within 0.3 s (SoundEffects stops a layer 50 ms after its length).
    /// </summary>
    [Fact]
    public void TheSourcesAreTheEightSounds()
    {
        Assert.Equal(GameSounds.Names.Select(n => n + ".json").Order(StringComparer.Ordinal),
            Directory.GetFiles(GameSounds.Folder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        foreach (var name in GameSounds.Names)
        {
            var source = GameSounds.Source(name);
            var patch = RecordSerializer.FromJson<EffectPatch>(source);
            Assert.Empty(RecordChecks.Check(patch));
            Assert.Equal(RecordSerializer.ToJson(patch), source);
            Assert.InRange(patch.Layers.Max(l => l.Delay + l.Attack + l.Length + 0.05), 0, GameSounds.MaxSeconds);
        }
    }

    /// <summary>The committed audio.pak's effects are the eight, each the record its source holds.</summary>
    [Fact]
    public void TheCommittedPackHoldsTheEightAsTheirSources()
    {
        var effects = TestMusic.Library.Effects;
        Assert.Equal(GameSounds.Names.Order(StringComparer.Ordinal), effects.Keys.Order(StringComparer.Ordinal));
        using var pack = PackFile.Open(TestMusic.Pack);
        foreach (var name in GameSounds.Names)
        {
            Assert.Contains(pack.Entries, e => e.Path == "audio/effects/" + name && e.IsRecord);
            Assert.Equal(GameSounds.Source(name), RecordSerializer.ToJson(effects[name]));
        }
    }

    /// <summary>
    /// The audio system on the committed pack registers each under its name, and a play of it by that name sounds and
    /// is over (its last sample, its voice released) within 0.3 s.
    /// </summary>
    [Theory]
    [MemberData(nameof(Names))]
    public void EachSoundRegistersUnderItsNameAndIsOverWithinItsTime(string name)
    {
        const int Rate = 48000;
        var graph = new AudioGraph(Rate, 2);
        var system = new AudioSystem(graph, TestMusic.Library);
        var registered = system.Effects.Find(name);
        Assert.NotNull(registered);
        Assert.Same(TestMusic.Library.Effects[name], registered.Patch);
        system.Effects.Play(name);
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var end = (int)(GameSounds.MaxSeconds * Rate);
        long lastSounding = -1;
        var peak = 0.0;
        for (long frame = 0; frame < Rate / 2; frame += AudioGraph.RenderQuantumFrames)
        {
            system.BeforeQuantum();
            graph.RenderQuantum(bus);
            for (var c = 0; c < 2; c++)
                for (var i = 0; i < AudioGraph.RenderQuantumFrames; i++)
                {
                    var sample = Math.Abs(bus.Channel(c).Data[i]);
                    peak = Math.Max(peak, sample);
                    if (sample > 0)
                        lastSounding = Math.Max(lastSounding, frame + i);
                }
            if (frame + AudioGraph.RenderQuantumFrames > end + AudioGraph.RenderQuantumFrames)
                Assert.Equal(0, system.Effects.ActiveVoices);
        }
        Assert.True(peak > 0.01, $"{name} peaks at {peak}");
        Assert.InRange(lastSounding, 0, end - 1);
    }
}
