using System;
using System.Linq;
using Godot;
using ShipTest.Grids;

namespace ShipTest.Serialization;

[GlobalClass]
public partial class ShipBlueprint : Node2D
{
    [Export]
    public string ShipName { get; set; }

    public override void _Ready()
    {
        foreach (var child in GetChildren())
        {
            child.Owner = this;
            child.ChildEnteredTree += OnLayerChildEnteredTree;
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
            Name = ShipName
        };

        foreach (var child in GetChildren())
        {
            body.AddChild(child.Duplicate());
        }

        return body;
    }

    private void OnLayerChildEnteredTree(Node child)
    {
        child.Owner = this;
    }
}