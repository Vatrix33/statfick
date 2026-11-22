using System.Collections.Concurrent;
using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Animation;

// A clip keeps its contacts with the ground on a body other than the one it was keyed on (GM2-P37, "contacts kept"; the
// manager's, 2026-11-21; Docs/Animation.md, "A clip keeps its contacts"; Docs/Game/Hero.md, "How it works", "Contacts
// kept"). GM2-P22 scales a clip's bone places by the legs; that keeps the joints in proportion but not the feet's or the
// body's own thickness, so a long foot pitched toe-down or a heavy thigh lying down goes into the ground. Here what touches
// the ground on the keyed body touches it on the playing body, and what is in the air stays as GM2-P22 places it: worked
// out once per body, worn set and clip at the clip's key times (straight between), as corrected channels of the root's
// place and the chains' turns, so a frame costs what it did. Blender's side is Tools/BlenderReference/clip_contacts.py, the
// same arithmetic.

/// <summary>
/// The rule's numbers (GM2-P37; each recorded with its reason in Docs/Game/Hero.md, "How it works", "Contacts kept").
/// </summary>
public static class ContactRule
{
    /// <summary>
    /// The keyed clearance (metres) at which a group's correction has faded out: it counts fully at a keyed clearance of 0 or
    /// less and fades smoothly (smoothstep) to nothing here. 150 mm: the hobbit's toe-down push-off in flee clears the ground
    /// only once the man's toe is about 145 mm up, and outside a fall no trunk of the clips comes that low (the man's lowest,
    /// meditate's fingers, 344 mm), so the hips never answer a standing or kneeling trunk.
    /// </summary>
    public const double Fade = 0.150;

    /// <summary>
    /// The contact band (metres): a keyed clearance up to this, under the ground included, is the keying's tolerance about the
    /// ground (the man's soles and knee rest from 2.2 mm under to 2 mm over it), kept as it is on every body; above it the
    /// height over the band is scaled by the measure's ratio. Scaled whole, a body 1.21 times the man's would sink 2.7 mm and
    /// float 2.4 mm where he touches.
    /// </summary>
    public const double Band = 0.002;

    /// <summary>The rounds after the first pass in which what a chain can't reach moves the hips (four: they converge to 0.02 mm on the bodies measured).</summary>
    public const int Rounds = 4;

    /// <summary>What counts as done (metres): an end within a micrometre of where it should be isn't solved, and a round that leaves no more than this to do ends the rounds.</summary>
    public const double Settled = 1e-6;

    /// <summary>How fully a group's correction counts at its keyed clearance <paramref name="keyed"/> (metres): 1 at 0 or under, 1 − smoothstep(keyed / <see cref="Fade"/>) above, 0 from <see cref="Fade"/> on.</summary>
    public static double Weight(double keyed)
    {
        if (keyed <= 0)
            return 1;
        var x = keyed / Fade;
        if (x >= 1)
            return 0;
        return 1 - x * x * (3 - 2 * x);
    }

    /// <summary>
    /// The clearance a group aims at on a body whose measure is <paramref name="ratio"/> times the keyed one's: in the band
    /// as keyed, above it the band plus the ratio times the height over it; written as the keyed clearance plus the ratio's
    /// excess times the height over the band, so the ratio 1 gives the keyed clearance exactly.
    /// </summary>
    public static double Target(double keyed, double ratio) => keyed + (ratio - 1) * Math.Max(0, keyed - Band);
}

/// <summary>
/// A clip's contacts (<see cref="ModelAnimation.Contacts"/>, GM2-P37): the joint that carries the body, its chains, and
/// the body it was keyed on, given by whoever loaded the clip (<see cref="AnimationClip.AllOf(ModelData, Func{string, ModelData?})"/>).
/// The keyed body's clearances at the clip's key times are worked out once, the first time a body other than it binds the
/// clip.
/// </summary>
public sealed class ClipContacts
{
    readonly Lazy<double[][]> _keyed;

    internal ClipContacts(AnimationClip clip, ModelContacts data, ModelData? keyedBody)
    {
        Data = data;
        KeyedBody = keyedBody;
        Chains = [.. data.Chains.Select(c => c.Joints.ToArray())];
        Signature = data.Root + "|" + string.Join("|", Chains.Select(c => string.Join(",", c)));
        Times = KeyTimes(clip);
        _keyed = new Lazy<double[][]>(() => Measure(clip), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ModelContacts Data { get; }
    /// <summary>The model of the body the clip was keyed on (<see cref="ModelContacts.KeyedOn"/>), or null when the clip was loaded without it.</summary>
    public ModelData? KeyedBody { get; }
    public string Root => Data.Root;
    public IReadOnlyList<string[]> Chains { get; }
    /// <summary>The clip's key times (seconds): every key of every channel, in order, once each.</summary>
    public float[] Times { get; }
    internal string Signature { get; }

    /// <summary>The keyed body's clearances (metres over its ground) per key time, per group (<see cref="ContactShape"/>'s order).</summary>
    internal double[][] Keyed => _keyed.Value;

    static float[] KeyTimes(AnimationClip clip) =>
        [.. clip.Tracks.SelectMany(t => new[] { t.Translation, t.Rotation, t.Scale }).OfType<AnimationCurve>().SelectMany(c => c.Times).Distinct().Order()];

    double[][] Measure(AnimationClip clip)
    {
        var body = KeyedBody ?? throw new InvalidOperationException(
            $"the clip {clip.Name} keeps its contacts keyed on {Data.KeyedOn}, which wasn't given: load it with its keyed body (AnimationClip.AllOf with the models)");
        var skeleton = new Skeleton(body);
        var shape = skeleton.ShapeFor(this) ?? throw new InvalidDataException(
            $"the clip {clip.Name}'s keyed body {Data.KeyedOn} has no mesh or lacks a joint of its contacts");
        var binding = new ClipBinding(clip, skeleton, contacts: false);
        var buffer = new PoseBuffer(skeleton);
        var result = new double[Times.Length][];
        for (var k = 0; k < Times.Length; k++)
        {
            buffer.Clear();
            binding.Sample(Times[k], buffer);
            result[k] = shape.Clearances(body.WorldMatrices(buffer.ToPose()));
        }
        return result;
    }
}

/// <summary>
/// A body as the ground meets it, for a set of contact chains (GM2-P37): the vertices of its skin and of the garments it
/// wears (its rigid items left out, as its ground is measured), each in the group of the bone it follows most: the rest of
/// the body (0), and per chain c its end (1 + 2c: the end joint and the bones it carries, a hand's fingers and thumb) and its
/// upper part (2 + 2c: the top and middle joints). A group's clearance is its lowest point over the body's ground
/// (<see cref="Statventure.Scene.Actor.GroundOf"/>).
/// </summary>
sealed class ContactShape
{
    readonly Matrix4x4[] _before;
    readonly int[] _slotNode;
    readonly float[] _x, _y, _z, _w;
    readonly int[] _s;
    readonly byte[] _group;
    readonly ConcurrentDictionary<AnimationClip, Lazy<ContactCorrection?>> _corrections = new();

    ContactShape(Skeleton skeleton, int root, int[][] chains, float ground, List<(Matrix4x4 Before, int Node)> slots,
        List<(Vector3 Position, int[] Slots, float[] Weights, byte Group)> vertices)
    {
        Skeleton = skeleton;
        Root = root;
        Chains = chains;
        Ground = ground;
        Groups = 1 + 2 * chains.Length;
        _before = [.. slots.Select(s => s.Before)];
        _slotNode = [.. slots.Select(s => s.Node)];
        var n = vertices.Count;
        (_x, _y, _z, _group) = (new float[n], new float[n], new float[n], new byte[n]);
        (_w, _s) = (new float[4 * n], new int[4 * n]);
        for (var v = 0; v < n; v++)
        {
            var (p, s, w, g) = vertices[v];
            (_x[v], _y[v], _z[v], _group[v]) = (p.X, p.Y, p.Z, g);
            for (var k = 0; k < 4; k++)
                (_s[4 * v + k], _w[4 * v + k]) = (s[k], w[k]);
        }
    }

    public Skeleton Skeleton { get; }
    public int Root { get; }
    /// <summary>Each chain's three nodes: top, middle, end.</summary>
    public int[][] Chains { get; }
    public float Ground { get; }
    public int Groups { get; }
    public int Count => _x.Length;
    /// <summary>What the shape keeps (bytes, its arrays): its vertices' places, joints, weights and groups and its joints' matrices.</summary>
    public long Bytes => _x.Length * (3L * 4 + 4 * 4 + 4 * 4 + 1) + _slotNode.Length * (64L + 4);

    public static int EndGroup(int chain) => 1 + 2 * chain;
    public static int UpperGroup(int chain) => 2 + 2 * chain;

    /// <summary>The shape of a skeleton's body and worn set for a clip's contacts, or null when the skeleton lacks a joint they name or the body and its garments have no skinned mesh.</summary>
    public static ContactShape? Of(Skeleton skeleton, ClipContacts contacts)
    {
        var root = skeleton.Find(contacts.Root);
        var chains = contacts.Chains.Select(c => c.Select(skeleton.Find).ToArray()).ToArray();
        if (root < 0 || chains.Any(c => c.Length != 3 || c.Any(j => j < 0)))
            return null;
        var body = skeleton.Model;
        // Each node's group: a chain's end and every bone under it, its top and middle; the rest of the body otherwise.
        var groupOf = new byte[skeleton.Count];
        for (var c = 0; c < chains.Length; c++)
        {
            var below = new Stack<int>([chains[c][2]]);
            while (below.Count > 0)
            {
                var node = below.Pop();
                groupOf[node] = (byte)EndGroup(c);
                foreach (var child in body.Nodes[node].Children)
                    below.Push(child);
            }
            groupOf[chains[c][0]] = groupOf[chains[c][1]] = (byte)UpperGroup(c);
        }
        var slots = new List<(Matrix4x4 Before, int Node)>();
        var vertices = new List<(Vector3, int[], float[], byte)>();
        var rest = body.WorldMatrices();
        // The body's own skins: each joint's inverse bind matrix, then its node's posed world (the renderer's skinning).
        foreach (var node in body.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
        {
            var skin = body.Skins[node.Skin];
            AddSkin(body, node, j => (skin.InverseBindMatrices[j], skin.Joints[j]), slots, vertices, groupOf);
        }
        // A garment's joint follows the body's bone of its name (Gear.cs, Garment): its rest world times that bone's
        // deformation, so its skinning is the inverse bind, its rest world and the bone's inverse rest before the bone's
        // posed world; a joint the body hasn't stays at the garment's rest.
        foreach (var garment in skeleton.Worn)
        {
            var garmentRest = garment.WorldMatrices();
            foreach (var node in garment.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
            {
                var skin = garment.Skins[node.Skin];
                AddSkin(garment, node, j =>
                {
                    var joint = skin.Joints[j];
                    var wearer = skeleton.Find(garment.Nodes[joint].Name);
                    var before = skin.InverseBindMatrices[j] * garmentRest[joint];
                    if (wearer < 0)
                        return (before, -1);
                    Matrix4x4.Invert(rest[wearer], out var inverse);
                    return (before * inverse, wearer);
                }, slots, vertices, groupOf);
            }
        }
        if (vertices.Count == 0)
            return null;
        return new ContactShape(skeleton, root, chains, Statventure.Scene.Actor.GroundOf(body, skeleton.Worn), slots, vertices);
    }

    static void AddSkin(ModelData model, ModelNode node, Func<int, (Matrix4x4 Before, int Node)> slotOf, List<(Matrix4x4 Before, int Node)> slots,
        List<(Vector3, int[], float[], byte)> vertices, byte[] groupOf)
    {
        var skin = model.Skins[node.Skin];
        var first = slots.Count;
        for (var j = 0; j < skin.Joints.Count; j++)
            slots.Add(slotOf(j));
        foreach (var primitive in model.Meshes[node.Mesh].Primitives)
        {
            if (primitive.Joints is not { } joints || primitive.Weights is not { } weights)
                continue;
            for (var v = 0; v < primitive.Positions.Length; v++)
            {
                var (j, w) = (joints[v], weights[v]);
                int[] js = [first + j.J0, first + j.J1, first + j.J2, first + j.J3];
                float[] ws = [w.X, w.Y, w.Z, w.W];
                // The bone it follows most (the first of equal weights, in the file's order).
                var most = 0;
                for (var k = 1; k < 4; k++)
                    if (ws[k] > ws[most])
                        most = k;
                var bone = slots[js[most]].Node;
                vertices.Add((primitive.Positions[v], js, ws, bone >= 0 ? groupOf[bone] : (byte)0));
            }
        }
    }

    /// <summary>
    /// Each group's clearance in a pose (<paramref name="world"/>: the skeleton's posed node matrices): its lowest vertex's
    /// height (glTF's Y, the vertices skinned by linear blending, four joints) over the ground; +∞ for a group with no vertex.
    /// </summary>
    public double[] Clearances(Matrix4x4[] world)
    {
        // Only heights are needed: each slot's matrix's second column.
        var columns = new Vector4[_slotNode.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            var m = _slotNode[i] >= 0 ? _before[i] * world[_slotNode[i]] : _before[i];
            columns[i] = new Vector4(m.M12, m.M22, m.M32, m.M42);
        }
        var low = new float[Groups];
        Array.Fill(low, float.PositiveInfinity);
        for (var v = 0; v < _x.Length; v++)
        {
            var y = 0f;
            for (var k = 4 * v; k < 4 * v + 4; k++)
            {
                var weight = _w[k];
                if (weight == 0)
                    continue;
                var c = columns[_s[k]];
                y += weight * (_x[v] * c.X + _y[v] * c.Y + _z[v] * c.Z + c.W);
            }
            if (y < low[_group[v]])
                low[_group[v]] = y;
        }
        var result = new double[Groups];
        for (var g = 0; g < Groups; g++)
            result[g] = float.IsPositiveInfinity(low[g]) ? double.PositiveInfinity : (double)low[g] - Ground;
        return result;
    }

    /// <summary>The clip's correction on this body (once per shape and clip), or null when it changes nothing.</summary>
    public ContactCorrection? CorrectionOf(AnimationClip clip, ClipContacts contacts) =>
        _corrections.GetOrAdd(clip, c => new Lazy<ContactCorrection?>(() => ContactCorrection.Compute(this, c, contacts), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
}

/// <summary>
/// What keeping a clip's contacts does on one body (GM2-P37): the root's place and the chains' joints' turns made again at
/// each of the clip's key times, as curves a binding plays instead of the clip's own (straight between keys), and what it
/// met: the keys it changed, the keys where a chain couldn't reach its target (clamped) and where one part asked the hips up
/// and another down (up won).
/// </summary>
public sealed class ContactCorrection
{
    ContactCorrection(Dictionary<int, AnimationTrack> tracks, int keys, int changed, int clamped, int conflicts, int lowered, double largest, double leftOver)
    {
        Lowered = lowered;
        LeftOver = leftOver;
        Tracks = tracks;
        Keys = keys;
        Changed = changed;
        Clamped = clamped;
        Conflicts = conflicts;
        LargestHipsMove = largest;
    }

    /// <summary>The corrected tracks by skeleton node: the root's place (already in the body's measure), the chains' turns.</summary>
    internal IReadOnlyDictionary<int, AnimationTrack> Tracks { get; }
    /// <summary>The clip's key times.</summary>
    public int Keys { get; }
    /// <summary>Key times at which the correction moved something.</summary>
    public int Changed { get; }
    /// <summary>Key times at which a chain ended clamped (its target out of its reach).</summary>
    public int Clamped { get; }
    /// <summary>Key times at which a clamped end asked the hips down and wasn't granted it in full: a part asked them up, or they came down only as far as nothing went under the ground or its goal (up won).</summary>
    public int Conflicts { get; }
    /// <summary>What up winning cost: the farthest a clamped end was left from where it should be at a conflict (metres).</summary>
    public double LeftOver { get; }
    /// <summary>The largest move of the hips at a key (metres, either way).</summary>
    public double LargestHipsMove { get; }
    /// <summary>Key times at which a clamped end brought the hips down (a straight leg that can't reach the ground).</summary>
    public int Lowered { get; }

    /// <summary>What the correction keeps (bytes): its curves' values and their key times (shared, counted once).</summary>
    public long Bytes => Tracks.Values.SelectMany(t => new[] { t.Translation, t.Rotation }).OfType<AnimationCurve>().Distinct().Sum(c => 4L * c.Values.Length)
        + 4L * Tracks.Values.SelectMany(t => new[] { t.Translation, t.Rotation }).OfType<AnimationCurve>().Select(c => c.Times).Distinct().Sum(t => t.Length);

    internal static ContactCorrection? Compute(ContactShape shape, AnimationClip clip, ClipContacts contacts)
    {
        var skeleton = shape.Skeleton;
        var model = skeleton.Model;
        var binding = new ClipBinding(clip, skeleton, contacts: false);
        var ratio = binding.Scale;
        var keyed = contacts.Keyed;
        var times = contacts.Times;
        var buffer = new PoseBuffer(skeleton);
        var nodes = new[] { shape.Root }.Concat(shape.Chains.SelectMany(c => c)).Distinct().ToArray();
        var moved = new HashSet<int>();
        var values = nodes.ToDictionary(n => n, n => new float[times.Length * 3]);
        var rotations = nodes.ToDictionary(n => n, n => new float[times.Length * 4]);
        var (changed, clamped, conflicts, lowered, largest, leftOver) = (0, 0, 0, 0, 0.0, 0.0);
        for (var k = 0; k < times.Length; k++)
        {
            buffer.Clear();
            binding.Sample(times[k], buffer);
            var solve = new KeySolve(shape, buffer, keyed[k], ratio);
            solve.Run();
            if (solve.Moved.Count > 0)
                changed++;
            if (solve.EndedClamped)
                clamped++;
            if (solve.Conflicted)
                conflicts++;
            if (solve.Lowered)
                lowered++;
            largest = Math.Max(largest, Math.Abs(solve.Hips));
            leftOver = Math.Max(leftOver, solve.LeftOver);
            moved.UnionWith(solve.Moved);
            foreach (var node in nodes)
            {
                if (node == shape.Root)
                    for (var c = 0; c < 3; c++)
                        values[node][3 * k + c] = (float)buffer.Translation[3 * node + c];
                // A turn as the clip's sample gives it (w ≥ 0 after sampling) or as the solve set it (unit length).
                for (var c = 0; c < 4; c++)
                    rotations[node][4 * k + c] = (float)buffer.Rotation[4 * node + c];
            }
        }
        if (moved.Count == 0)
            return null;
        var tracks = new Dictionary<int, AnimationTrack>();
        var byNode = clip.Tracks.ToDictionary(t => t.Node, t => t);
        foreach (var node in moved)
        {
            var name = model.Nodes[node].Name;
            var original = byNode.TryGetValue(name, out var track) ? track : new AnimationTrack(name, true, null, null, null);
            var translation = node == shape.Root ? new AnimationCurve(AnimationPath.Translation, AnimationInterpolation.Linear, times, values[node]) : original.Translation;
            var rotation = node == shape.Root ? original.Rotation : new AnimationCurve(AnimationPath.Rotation, AnimationInterpolation.Linear, times, rotations[node]);
            tracks[node] = original with { Translation = translation, Rotation = rotation };
        }
        return new ContactCorrection(tracks, times.Length, changed, clamped, conflicts, lowered, largest, leftOver);
    }
}

/// <summary>
/// The rule at one key time (GM2-P37; Docs/Game/Hero.md, "How it works", "Contacts kept"), on a buffer holding the clip's
/// sample there (its places in the body's measure, GM2-P22):
/// 1. Each group's clearance c here and c* on the keyed body; its goal c + w(c*)·(T(c*) − c) (<see cref="ContactRule"/>).
/// 2. The hips (the root's place, straight up or down in model space) by the rest of the body: to its goal.
/// 3. Each chain's end by a two-bone solve to where it should be: at its goal as far as it is a contact, moving with the
///    hips as far as it is in the air (its goal plus 1 − w of the hips' move).
/// 4. Rounds (<see cref="ContactRule.Rounds"/>): an upper part under the ground and under its goal (a kneeling knee, a
///    lying thigh or arm) asks the hips up by what it lacks; a clamped end over where it should be (a straight leg that
///    can't reach down) asks them down, as far as nothing (the rest, an upper part) goes under the ground or its goal: up
///    wins (counted where a down isn't granted in full); then every end is solved again to where it should be.
/// </summary>
sealed class KeySolve(ContactShape shape, PoseBuffer buffer, double[] keyed, double ratio)
{
    readonly ModelData _model = shape.Skeleton.Model;
    readonly bool[] _clamped = new bool[shape.Chains.Length];
    double[] _goal = [];
    double[] _weight = [];
    Matrix4x4[] _world = [];

    public double Hips { get; private set; }
    public bool Conflicted { get; private set; }
    public bool Lowered { get; private set; }
    /// <summary>At a conflict, the farthest a clamped end was left from where it should be (metres); 0 elsewhere.</summary>
    public double LeftOver { get; private set; }
    public HashSet<int> Moved { get; } = [];
    public bool EndedClamped => _clamped.Any(c => c);

    bool Usable(int group) => !double.IsInfinity(_goal[group]);

    /// <summary>Where a chain's end group should be once the hips have moved by <paramref name="hips"/>.</summary>
    double Desired(int end, double hips) => _goal[end] + (1 - _weight[end]) * hips;

    public void Run()
    {
        _world = _model.WorldMatrices(buffer.ToPose());
        var before = shape.Clearances(_world);
        var n = shape.Groups;
        (_goal, _weight) = (new double[n], new double[n]);
        for (var g = 0; g < n; g++)
        {
            if (double.IsInfinity(before[g]) || double.IsInfinity(keyed[g]))
            {
                (_goal[g], _weight[g]) = (double.PositiveInfinity, 0);
                continue;
            }
            _weight[g] = ContactRule.Weight(keyed[g]);
            _goal[g] = before[g] + _weight[g] * (ContactRule.Target(keyed[g], ratio) - before[g]);
        }
        // Pass 1: the hips by the rest of the body; each end to where it should be.
        // The rest of the body floating over its goal comes down to it (a slender body doesn't float); under its goal it is
        // only lifted out of the ground (a trunk in mail lies on its mail, not as high as the keyed body's bare back).
        var first = !Usable(0) ? 0 : before[0] > _goal[0] ? _goal[0] - before[0] : Math.Max(0, Math.Min(_goal[0] - before[0], -before[0]));
        MoveHips(first);
        for (var c = 0; c < shape.Chains.Length; c++)
        {
            var end = ContactShape.EndGroup(c);
            if (!Usable(end))
                continue;
            var lack = Desired(end, Hips) - (before[end] + first);
            if (Math.Abs(lack) >= ContactRule.Settled)
                _clamped[c] = Solve(c, _world[shape.Chains[c][2]].Translation, lack);
        }
        // The rounds: what the chains can't do moves the hips. An upper part under the ground and its goal asks them up; a
        // clamped end over where it should be (a straight leg that can't reach down) asks them down, as far as nothing (the
        // rest, an upper part) goes under the ground or its goal: up wins, in every round.
        for (var round = 0; round < ContactRule.Rounds; round++)
        {
            var now = shape.Clearances(_world);
            var (up, down) = (0.0, 0.0);
            var room = Usable(0) ? now[0] - Math.Min(_goal[0], 0) : double.PositiveInfinity;
            for (var c = 0; c < shape.Chains.Length; c++)
            {
                var (end, upper) = (ContactShape.EndGroup(c), ContactShape.UpperGroup(c));
                if (Usable(upper))
                {
                    var floor = Math.Min(_goal[upper], 0);
                    up = Math.Max(up, floor - now[upper]);
                    room = Math.Min(room, now[upper] - floor);
                }
                if (Usable(end) && _clamped[c])
                    down = Math.Min(down, Desired(end, Hips) - now[end]);
            }
            var move = up > 0 ? up : Math.Max(down, -Math.Max(0, room));
            if (down < 0 && move > down)
                Conflicted = true;
            if (Math.Abs(move) < ContactRule.Settled)
                move = 0;
            if (move < 0)
                Lowered = true;
            var lacks = new double[shape.Chains.Length];
            var places = new Vector3[shape.Chains.Length];
            var settled = move == 0;
            for (var c = 0; c < shape.Chains.Length; c++)
            {
                var end = ContactShape.EndGroup(c);
                places[c] = _world[shape.Chains[c][2]].Translation;
                lacks[c] = Usable(end) ? Desired(end, Hips + move) - now[end] : move;
                if (Math.Abs(lacks[c] - move) >= ContactRule.Settled)
                    settled = false;
            }
            if (settled)
                break;
            MoveHips(move);
            for (var c = 0; c < shape.Chains.Length; c++)
                if (Math.Abs(lacks[c] - move) >= ContactRule.Settled && Usable(ContactShape.EndGroup(c)))
                    _clamped[c] = Solve(c, places[c], lacks[c]);
        }
        if (Conflicted)
        {
            var now = shape.Clearances(_world);
            for (var c = 0; c < shape.Chains.Length; c++)
                if (_clamped[c] && Usable(ContactShape.EndGroup(c)))
                    LeftOver = Math.Max(LeftOver, Math.Abs(Desired(ContactShape.EndGroup(c), Hips) - now[ContactShape.EndGroup(c)]));
        }
    }

    /// <summary>Moves the root's place by <paramref name="up"/> metres up in model space (glTF's Y).</summary>
    void MoveHips(double up)
    {
        if (up == 0)
            return;
        var root = shape.Root;
        var node = _model.Nodes[root];
        var parent = node.Parent >= 0 ? _world[node.Parent] : Matrix4x4.Identity;
        Matrix4x4.Invert(parent, out var inverse);
        // Model space into the parent's, then out of the rest's turn and scale (PoseBuffer.ToPose: rest · T(t)).
        var (ix, iy, iz) = (up * inverse.M21, up * inverse.M22, up * inverse.M23);
        var r = Quaternion.Normalize(node.Rotation);
        var local = Rotate(Conjugate(r), (ix, iy, iz));
        buffer.Translation[3 * root] += local.X / node.Scale.X;
        buffer.Translation[3 * root + 1] += local.Y / node.Scale.Y;
        buffer.Translation[3 * root + 2] += local.Z / node.Scale.Z;
        buffer.Set[root] |= ChannelSet.Translation;
        Hips += up;
        Moved.Add(root);
        _world = _model.WorldMatrices(buffer.ToPose());
    }

    /// <summary>
    /// Chain <paramref name="chain"/>'s end to <paramref name="from"/> raised by <paramref name="up"/> (model space): the
    /// middle joint turned about its own X (the rig's hinge) to the bend that puts the end at the target's distance from the
    /// top, keeping the side of straight it is on (knees flexed forward, elbows the other way); then the chain turned about
    /// its top by the smallest turn onto the target; the end keeps its turn in the world. True when the target is out of
    /// reach (the bend clamped straight or folded).
    /// </summary>
    bool Solve(int chain, Vector3 from, double up)
    {
        var (top, middle, end) = (shape.Chains[chain][0], shape.Chains[chain][1], shape.Chains[chain][2]);
        var target = (X: (double)from.X, Y: from.Y + up, Z: (double)from.Z);
        var h = Of(_world[top].Translation);
        var k = Of(_world[middle].Translation);
        var a = Of(_world[end].Translation);
        var x = Normalize((_world[middle].M11, _world[middle].M12, _world[middle].M13));
        var p = Sub(a, k);
        var kh = Sub(k, h);
        double alpha = Dot(kh, p), beta = Dot(kh, Cross(x, p));
        var reach = Math.Sqrt(alpha * alpha + beta * beta);
        if (reach < 1e-12)
            return true;
        var toTarget = Sub(target, h);
        var cosine = (Dot(toTarget, toTarget) - Dot(kh, kh) - Dot(p, p)) / 2 / reach;
        var clamped = false;
        if (cosine > 1)
            (cosine, clamped) = (1, true);
        else if (cosine < -1)
            (cosine, clamped) = (-1, true);
        var straight = Math.Atan2(beta, alpha);
        // The flexion is the turn away from straight: keep its side.
        var bendAngle = straight + (-straight >= 0 ? 1 : -1) * Math.Acos(cosine);
        var bend = AxisAngle(x, bendAngle);
        var a2 = Add(k, Rotate(bend, p));
        var swing = Between(Sub(a2, h), toTarget);
        var parentTurn = TurnOf(_world[_model.Nodes[top].Parent]);
        var (topTurn, middleTurn, endTurn) = (TurnOf(_world[top]), TurnOf(_world[middle]), TurnOf(_world[end]));
        var topNew = Multiply(swing, topTurn);
        var middleNew = Multiply(Multiply(swing, bend), middleTurn);
        SetTurn(top, parentTurn, topNew);
        SetTurn(middle, topNew, middleNew);
        SetTurn(end, middleNew, endTurn);
        _world = _model.WorldMatrices(buffer.ToPose());
        return clamped;
    }

    /// <summary>A joint's channel turn (rest-relative, Blender's pose channel) for a world turn under its parent's.</summary>
    void SetTurn(int node, (double X, double Y, double Z, double W) parentWorld, (double X, double Y, double Z, double W) world)
    {
        var rest = Quaternion.Normalize(_model.Nodes[node].Rotation);
        var basis = Multiply(Conjugate(rest), Multiply(Conjugate(parentWorld), world));
        var length = Math.Sqrt(basis.X * basis.X + basis.Y * basis.Y + basis.Z * basis.Z + basis.W * basis.W);
        buffer.Rotation[4 * node] = basis.X / length;
        buffer.Rotation[4 * node + 1] = basis.Y / length;
        buffer.Rotation[4 * node + 2] = basis.Z / length;
        buffer.Rotation[4 * node + 3] = basis.W / length;
        buffer.Set[node] |= ChannelSet.Rotation;
        Moved.Add(node);
    }

    // Double precision vectors and quaternions (x, y, z, w; Hamilton products, as System.Numerics' operator *).
    static (double X, double Y, double Z) Of(Vector3 v) => (v.X, v.Y, v.Z);
    static (double X, double Y, double Z) Add((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    static (double X, double Y, double Z) Sub((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        (a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    static (double X, double Y, double Z) Normalize((double X, double Y, double Z) a)
    {
        var length = Math.Sqrt(Dot(a, a));
        return (a.X / length, a.Y / length, a.Z / length);
    }

    static (double X, double Y, double Z, double W) Conjugate((double X, double Y, double Z, double W) q) => (-q.X, -q.Y, -q.Z, q.W);
    static (double X, double Y, double Z, double W) Conjugate(Quaternion q) => (-q.X, -q.Y, -q.Z, q.W);

    static (double X, double Y, double Z, double W) Multiply((double X, double Y, double Z, double W) a, (double X, double Y, double Z, double W) b) =>
        (a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
         a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
         a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
         a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

    static (double X, double Y, double Z, double W) Multiply((double X, double Y, double Z, double W) a, Quaternion b) => Multiply(a, (b.X, b.Y, b.Z, b.W));

    static (double X, double Y, double Z) Rotate((double X, double Y, double Z, double W) q, (double X, double Y, double Z) v)
    {
        // v + 2w (u × v) + 2 u × (u × v), u the vector part.
        var u = (q.X, q.Y, q.Z);
        var t = Cross(u, v);
        t = (2 * t.Item1, 2 * t.Item2, 2 * t.Item3);
        var c = Cross(u, t);
        return (v.X + q.W * t.Item1 + c.X, v.Y + q.W * t.Item2 + c.Y, v.Z + q.W * t.Item3 + c.Z);
    }

    static (double X, double Y, double Z, double W) AxisAngle((double X, double Y, double Z) axis, double angle)
    {
        var (s, c) = (Math.Sin(angle / 2), Math.Cos(angle / 2));
        return (axis.X * s, axis.Y * s, axis.Z * s, c);
    }

    /// <summary>The smallest turn taking direction <paramref name="from"/> to <paramref name="to"/>.</summary>
    static (double X, double Y, double Z, double W) Between((double X, double Y, double Z) from, (double X, double Y, double Z) to)
    {
        var (f, t) = (Normalize(from), Normalize(to));
        var c = Cross(f, t);
        var q = (c.X, c.Y, c.Z, 1 + Dot(f, t));
        var length = Math.Sqrt(q.Item1 * q.Item1 + q.Item2 * q.Item2 + q.Item3 * q.Item3 + q.Item4 * q.Item4);
        return (q.Item1 / length, q.Item2 / length, q.Item3 / length, q.Item4 / length);
    }

    /// <summary>A world matrix's turn (its scale taken out), in double.</summary>
    static (double X, double Y, double Z, double W) TurnOf(Matrix4x4 m)
    {
        Matrix4x4.Decompose(m, out _, out var q, out _);
        q = Quaternion.Normalize(q);
        return (q.X, q.Y, q.Z, q.W);
    }
}
