using System;

namespace ShipTest.Globals;

public class DebugOptions
{
    private static readonly DebugOptions _instance = new();
    private Flags _state = Flags.None;

    private DebugOptions()
    { // singleton
    }

    public static DebugOptions GetInstance()
    {
        return _instance;
    }

    public void SetFlag(Flags flag)
    {
        _state |= flag;
    }

    public bool IsSet(Flags flag)
    {
        return (_state & flag) != 0;
    }

    [Flags]
    public enum Flags
    {
        None = 0x0,
        CenterOfMass = 0x1,
        MovementVectors = 0x2
    }
}
