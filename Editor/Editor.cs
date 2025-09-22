using Godot;
using Godot.Collections;

namespace ShipTest.Editor;

public partial class Editor : Node2D
{
    private const string InventoryGridPath =
        "HUD/PartInventory/PanelContainer/VBoxContainer/ScrollContainer/MarginContainer/GridContainer";
    private readonly Array _parts;

    public Editor()
    {
        _parts = InitializeParts();

        return;
        
        Array InitializeParts()
        {
            var jsonString = FileAccess.Open("res://Resources/Scenes/Editor/parts.json", 
                FileAccess.ModeFlags.Read).GetAsText();
            return Json.ParseString(jsonString).As<Array>();
        }
    }

    public override void _Ready()
    {
        var partInventoryItem = GD.Load<PackedScene>("res://Resources/Scenes/Editor/part_inventory_item.tscn");
        var grid = GetNode<GridContainer>(InventoryGridPath);
        
        foreach (var p in _parts)
        {
            var part = p.AsGodotDictionary();
            var inventoryItem = partInventoryItem.Instantiate<Button>();
            
            inventoryItem.GetNode<Label>("Label").Text = part["name"].AsString();
            inventoryItem.Icon = GD.Load<CompressedTexture2D>(part["icon"].AsString());
            inventoryItem.SetMeta("part_id", part["id"].AsInt32());
            
            grid.AddChild(inventoryItem);
        }
    }
}