using System.Collections.Generic;

namespace ShipTest.Core.Ecs;

// This is a sort of pseudo-ECS, IComponent implementers should always be children of IEntity implementers.
public interface IEntity
{
    /// <summary>
    /// Get all attached components to this Entity as the type T
    /// (in most cases T will probably have to be IComponent).
    /// </summary>
    public List<T> GetComponents<T>() where T : class;
}
