using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;

namespace ShipTest.Editor;

public partial class Editor : Node2D
{
    private const string InventoryGridPath =
        "HUD/PartInventory/PanelContainer/VBoxContainer/ScrollContainer/MarginContainer/GridContainer";

    private readonly Color _validColor = new("#00ff00");
    private readonly Color _invalidColor = new("#ff0000");
    private readonly List<EditorPartInfo> _parts;

    private bool gridSnapping { get; set; }
    private bool canRotate { get; set; }
    private int rotationIdx { get; set; } = 0;

    private EditorPartInfo? selectedPart { get; set; }

    public Editor()
    {
        _parts = InitializeParts();

        return;
        
        List<EditorPartInfo> InitializeParts()
        {
            var jsonString = FileAccess.Open("res://Resources/Scenes/Editor/parts.json", 
                FileAccess.ModeFlags.Read).GetAsText();
            var array = Json.ParseString(jsonString).As<Array>();

            var parts = new List<EditorPartInfo>();
            foreach (var p in array)
            {
                parts.Add(new EditorPartInfo(p.AsGodotDictionary()));
            }
            
            return parts;
        }
    }

    public override void _Ready()
    {
        var partInventoryItem = GD.Load<PackedScene>("res://Resources/Scenes/Editor/part_inventory_item.tscn");
        var grid = GetNode<GridContainer>(InventoryGridPath);
        
        foreach (var part in _parts)
        {
            var inventoryItem = partInventoryItem.Instantiate<Button>();
            
            inventoryItem.GetNode<Label>("Label").Text = part.Name;
            inventoryItem.Icon = part.Icon.Duplicate() as Texture2D;
            inventoryItem.SetMeta("part_id", part.Id);
            
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
        if (Input.IsActionJustPressed("editor_rotate") && selectedPart != null && canRotate)
        {
            if (++rotationIdx == 4)
            {
                rotationIdx = 0;
            }

            var preview = GetNode<Sprite2D>("PlacementPreview");
            if (selectedPart.Value.Tags.Contains("tile"))
            {
                var atlas = preview.Texture as AtlasTexture;
                Debug.Assert(atlas != null, "Tile parts must have an AtlasTexture icon!!");
                atlas.Region = new Rect2(32*selectedPart.Value.Orientations[rotationIdx], 32*Vector2.One);
            }
            else
            {
                GetNode<Sprite2D>("PlacementPreview").RotationDegrees = rotationIdx * 90.0f;
            }
        } else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            if (selectedPart == null)
            {
                return;
            }

            TileMapLayer tileMap = null;
            if (selectedPart.Value.Tags.Contains("layer_floor"))
            {
                tileMap = GetNode<TileMapLayer>("Floor");
            } 
            else if (selectedPart.Value.Tags.Contains("layer_wall"))
            {
                tileMap = GetNode<TileMapLayer>("Walls");
            }
            
            Debug.Assert(tileMap != null, "Tried to place a tile without a layer tag!?");
            tileMap.SetCell(
                tileMap.LocalToMap(tileMap.GetLocalMousePosition()),
                selectedPart.Value.SourceId,
                selectedPart.Value.Tags.Contains("can_rotate")
                    ? selectedPart.Value.Orientations[rotationIdx]
                    : selectedPart.Value.AtlasPosition);
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

        selectedPart = _parts[partId];

        preview.Modulate = _validColor;
        preview.Texture = selectedPart.Value.Icon.Duplicate() as Texture2D;
        preview.Show();

        if (selectedPart.Value.Tags.Contains("tile"))
        {
            gridSnapping = true;
        }

        if (selectedPart.Value.Tags.Contains("can_rotate"))
        {
            canRotate = true;
        }
    }
}