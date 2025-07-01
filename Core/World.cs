using Godot;
using ShipTest.Globals;

namespace ShipTest.Core;
public partial class World : Node2D
{
    public override void _Ready()
    {
        DebugOptions.GetInstance().SetFlag(DebugOptions.Flags.CenterOfMass);
        DebugOptions.GetInstance().SetFlag(DebugOptions.Flags.MovementVectors);
    }
}
