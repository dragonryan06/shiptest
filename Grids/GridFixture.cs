using System.Collections.Generic;
using Godot;
using ShipTest.Globals;

namespace ShipTest.Grids;

public partial class GridFixture : CollisionPolygon2D
{
    public GridFixture(string name, Vector2[] polygon, Vector2I referenceCell)
    {
        Name = name;
        Polygon = polygon;
        ReferenceCell = referenceCell;
    }

    // All neighbors must be on other chunks, otherwise they'd be a part of this fixture.
    public List<GridFixture> Neighbors { get; set; } = [];
    
    // Dirty fixtures need to be checked for disconnection from the larger graph.
    public bool IsDirty { get; set; } = false;

    // An example position inside this fixture for testing connection.
    public Vector2I ReferenceCell { get; set; }

    // Used for debug drawing and stuff (Warning! pretty expensive! probably want to disable debug calls to this when they're not visible)
    public Vector2 Center
    {
        get
        {
            var c = Vector2.Zero;

            foreach (var p in Polygon)
            {
                c += ToGlobal(p);
            }

            c /= Polygon.Length;

            return c;
        }
    }

    public override void _Process(double delta)
    {
        DebugDraw.Instance.Add(new DebugPoint(
            Center,
            IsDirty
                ? new Color("#ff0000")
                : new Color("#00ff00"),
            DebugLayerFlags.Grids,
            5f));

        foreach (var n in Neighbors)
        {
            // all the lines will be doubled this way but idc
            DebugDraw.Instance.Add(new DebugLine(
                Center,
                n.Center,
                new Color("#ffff00"),
                DebugLayerFlags.Grids));
        }
    }
}
