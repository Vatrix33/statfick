using Statventure.Packs;

namespace Statventure.Render3D;

/// <summary>
/// The models importer (Docs/Assets.md, "Importers"; Q20, S3-3): packing a folder, each <c>.glb</c> file
/// becomes one record entry at its path without the ending (<c>models/figure.glb</c> →
/// <c>models/figure</c>) holding its <see cref="ModelData"/> as <see cref="GltfImport"/> reads it: nodes,
/// meshes, materials, skins, clips with their events; checked as a model loaded from the game's data is
/// (<see cref="ModelData.Validate"/>; S5-1), so no pack holds a model that won't load. State machines are
/// entries of their own, packed from their JSON form.
/// </summary>
[Importer("models", ".glb")]
public sealed class ModelImporter : IImporter
{
    public void Import(ImportContext context)
    {
        var model = GltfImport.Load(context.SourceFile);
        model.Validate();
        context.AddRecord(context.PathWithoutEnding, model);
    }
}
