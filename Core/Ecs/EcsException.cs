using System;

namespace ShipTest.Core.Ecs;

[Serializable]
public class EcsException : Exception
{
    public EcsException()
    {}

    public EcsException(string message)
        : base(message)
    {}

    public EcsException(string message, Exception inner)
        : base(message, inner)
    {}
}
