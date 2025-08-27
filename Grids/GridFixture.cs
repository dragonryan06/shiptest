using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Core.Graphs;
using ShipTest.Globals;

namespace ShipTest.Grids;

public partial class GridFixture : CollisionPolygon2D, IGraphNode
{
    private bool _disposed;
    private Vector2 _referenceCellGlobalPos;

    public GridFixture(
        string name, 
        Vector2[] polygon, 
        List<Vector2I> containedCells, 
        Vector2 referenceCellGlobalPos)
    {
        Name = name;
        Polygon = polygon;
        ContainedCells = containedCells;
        ReferenceCellGlobalPos = referenceCellGlobalPos;
    }

    public GridFixture()
    {
        Name = "UninitializedFixture";
    }

    // IGraphNode
    // All neighbors must be on other chunks, otherwise they'd be a part of this fixture.
    public List<IGraphNode> Neighbors { get; set; } = [];
    
    public bool Visited { get; set; }
    
    public List<Vector2I> ContainedCells { get; }
    
    // An example position inside this fixture for testing connection.
    public Vector2I ReferenceCell => ContainedCells[0];

    // For debug drawing, should be this.ToGlobal(TileMap.MapToLocal(ContainedCells[0]))
    public Vector2 ReferenceCellGlobalPos
    {
        get => ToGlobal(_referenceCellGlobalPos);
        set => _referenceCellGlobalPos = value;
    }

    public List<IGraphNode> DetermineNeighbors()
    {
        var parentBody = (GridBody)GetParent();
        var aStarGrid = new AStarGrid2D();
        var chunkPos = GridBody.TileToChunkPos(ReferenceCell);
        
        var result = new List<IGraphNode>();
        
        foreach (var neighborChunk in parentBody.GetNeighboringChunksOf(chunkPos))
        {
            aStarGrid.Region = parentBody.Chunks[chunkPos].Bounds.Merge(neighborChunk.Bounds);
            aStarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
            aStarGrid.Update();

            aStarGrid.FillSolidRegion(aStarGrid.Region);

            foreach (var cell in parentBody.GetNode<TileMapLayer>(nameof(LayerNames.Floor))
                         .GetUsedCells().Where(c => aStarGrid.Region.HasPoint(c)))
            {
                aStarGrid.SetPointSolid(cell, false);
            }

            result.AddRange(neighborChunk.Fixtures.Where(otherFixture => aStarGrid.GetIdPath(ReferenceCell, otherFixture.ReferenceCell).Count != 0));
        }

        return result;
    }

    public override void _Process(double delta)
    {
        DebugDraw.Instance.Add(new DebugPoint(
            ReferenceCellGlobalPos,
            new Color("#00ff00"),
            DebugLayerFlags.Grids,
            5f));

        foreach (var n in Neighbors)
        {
            var other = (GridFixture)n;
            // all the lines will be doubled this way but idc
            DebugDraw.Instance.Add(new DebugLine(
                ReferenceCellGlobalPos,
                other.ReferenceCellGlobalPos,
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
