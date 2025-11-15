using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Grids;

namespace ShipTest.Serialization;

public class ShipBlueprint
{
    public string Name { get; set; }
    
    public Dictionary<string, byte[]> GridLayers { get; set; }

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

        return body;
    } 
}