using System.Collections.Generic;

namespace ShipTest.Core.Ecs;

// This is a sort of pseudo-ECS, IComponent implementers should always be children of IEntity implementers.
public interface IEntity
{
    /// <summary>
    /// Get all attached components to this Entity as the type T.
    /// </summary>
    public List<T> GetComponents<T>() where T : class, IComponent;

    /// <summary>
    /// Try to get a specific component from this Entity, should
    /// be more performant than searching through GetComponents()
    /// </summary>
    public bool TryGetComponent<T>(out T component) where T : class, IComponent;
}
