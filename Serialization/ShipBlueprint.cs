using System;
using System.Collections.Generic;
using Godot;

namespace ShipTest.Serialization;

public class ShipBlueprint
{
    public string Name { get; set; }
    
    public Dictionary<string, TileMapLayer> GridLayers { get; set; }
}