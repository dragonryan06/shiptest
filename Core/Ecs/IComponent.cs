
namespace ShipTest.Core.Ecs;

// This is a sort of pseudo-ECS, IComponent implementers should always be children of IEntity implementers.
public interface IComponent
{
    /// <summary>
    /// Get this component's Entity as the type T. In most cases
    /// this is probably as simple as a call to GetParent() with a cast.
    /// </summary>
    public T GetEntity<T>() where T : class;
}
