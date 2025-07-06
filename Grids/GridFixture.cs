using System.Collections.Generic;
using Godot;
using ShipTest.Globals;

namespace ShipTest.Grids;

public partial class GridFixture : CollisionPolygon2D
{
    private bool _disposed;
    private Vector2 _referenceCellGlobalPos;

    public GridFixture(
        string name, 
        Vector2[] polygon, 
        Vector2I referenceCell, 
        Vector2 referenceCellGlobalPos)
    {
        Name = name;
        Polygon = polygon;
        ReferenceCell = referenceCell;
        ReferenceCellGlobalPos = referenceCellGlobalPos;
    }

    // All neighbors must be on other chunks, otherwise they'd be a part of this fixture.
    public List<GridFixture> Neighbors { get; set; } = [];

    // An example position inside this fixture for testing connection.
    public Vector2I ReferenceCell { get; set; }

    // For debug drawing
    public Vector2 ReferenceCellGlobalPos
    {
        get => ToGlobal(_referenceCellGlobalPos);
        set => _referenceCellGlobalPos = value;
    }

    // Used for debug drawing and stuff (Warning! pretty expensive! probably want to disable debug calls to this when they're not visible)
    //public Vector2 Center
    //{
    //    get
    //    {
    //        var c = Vector2.Zero;

    //        foreach (var p in Polygon)
    //        {
    //            c += ToGlobal(p);
    //        }

    //        c /= Polygon.Length;

    //        return c;
    //    }
    //}

    public override void _Process(double delta)
    {
        DebugDraw.Instance.Add(new DebugPoint(
            ReferenceCellGlobalPos,
            new Color("#00ff00"),
            DebugLayerFlags.Grids,
            5f));

        foreach (var n in Neighbors)
        {
            // all the lines will be doubled this way but idc
            DebugDraw.Instance.Add(new DebugLine(
                ReferenceCellGlobalPos,
                n.ReferenceCellGlobalPos,
                new Color("#ffff00"),
                DebugLayerFlags.Grids));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            base.Dispose(disposing);
        }

        foreach (var neighbor in Neighbors)
        {
            neighbor.Neighbors.Remove(this);
        }

        _disposed = true;
    }
}
