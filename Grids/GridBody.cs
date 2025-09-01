using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Core.Ecs;
using ShipTest.Core.Graphs;
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

    public Dictionary<Vector2I, GridChunk> Chunks { get; } = new();

    public Graph<GridFixture> FixtureGraph { get; } = new();

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

        chunk.Fixtures.ForEach(f => FixtureGraph.RemoveNode(f));
        chunk.Fixtures.Clear();

        var fixtures = chunk.GenerateCollisions(GetNode<TileMapLayer>(nameof(LayerNames.Floor)));
        fixtures.ForEach(f =>
        {
            AddChild(f);
            FixtureGraph.AddNode(f, false);
        });
    }

    private void UpdateFixtureGraph()
    {
        foreach (var chunkPos in Chunks.Keys.Where(k => Chunks[k].IsDirty))
        {
            Chunks[chunkPos].Fixtures.ForEach(fixture => FixtureGraph.UpdateNeighborsOf(fixture));
            Chunks[chunkPos].IsDirty = false;
        }

        HandlePotentialDisconnection();

        return;

        void HandlePotentialDisconnection()
        {
            var components = FixtureGraph.GetConnectedComponents();

            if (components.Count == 1)
            {
                return;
            }

            // iterate over the components and create new GridBodys as necessary
            foreach (var comp in components.Skip(1))
            {
                var oldFloor = GetNode<TileMapLayer>(nameof(LayerNames.Floor));

                var newBody = new GridBody
                {
                    Name = $"Debris ({Name})",
                    Position = Position,
                    Rotation = Rotation,
                    LinearVelocity = LinearVelocity,
                    AngularVelocity = AngularVelocity,
                };
                var newMap = (TileMapLayer)oldFloor.Duplicate();
                newMap.Clear();

                foreach (var fixture in comp)
                {
                    FixtureGraph.RemoveNode(fixture);
                    RemoveChild(fixture);
                    
                    foreach (var cell in fixture.ContainedCells)
                    {
                        newMap.SetCell(
                            cell, 
                            oldFloor.GetCellSourceId(cell), 
                            oldFloor.GetCellAtlasCoords(cell),
                            oldFloor.GetCellAlternativeTile(cell));
                        
                        oldFloor.EraseCell(cell);
                    }
                }

                // this one just for the debug explosions
                newBody.InputPickable = true;
                
                newBody.AddChild(newMap);
                AddSibling(newBody);
            }
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