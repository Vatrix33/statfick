using Statventure.Packs;

namespace Statventure.Render3D;

/// <summary>
/// The terrain importer (Docs/Assets.md, "Importers"; Q26, S3-3, S8-1): packing a folder, each terrain
/// description (<c>&lt;name&gt;.terrain.json</c>) becomes one record entry at its path without the
/// ending (<c>world/terrain/vale.terrain.json</c> → <c>world/terrain/vale</c>) holding its
/// <see cref="TerrainData"/>: the height image's 16-bit samples, the range, the cover image's covers and
/// the rest of the description, read as <see cref="TerrainData.Load(string)"/> reads them. The two images
/// are named relative to the description's folder and taken by it: they aren't packed themselves.
/// </summary>
[Importer("world", ".terrain.json")]
public sealed class TerrainImporter : IImporter
{
    public void Import(ImportContext context)
    {
        var folder = context.SourcePath[..(context.SourcePath.LastIndexOf('/') + 1)];
        var terrain = TerrainData.FromDescription(context.Read(), name => context.ReadSource(folder + name));
        context.AddRecord(context.PathWithoutEnding, terrain);
    }
}
