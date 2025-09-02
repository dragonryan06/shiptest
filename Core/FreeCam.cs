using Godot;

namespace ShipTest.Core;
public partial class FreeCam : Camera2D
{
    private const float ZoomStep = 0.1f;
    private const float ZoomMax = 5.0f;
    private const float ZoomMin = 0.2f;

    private bool _dragging;
    private float _zoomSetpoint = 1.0f;
    
    public override void _Input(InputEvent @event)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            Input.SetDefaultCursorShape(Input.CursorShape.Drag);
            _dragging = true;
        }
        else
        {
            Input.SetDefaultCursorShape();
            _dragging = false;
        }

        if (_dragging && @event is InputEventMouseMotion motion)
        {
            Position -= motion.Relative * new Vector2(1.0f/Zoom.X, 1.0f/Zoom.Y);
        }

        if (@event is InputEventMouseButton button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    _zoomSetpoint += ZoomStep;
                    break;
                case MouseButton.WheelDown:
                    _zoomSetpoint -= ZoomStep;
                    break;
            }
        }
    }

    public override void _Process(double delta)
    {
        _zoomSetpoint = Mathf.Clamp(_zoomSetpoint, ZoomMin, ZoomMax);
        Zoom = Zoom.Lerp(new Vector2(_zoomSetpoint, _zoomSetpoint), (float)delta*4.0f);
    }
}
