using System.Collections.Generic;
using Godot;
using ShipTest.Core.Ecs;

namespace ShipTest.Grids;

[GlobalClass]
public partial class TileEntity : Node2D, IEntity
{
    [Export]
    public Vector2I TilePosition { get; set; }
    
    public List<T> GetComponents<T>() where T : class, IComponent
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetComponent<T>(out T component) where T : class, IComponent
    {
        var found = false;
        component = null;
        
        for (var i = 0; i < GetChildCount() && !found; i++)
        {
            component = GetChildOrNull<T>(i);
            
            if (component != null)
            {
                found = true;
            }
        }

        return found;
    }
}