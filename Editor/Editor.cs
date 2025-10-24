using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;

namespace ShipTest.Editor;

public partial class Editor : Node2D
{
    private const string InventoryGridPath =
        "HUD/PartInventory/PanelContainer/VBoxContainer/ScrollContainer/MarginContainer/GridContainer";

    private readonly Color _placingColor = new("#00ff00");
    private readonly Color _deletingColor = new("#ff0000");
    private readonly List<EditorPartInfo> _parts;
    
    private bool GridSnapping { get; set; }
    private bool CanRotate { get; set; }
    private int RotationIdx { get; set; }
    private bool Dragging { get; set; }

    private bool _deleting;
    private bool Deleting
    {
        get => _deleting;
        set
        {
            _deleting = value;

            if (_deleting)
            {
                WorkingMap.GetNode<TileMapLayer>("Preview").Modulate = _deletingColor;
            }
            else
            {
                WorkingMap.GetNode<TileMapLayer>("Preview").Modulate = _placingColor;
            }
        }
    }
    
    private TileMapLayer WorkingMap { get; set; }

    private EditorPartInfo? _selectedPart;
    private EditorPartInfo? SelectedPart
    {
        get => _selectedPart;
        set
        {
            _selectedPart = value;
            UpdateWorkingMap();
        }
    }

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
        if (SelectedPart == null)
        {
            return;
        }
        
        var preview = WorkingMap.GetNode<TileMapLayer>("Preview");
        
        if (!Dragging)
        {
            preview.Clear();
        }
        
        preview.SetCell(
            preview.LocalToMap(preview.GetLocalMousePosition()),
            SelectedPart.Value.SourceId,
            SelectedPart.Value.Tags.Contains("can_rotate")
                ? SelectedPart.Value.Orientations[RotationIdx]
                : SelectedPart.Value.AtlasPosition);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("editor_rotate") && SelectedPart != null && CanRotate)
        {
            if (++RotationIdx == 4)
            {
                RotationIdx = 0;
            }

            var preview = GetNode<Sprite2D>("PlacementPreview");
            if (SelectedPart.Value.Tags.Contains("tile"))
            {
                var atlas = preview.Texture as AtlasTexture;
                Debug.Assert(atlas != null, "Tile parts must have an AtlasTexture icon!!");
                atlas.Region = new Rect2(32*SelectedPart.Value.Orientations[RotationIdx], 32*Vector2.One);
            }
            else
            {
                GetNode<Sprite2D>("PlacementPreview").RotationDegrees = RotationIdx * 90.0f;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (SelectedPart == null)
            {
                return;
            }

            Deleting = mouseButton.ButtonIndex == MouseButton.Right;

            if (mouseButton.Pressed)
            {
                Dragging = true;
            }
            else
            {
                Dragging = false;

                var preview = WorkingMap.GetNode<TileMapLayer>("Preview");
                if (preview.TileMapData.IsEmpty())
                {
                    return;
                }
                
                // I would love to eventually have a like fly-in and then weld on animation here!
                foreach (var cell in preview.GetUsedCells())
                {
                    if (Deleting)
                    {
                        WorkingMap.EraseCell(cell);
                    }
                    else
                    {
                        WorkingMap.SetCell(cell, 
                            preview.GetCellSourceId(cell), 
                            preview.GetCellAtlasCoords(cell), 
                            preview.GetCellAlternativeTile(cell));
                    }
                }
                preview.Clear();
            }
        }
    }

    public void SelectionChanged(int partId)
    {
        GridSnapping = false;
        CanRotate = false;
        RotationIdx = 0;
        
        if (partId == -1)
        {
            SelectedPart = null;
            return;
        }

        SelectedPart = _parts[partId];

        if (SelectedPart.Value.Tags.Contains("tile"))
        {
            GridSnapping = true;
        }

        if (SelectedPart.Value.Tags.Contains("can_rotate"))
        {
            CanRotate = true;
        }
    }

    private void UpdateWorkingMap()
    {
        if (SelectedPart == null || !SelectedPart.Value.Tags.Contains("tile"))
        {
            WorkingMap = null;
            return;
        }
        
        TileMapLayer tileMap = null;
        if (SelectedPart.Value.Tags.Contains("layer_floor"))
        {
            tileMap = GetNode<TileMapLayer>("Floor");
        } 
        else if (SelectedPart.Value.Tags.Contains("layer_wall"))
        {
            tileMap = GetNode<TileMapLayer>("Walls");
        }
        Debug.Assert(tileMap != null, "Selected tile lacks a layer tag!?!?");

        WorkingMap = tileMap;
    }
}