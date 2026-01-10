using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Grids;

namespace ShipTest.Serialization;

public class ShipBlueprint
{
    public string Name { get; set; }
    
    public Dictionary<string, byte[]> GridLayers { get; set; }
    
    // Unfortunately there's no way to encode information we store on scene tiles into the tilemap data
    public Dictionary<Vector2I, float> TileEntityRotations { get; set; }

    public GridBody ToGridBody()
    {
        var body = new GridBody
        {
            Name = Name
        };

        foreach (var tileMap in GridLayers.Select(layer => new TileMapLayer
                 {
                     Name = layer.Key,
                     TileSet = GD.Load<TileSet>(GridBody.LayerTileSets[layer.Key]),
                     TileMapData = layer.Value
                 }))
        {
            body.AddChild(tileMap);
        }

        // Possible memory leak here idk how best to do this callback tbh...
        body.Ready += () => BodyReadyCallback(body);

        return body;
    }

    private void BodyReadyCallback(GridBody body)
    {
        foreach (var layerName in GridLayers.Select(layer => layer.Key))
        {
            foreach (var child in body.GetNode<TileMapLayer>(layerName).GetChildren())
            {
                if (child is not TileEntity entity)
                {
                    continue;
                }

                entity.Rotation = TileEntityRotations[entity.TilePosition];
            }
        }
    }
}