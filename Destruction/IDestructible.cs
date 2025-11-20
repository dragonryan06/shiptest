using System.Collections.Generic;
using Godot;

namespace ShipTest.Destruction;

// Might want to change this in the future but for now, this should only be implemented by tile-based stuff.
public interface IDestructible
{
    // TODO: Could have this receive a Damage object or something in the future on a DamageCell() method.

    public bool IsCellExplosionObstacle(Vector2I cell);
    
    public void DestroyCell(Vector2I cell);
}
