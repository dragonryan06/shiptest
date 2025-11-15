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
        // TODO: big problem rn that debug stuff will still be being calculated even if the layer isnt drawing.
        DebugDraw.Instance.LayerState |= DebugLayers;

        GetNode<Control>("HUD/DebugOverlay").Connect("debug_spawning", new Callable(this, MethodName.OnDebugSpawning));
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _Input(InputEvent @event)
    {
        if (_debugSpawning && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton)
        {
            _debugSpawning = false;

            _loadedBody.Position = mouseButton.Position;
            AddChild(_loadedBody);
        }
        else if (_debugSpawning && @event is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            _debugSpawning = false;
        }
    }

    private void OnDebugSpawning(string fileName)
    {
        if (!SerializationService.ReadObjectFromFile<ShipBlueprint>(fileName, out var blueprint))
        {
            GD.PrintErr("Failed to debug spawn!");
            return;
        }
        
        _debugSpawning = true;
        _loadedBody = blueprint.ToGridBody();
    }
}