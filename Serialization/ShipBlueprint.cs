using System;
using System.Linq;
using Godot;
using ShipTest.Grids;

namespace ShipTest.Serialization;

[GlobalClass]
public partial class ShipBlueprint : Node
{
    [Export]
    public string ShipName { get; set; }
    
    [Export]
    public Godot.Collections.Dictionary<string, byte[]> GridLayers { get; set; }
    
    // Unfortunately there's no way to encode information we store on scene tiles into the tilemap data
    [Export]
    public Godot.Collections.Dictionary<Vector2I, float> TileEntityRotations { get; set; }

    public override void _Ready()
    {
        foreach (var child in GetChildren())
        {
            child.Owner = this;
        }
    }
    
    public void Clear()
    {
        ShipName = "Unnamed Ship";
        
        foreach (var child in GetChildren())
        {
            if (child is not TileMapLayer tileMap)
            {
                continue;
            }
            
            tileMap.Clear();
        
            foreach (var entity in tileMap.GetChildren())
            {
                entity.QueueFree();
            }
        }
    }
    
    public GridBody ToGridBody()
    {
        var body = new GridBody
        {
            Name = Name
        };

        foreach (var tileMap in GridLayers.Select(layer => new TileMapLayer
                 {
                     Name = layer.Key,
                     ZIndex = GridBody.LayerZIndicies[layer.Key],
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