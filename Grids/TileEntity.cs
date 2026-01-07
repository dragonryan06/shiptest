using System.Collections.Generic;
using Godot;
using ShipTest.Core.Ecs;

namespace ShipTest.Grids;

[GlobalClass]
public partial class TileEntity : Node2D, IEntity
{
    private const int TileSize = 32;
    
    public Vector2I TilePosition { get; private set; }
    
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

    public override void _Ready()
    {
        TilePosition = (Vector2I)((Position - new Vector2(TileSize/2.0f, TileSize/2.0f)) / TileSize);
    }
}