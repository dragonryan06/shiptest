using Godot;
using ShipTest.Core.Ecs;

namespace ShipTest.Ships;

// Extends AnimatedSprite cause this node also serves as the flame/exhaust sprite.
[GlobalClass]
public partial class ThrusterComponent : AnimatedSprite2D, IComponent
{
    private readonly StringName _enabledAnimation = new("enabled");
    private readonly StringName _disabledAnimation = new("disabled");
    
    private bool _enabled = true;
    private bool _thrusting;

    [Export]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            
            if (value == false)
            {
                Thrusting = false;
            }

            if (GetParent().GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D") is not { } sprite) return;
            
            sprite.Animation = value
                ? _enabledAnimation
                : _disabledAnimation;
        }
    }
    
    [Export]
    public bool Thrusting
    {
        get => _thrusting;
        set
        {
            _thrusting = value;

            Visible = value;
        }
    }

    public T GetEntity<T>() where T : class
    {
        return GetParentOrNull<T>() ?? throw new EcsException($"Component {nameof(ThrusterComponent)} has no parent Entity!");
    }
}