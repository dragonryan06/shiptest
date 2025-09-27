using System.Linq;
using Godot;
using Godot.Collections;

namespace ShipTest.Editor;

public partial class Editor : Node2D
{
    private const string InventoryGridPath =
        "HUD/PartInventory/PanelContainer/VBoxContainer/ScrollContainer/MarginContainer/GridContainer";

    private readonly Color _validColor = new("#00ff00");
    private readonly Color _invalidColor = new("#ff0000");
    private readonly Array _parts;

    private bool gridSnapping { get; set; }
    private bool canRotate { get; set; }
    private int rotationIdx { get; set; } = 0;
    
    private Dictionary selectedPart { get; set; }

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

        GetNode<CanvasLayer>("HUD").Connect("selection_changed", new Callable(this, MethodName.SelectionChanged));
    }

    public override void _Process(double delta)
    {
        if (selectedPart == null)
        {
            return;
        }
        
        var preview = GetNode<Sprite2D>("PlacementPreview");
        preview.Position = GetGlobalMousePosition();
        
        if (gridSnapping)
        {
            preview.Position = new Vector2(
                Mathf.Snapped(preview.Position.X + 16, 32) - 16,
                Mathf.Snapped(preview.Position.Y + 16, 32) - 16);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("editor_rotate") && canRotate)
        {
            if (++rotationIdx == 4)
            {
                rotationIdx = 0;
            }
            GetNode<Sprite2D>("PlacementPreview").RotationDegrees = rotationIdx * 90.0f;
        }
    }

    public void SelectionChanged(int partId)
    {
        var preview = GetNode<Sprite2D>("PlacementPreview");
        gridSnapping = false;
        canRotate = false;
        rotationIdx = 0;
        
        if (partId == -1)
        {
            preview.Hide();
            selectedPart = null;
            return;
        }
        
        selectedPart = _parts[partId].AsGodotDictionary();

        preview.Modulate = _validColor;
        preview.Texture = GD.Load<CompressedTexture2D>(selectedPart["icon"].AsString());
        preview.Show();

        switch (selectedPart["tags"].AsStringArray()[0])
        {
            case "tile_floor":
            case "tile_wall":
                gridSnapping = true;
                break;
        }

        if (selectedPart["tags"].AsStringArray().Contains("can_rotate"))
        {
            canRotate = true;
        }
    }
}