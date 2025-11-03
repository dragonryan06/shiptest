using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace ShipTest.Editor;

public struct EditorPartInfo
{
    public EditorPartInfo(Dictionary godotDict)
    {
        Id = godotDict["id"].AsInt32();
        Name = godotDict["name"].AsString();
        Icon = GD.Load<CompressedTexture2D>(godotDict["icon"].AsString());
        Tags = godotDict["tags"].AsStringArray().ToList();

        if (Tags.Contains("tile"))
        {
            Icon = new AtlasTexture { Atlas = Icon, Region = new Rect2(0, 0, 32, 32)};
            SourceId = godotDict["source_id"].AsInt32();
            if (Tags.Contains("can_rotate"))
            {
                var orientations = godotDict["orientations"].AsGodotArray();
                foreach (var pos in orientations)
                {
                    var arr = pos.AsGodotArray();
                    Orientations.Add(new Vector2I(arr[0].AsInt32(), arr[1].AsInt32()));
                }

                AtlasPosition = Orientations[0];
                var atlas = (AtlasTexture)Icon;
                atlas.Region = new Rect2(32 * AtlasPosition, 32 * Vector2.One);
            }
            else
            {
                var pos = godotDict["atlas_pos"].AsGodotArray();
                AtlasPosition = new Vector2I(pos[0].AsInt32(), pos[1].AsInt32());
            }

            if (Tags.Contains("terrain"))
            {
                // In the future we can support multiple terrains on one set... if necessary
                Terrain = 0;
            }
        }
    }

    // All Parts
    public int Id { get; }
    
    public string Name { get; }
    
    public Texture2D Icon { get; }
    
    public List<string> Tags { get; }
    
    // Tiles only
    public int SourceId { get; } = -1;

    public Vector2I AtlasPosition { get; } = -Vector2I.One;

    public int Terrain { get; } = -1;
    
    /// Will be empty if this isn't tagged can_rotate.
    public List<Vector2I> Orientations { get; } = [];
}