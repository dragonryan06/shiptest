using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using ShipTest.Core.Ecs;
using ShipTest.Destruction;
using ShipTest.Globals;

namespace ShipTest.Grids;

// https://docs.spacestation14.com/en/robust-toolbox/transform/grids.html
// If this system of doing polygon merging gets too laggy, might be worth it to implement
// exactly what RobustToolbox does here, with two levels of flood-fill algorithms instead.
public partial class GridBody : RigidBody2D, IEntity, IDestructible
{
    private const int ChunkSize = 16;

    private bool _mouseHover;
    private bool _mouseDrag;

    public Dictionary<Vector2I, GridChunk> Chunks { get; } = new();

    // IEntity
    public List<T> GetComponents<T>() where T : class
    {
        throw new NotImplementedException();
    }

    // IDestructible
    public void DestroyCell(Vector2I cell) // TODO all cell changes need to be piped through common methods eventually cause theres a lot that needs to be updated in every case (most of the time on a CallDeferred basis).
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));
        if (tileMap.GetCellSourceId(cell) != -1)
        {
            tileMap.EraseCell(cell);
            SetCenterOfMass();

            GenerateChunkCollisions(Chunks[TileToChunkPos(cell)]);

            UpdateFixtureGraph();
        }
    }

    public override void _Ready()
    {
        if (!HasNode(nameof(LayerNames.Floor)))
        {
            throw new InvalidOperationException("A GridBody instance must have at least a floor layer when readying!");
        }

        InitializeChunks();

        SetCenterOfMass();

        foreach (var chunk in Chunks.Values)
        {
            GenerateChunkCollisions(chunk);
        }

        UpdateFixtureGraph();

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton)
        {
            return;
        }

        switch (mouseButton.ButtonIndex)
        {
            case MouseButton.Left:

                if (_mouseHover && mouseButton.IsPressed())
                {
                    _mouseDrag = true;
                    Freeze = true;
                }
                else if (_mouseDrag)
                {
                    _mouseDrag = false;
                    Freeze = false;
                    ApplyCentralImpulse(Input.GetLastMouseVelocity());
                }

                break;

            case MouseButton.Right when _mouseHover && mouseButton.IsPressed():

                if (HasNode("ExplosionComponent")) // TODO: maybe an IEntity.HasComp(Component) would be cool? or maybe something that injects the component if it isn't present.
                {
                    GetNode<ExplosionComponent>("ExplosionComponent").StartExplosion(
                        GetNode<TileMapLayer>(nameof(LayerNames.Floor)).LocalToMap(GetLocalMousePosition()),
                        25);
                }
                else
                {
                    AddChild(new ExplosionComponent(
                        GetNode<TileMapLayer>(nameof(LayerNames.Floor)).LocalToMap(GetLocalMousePosition()),
                            25));
                }

                break;
        }
    }

    public override void _Process(double delta)
    {
        DebugDraw.Instance.Add(
            new DebugPoint(
                ToGlobal(CenterOfMass), new Color("#ff0000"), 
                DebugLayerFlags.Physics, 
                5f));

        DebugDraw.Instance.Add(
            new DebugLine(
                ToGlobal(CenterOfMass), ToGlobal(CenterOfMass) + LinearVelocity, 
                new Color("#0000ff"), 
                DebugLayerFlags.Physics, 
                1f));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_mouseDrag)
        {
            var collision = MoveAndCollide(GetLocalMousePosition());
            if (collision != null && collision.GetCollider() is RigidBody2D body)
            {
                body.ApplyCentralImpulse(-collision.GetNormal()*new Vector2(80,80));
            }
        }
    }

    public static Vector2I TileToChunkPos(Vector2I tilePos)
    {
        return new Vector2I(
            (int)Math.Floor(tilePos.X / (float)ChunkSize), 
            (int)Math.Floor(tilePos.Y / (float)ChunkSize));
    }

    public List<GridChunk> GetNeighboringChunksOf(Vector2I chunkPos)
    {
        Vector2I[] directions = [Vector2I.Left, Vector2I.Up, Vector2I.Right, Vector2I.Down];

        var neighbors = new List<GridChunk>();

        foreach (var d in directions)
        {
            if (Chunks.TryGetValue(chunkPos + d, out var chunk))
            {
                neighbors.Add(chunk);
            }
        }

        return neighbors;
    }

    private void InitializeChunks()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));
        var usedRect = tileMap.GetUsedRect();
        for (var y = TileToChunkPos(usedRect.Position).Y; y < TileToChunkPos(usedRect.Size).Y + 1; y++)
        {
            for (var x = TileToChunkPos(usedRect.Position).X; x < TileToChunkPos(usedRect.Size).X + 1; x++)
            {
                var chunk = new GridChunk(
                    $"GridChunk({x}, {y})",
                    new Rect2I(x * ChunkSize, y * ChunkSize, ChunkSize, ChunkSize),
                    tileMap.TileSet.TileSize);
                Chunks.Add(new Vector2I(x, y), chunk);
            }
        }
    }

    private void SetCenterOfMass()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));
        CenterOfMassMode = CenterOfMassModeEnum.Custom;

        var usedRect = tileMap.GetUsedRect();
        CenterOfMass = new Rect2(
            tileMap.MapToLocal(usedRect.Position) - new Vector2(0.75f, 0.75f) * tileMap.TileSet.TileSize, 
            tileMap.MapToLocal(usedRect.Size)).GetCenter();
    }

    private void GenerateChunkCollisions(GridChunk chunk)
    {
        foreach (var fixture in chunk.Fixtures)
        {
            RemoveChild(fixture);
            fixture.QueueFree();
        }

        chunk.Fixtures.Clear();

        var fixtures = chunk.GenerateCollisions(GetNode<TileMapLayer>(nameof(LayerNames.Floor)));
        fixtures.ForEach(f => AddChild(f));
    }

    private void UpdateFixtureGraph()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));
        var aStarGrid = new AStarGrid2D();

        foreach (var chunkPos in Chunks.Keys.Where(k => Chunks[k].IsDirty))
        {
            foreach (var fixture in Chunks[chunkPos].Fixtures)
            {
                foreach (var neighborChunk in GetNeighboringChunksOf(chunkPos))
                {
                    aStarGrid.Region = Chunks[chunkPos].Bounds.Merge(neighborChunk.Bounds);
                    aStarGrid.Update();

                    aStarGrid.FillSolidRegion(aStarGrid.Region);

                    foreach (var cell in tileMap.GetUsedCells().Where(c => aStarGrid.Region.HasPoint(c)))
                    {
                        aStarGrid.SetPointSolid(cell, false);
                    }

                    foreach (var otherFixture in neighborChunk.Fixtures)
                    {
                        if (aStarGrid.GetIdPath(fixture.ReferenceCell, otherFixture.ReferenceCell).Count != 0)
                        {
                            fixture.Neighbors.Add(otherFixture);
                            otherFixture.Neighbors.Add(fixture);
                        }
                    }
                }
            }

            Chunks[chunkPos].IsDirty = false;
        }
    }

    private void OnMouseEntered()
    {
        _mouseHover = true;
    }

    private void OnMouseExited()
    {
        _mouseHover = false;
    }
}

public enum LayerNames
{
    Floor,
    Walls
}