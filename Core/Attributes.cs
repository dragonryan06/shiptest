using System;

namespace ShipTest.Core;

[AttributeUsage(AttributeTargets.Class)]
public sealed class Entity : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class Component : Attribute;