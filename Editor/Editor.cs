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
    private const int UnderWallSourceId = 1;

    private readonly Color _placingColor = new("#00ff00");
    private readonly Color _deletingColor = new("#ff0000");
    private readonly List<EditorPartInfo> _parts;
    
    // If we aren't placing a tile, we are placing an entity.
    private bool PlacingTile { get; set; }
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
                if (ActiveMap == null)
                {
                    return;
                }
                
                GetNode<TileMapLayer>("PlacePreview").Clear();
            }
            else
            {
                GetNode<TileMapLayer>("DeletePreview").Clear();
            }
        }
    }
    
    // ActiveMap previews using the preview maps, while ActiveEntity is its own preview (using Modulate)
    private TileMapLayer ActiveMap { get; set; }
    
    private TileEntity ActiveEntity { get; set; }

    private EditorPartInfo? _selectedPart;
    
    private EditorPartInfo? SelectedPart
    {
        get => _selectedPart;
        set
        {
            _selectedPart = value;
            UpdateActiveMap();
            UpdateActiveEntity();
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

        var placePreview = GetNode<TileMapLayer>("PlacePreview");
        var deletePreview = GetNode<TileMapLayer>("DeletePreview");

        if (!Dragging)
        {
            deletePreview.Clear();
            placePreview.Clear();
        }
        
        if (Deleting)
        {
            deletePreview.SetCell(deletePreview.LocalToMap(deletePreview.GetLocalMousePosition()), 0, Vector2I.Zero);
            return;
        }

        if (SelectedPart.Value.Tags.Contains("entity"))
        {
            var floor = GetNode<TileMapLayer>("WorkingDocument/Floor");
            ActiveEntity.TilePosition = floor.LocalToMap(GetLocalMousePosition());
            ActiveEntity.Position = floor.MapToLocal(ActiveEntity.TilePosition);
            
            if (SelectedPart.Value.Tags.Contains("snap_floor"))
            {
                ActiveEntity.Modulate = floor.GetCellSourceId(ActiveEntity.TilePosition) != -1
                    ? _placingColor
                    : _deletingColor;
            }

            return;
        }

        placePreview.SetCell(
            placePreview.LocalToMap(placePreview.GetLocalMousePosition()),
            SelectedPart.Value.SourceId,
            SelectedPart.Value.Tags.Contains("can_rotate")
                ? SelectedPart.Value.Orientations[RotationIdx]
                : SelectedPart.Value.AtlasPosition);

        if (SelectedPart.Value.Terrain != -1)
        {
            placePreview.SetCellsTerrainConnect(placePreview.GetUsedCells(), SelectedPart.Value.Terrain, 0);
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

            if (ActiveEntity != null)
            {
                ActiveEntity.Rotation = float.Tau * RotationIdx / 4.0f;
            }
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
                        : GetNode<TileMapLayer>("PlacePreview");
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
                            ActiveMap.SetCellsTerrainConnect([cell], SelectedPart.Value.Terrain, -1);
                            continue;
                        }
                        
                        ActiveMap.EraseCell(cell);
                    }
                    else
                    {
                        ActiveMap.SetCell(cell, 
                            preview.GetCellSourceId(cell), 
                            preview.GetCellAtlasCoords(cell), 
                            preview.GetCellAlternativeTile(cell));

                        if (ActiveMap.Name == "Walls")
                        {
                            GetNode<TileMapLayer>("WorkingDocument/Floor").SetCell(cell,UnderWallSourceId,Vector2I.Zero);
                        }
                    }
                }
                
                if (SelectedPart.Value.Terrain != -1)
                {
                    ActiveMap.SetCellsTerrainConnect(ActiveMap.GetUsedCells(), SelectedPart.Value.Terrain, 0);
                }
                preview.Clear();

                if (Deleting)
                {
                    Deleting = false;
                }
            }
        }
    }
    
    private void UpdateActiveMap()
    {
        if (SelectedPart == null || !SelectedPart.Value.Tags.Contains("tile"))
        {
            ActiveMap = null;
            return;
        }
        
        TileMapLayer tileMap = null;
        if (SelectedPart.Value.Tags.Contains("layer_floor"))
        {
            tileMap = GetNode<TileMapLayer>("WorkingDocument/Floor");
        }
        else if (SelectedPart.Value.Tags.Contains("layer_wall"))
        {
            tileMap = GetNode<TileMapLayer>("WorkingDocument/Walls");
        }
        Debug.Assert(tileMap != null, "Selected tile lacks a layer tag!?!?");

        ActiveMap = tileMap;
        GetNode<TileMapLayer>("PlacePreview").TileSet = tileMap.TileSet;
    }

    private void UpdateActiveEntity()
    {
        if (SelectedPart == null || !SelectedPart.Value.Tags.Contains("entity"))
        {
            ActiveEntity?.QueueFree();
            ActiveEntity = null;
            return;
        }

        ActiveEntity = GD.Load<PackedScene>(SelectedPart.Value.ScenePath).Instantiate<TileEntity>();
        ActiveEntity.Modulate = _placingColor;

        if (SelectedPart.Value.Tags.Contains("snap_floor"))
        {
            GetNode<TileMapLayer>("WorkingDocument/Floor").AddChild(ActiveEntity);
        }
        else
        {
            GetNode<ShipBlueprint>("WorkingDocument").AddChild(ActiveEntity);
        }
    }

    private void NewDocument()
    {
        GetNode<ShipBlueprint>("WorkingDocument").Clear();
    }

    private void OnSelectionChanged(int partId)
    {
        CanRotate = false;
        RotationIdx = 0;

        if (SelectedPart != null)
        {
            var preview = Deleting
                ? GetNode<TileMapLayer>("DeletePreview")
                : GetNode<TileMapLayer>("PlacePreview");
            preview.Clear();
        }
        
        if (partId == -1)
        {
            SelectedPart = null;
            return;
        }

        SelectedPart = _parts[partId];

        if (SelectedPart.Value.Tags.Contains("can_rotate"))
        {
            CanRotate = true;
        }
    }

    private void OnNameChanged(string newName)
    {
        GetNode<ShipBlueprint>("WorkingDocument").ShipName = newName;
    }

    private void OnFileNew() => NewDocument();
    
    private void OnFileOpen(string fileName)
    {
        var scene = ResourceLoader.Load<PackedScene>(fileName);

        if (scene == null)
        {
            GD.PrintErr($"Failed to load file '{fileName}'!");
            return;
        }

        var oldScene = GetNode<ShipBlueprint>("WorkingDocument");
        RemoveChild(oldScene);

        var newScene = scene.Instantiate<ShipBlueprint>();
        newScene.Name = "WorkingDocument";
        AddChild(newScene);
        MoveChild(newScene, 0);

        oldScene.QueueFree();
        
        // Todo emit some signal telling stuff like the name lineedit there's a new model.
    }

    private void OnFileSave(string fileName)
    {
        var scene = new PackedScene();
        var packResult = scene.Pack(GetNode<ShipBlueprint>("WorkingDocument"));

        if (packResult != Error.Ok)
        {
            GD.PrintErr($"Failed packing WorkingDocument to scene! {packResult.ToString()}");
            return;
        }

        var saveResult = ResourceSaver.Save(scene, fileName);

        if (saveResult != Error.Ok)
        {
            GD.PrintErr($"Failed saving WorkingDocument PackedScene to file '{fileName}'! {saveResult.ToString()}");
        }
    }
}