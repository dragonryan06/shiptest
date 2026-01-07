using System.Collections.Generic;
using Godot;
using ShipTest.Core.Ecs;

namespace ShipTest.Grids;

[GlobalClass]
public partial class TileEntity : Node2D, IEntity
{
    public List<T> GetComponents<T>() where T : class
    {
        throw new System.NotImplementedException();
    }
}