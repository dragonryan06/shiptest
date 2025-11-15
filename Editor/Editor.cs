using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using ShipTest.Grids;
using ShipTest.Serialization;
using Array = Godot.Collections.Array;

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
                if (WorkingMap == null)
                {
                    return;
                }
                
                WorkingMap.GetNode<TileMapLayer>("Preview").Clear();
            }
            else
            {
                GetNode<TileMapLayer>("DeletePreview").Clear();
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
    
    public ShipBlueprint Blueprint { get; set; }

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
                if (!p.AsGodotDictionary().TryGetValue("id", out var id) || id.AsInt32() == -1)
                {
                    // Giving a part id "-1" is the ideal way to have it ignored.
                    continue;
                }
                
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

        NewDocument();

        var hud = GetNode<CanvasLayer>("HUD");
        hud.Connect("selection_changed", new Callable(this, MethodName.OnSelectionChanged));
        hud.Connect("name_changed", new Callable(this, MethodName.OnNameChanged));
        hud.Connect("new_file", new Callable(this, MethodName.OnFileNew));
        hud.Connect("open_file", new Callable(this, MethodName.OnFileOpen));
        hud.Connect("save_file", new Callable(this, MethodName.OnFileSave));
    }

    public override void _Process(double delta)
    {
        if (SelectedPart == null)
        {
            return;
        }

        var preview = Deleting
                ? GetNode<TileMapLayer>("DeletePreview")
                : WorkingMap.GetNode<TileMapLayer>("Preview");
        
        if (!Dragging)
        {
            preview.Clear();
        }

        if (Deleting)
        {
            preview.SetCell(preview.LocalToMap(preview.GetLocalMousePosition()), 0, Vector2I.Zero);
            return;
        }
        
        preview.SetCell(
            preview.LocalToMap(preview.GetLocalMousePosition()),
            SelectedPart.Value.SourceId,
            SelectedPart.Value.Tags.Contains("can_rotate")
                ? SelectedPart.Value.Orientations[RotationIdx]
                : SelectedPart.Value.AtlasPosition);
        
        if (SelectedPart.Value.Terrain != -1)
        {
            preview.SetCellsTerrainConnect(preview.GetUsedCells(), SelectedPart.Value.Terrain, 0);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("editor_rotate") && SelectedPart != null && CanRotate)
        {
            if (++RotationIdx == 4)
            {
                RotationIdx = 0;
            }

            // Insert logic for non-tile parts
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right } mouseButton) 
        {
            Deleting = mouseButton.ButtonIndex == MouseButton.Right;

            if (!Deleting && SelectedPart == null)
            {
                return;
            }
            
            if (mouseButton.Pressed)
            {
                Dragging = true;
            }
            else
            {
                Dragging = false;

                var preview = Deleting
                        ? GetNode<TileMapLayer>("DeletePreview")
                        : WorkingMap.GetNode<TileMapLayer>("Preview");
                if (preview.TileMapData.IsEmpty() || SelectedPart == null)
                {
                    return;
                }
                
                // I would love to eventually have a like fly-in and then weld on animation here!
                foreach (var cell in preview.GetUsedCells())
                {
                    if (Deleting)
                    {
                        if (SelectedPart.Value.Terrain != -1)
                        {
                            WorkingMap.SetCellsTerrainConnect([cell], SelectedPart.Value.Terrain, -1);
                            continue;
                        }
                        
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
                
                if (SelectedPart.Value.Terrain != -1)
                {
                    WorkingMap.SetCellsTerrainConnect(WorkingMap.GetUsedCells(), SelectedPart.Value.Terrain, 0);
                }
                preview.Clear();

                if (Deleting)
                {
                    Deleting = false;
                }
            }
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

    private void NewDocument()
    {
        Blueprint = new ShipBlueprint
        {
            Name = "Unnamed Ship",
            GridLayers = new Dictionary<string, byte[]>()
        };
        foreach (var layer in Enum.GetNames(typeof(LayerNames)))
        {
            var node = GetNode<TileMapLayer>(layer);
            node.Clear();
            Blueprint.GridLayers[layer] = node.TileMapData;
        }
    }

    private void OnSelectionChanged(int partId)
    {
        GridSnapping = false;
        CanRotate = false;
        RotationIdx = 0;

        if (SelectedPart != null)
        {
            var preview = Deleting
                ? GetNode<TileMapLayer>("DeletePreview")
                : WorkingMap.GetNode<TileMapLayer>("Preview");
            preview.Clear();
        }
        
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

    private void OnNameChanged(string newName)
    {
        Blueprint.Name = newName;
    }

    private void OnFileNew()
    {
        NewDocument();
    }
    
    private void OnFileOpen(string fileName)
    {
        if (SerializationService.ReadObjectFromFile<ShipBlueprint>(fileName, out var blueprint))
        {
            Blueprint = blueprint;
            foreach (var layer in blueprint.GridLayers)
            {
                GetNode<TileMapLayer>(layer.Key).TileMapData = layer.Value;
            }
        }
        else
        {
            GD.PrintErr($"Failed to load file '{fileName}'!");
        }
    }

    private void OnFileSave(string fileName)
    {
        foreach (var layer in Enum.GetNames(typeof(LayerNames)))
        {
            var node = GetNode<TileMapLayer>(layer);
            Blueprint.GridLayers[layer] = node.TileMapData;
        }

        if (!SerializationService.WriteObjectToFile(fileName, Blueprint))
        {
            GD.PrintErr($"Failed to save file '{fileName}'!");
        }
    }
}