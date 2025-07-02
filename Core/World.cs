using Godot;
using ShipTest.Globals;

namespace ShipTest.Core;
public partial class World : Node2D
{
    public override void _Ready()
    {
        DebugDraw.Instance.LayerState |= DebugLayerFlags.General;
        DebugDraw.Instance.LayerState |= DebugLayerFlags.Physics;
    }
}
