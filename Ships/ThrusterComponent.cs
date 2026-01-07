using Godot;
using ShipTest.Core.Ecs;

namespace ShipTest.Ships;

// Extends AnimatedSprite cause this node also serves as the flame/exhaust sprite.
[GlobalClass]
public partial class ThrusterComponent : AnimatedSprite2D, IComponent
{
    public T GetEntity<T>() where T : class
    {
        return GetParentOrNull<T>() ?? throw new EcsException($"Component {nameof(ThrusterComponent)} has no parent Entity!");
    }
}