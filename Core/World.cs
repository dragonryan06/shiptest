using Godot;
using ShipTest.Globals;
using ShipTest.Grids;
using ShipTest.Serialization;

namespace ShipTest.Core;
public partial class World : Node2D
{
    private bool _debugSpawning;
    private GridBody _loadedBody;
    
    [Export(PropertyHint.Flags)] 
    public DebugLayerFlags DebugLayers { get; set; }
    
    public override void _Ready()
    {
        DebugDraw.Instance.LayerState |= DebugLayers;

        GetNode<Control>("HUD/DebugOverlay").Connect("debug_spawning", new Callable(this, MethodName.OnDebugSpawning));
    }

    public override void _Process(double delta)
    {
        if (_debugSpawning)
        {
            _loadedBody.Position = GetGlobalMousePosition();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_debugSpawning && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton)
        {
            _debugSpawning = false;

            _loadedBody.Freeze = false;
            _loadedBody.Modulate = Color.FromHtml("#ffffff");
            _loadedBody = null;
        }
        else if (_debugSpawning && @event is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            _debugSpawning = false;
        }
    }

    private void OnDebugSpawning(string fileName)
    {
        var scene = ResourceLoader.Load<PackedScene>(fileName);
        
        if (scene == null)
        {
            GD.PrintErr("Failed to load PackedScene while debug spawning!");
            return;
        }

        var blueprint = scene.Instantiate<ShipBlueprint>();

        if (blueprint == null)
        {
            GD.PrintErr("Failed to instantiate loaded scene as ShipBlueprint while debug spawning!");
            return;
        }
        
        _debugSpawning = true;
        _loadedBody = blueprint.ToGridBody();
        _loadedBody.Freeze = true;
        _loadedBody.Modulate = Color.FromHtml("#00ff00");
        AddChild(_loadedBody);
    }
}