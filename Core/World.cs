using Godot;
using ShipTest.Globals;

namespace ShipTest.Core;
public partial class World : Node2D
{
    public override void _Ready()
    {
        // TODO: big problem rn that debug stuff will still be being calculated even if the layer isnt drawing.
        DebugDraw.Instance.LayerState |= DebugLayerFlags.General;
        DebugDraw.Instance.LayerState |= DebugLayerFlags.Physics;
    }

    public override void _Process(double delta)
    {
        DebugDraw.Instance.Add(new DebugText(
            -GetViewport().GetVisibleRect().Position-GetViewport().GetVisibleRect().Size/2.0f + new Vector2(8,16),
            new Color("#aaaaaa"),
            $"FPS: {Engine.GetFramesPerSecond()}",
            fontSize: 16));
    }
}