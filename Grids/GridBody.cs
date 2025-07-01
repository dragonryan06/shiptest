using System;
using System.Collections.Generic;
using Godot;
using ShipTest.Globals;

namespace ShipTest.Grids;

public partial class GridBody : RigidBody2D
{
    private const int ChunkSize = 16;

    private bool _mouseHover;
    private bool _mouseDrag;

    public Dictionary<Vector2I, GridChunk> Chunks { get; } = new();

    public override void _Ready()
    {
        if (!HasNode(nameof(LayerNames.Floor)))
        {
            throw new InvalidOperationException("A GridBody instance must have at least a floor layer when readying!");
        }

        InitializeChunks();

        // We do this so the RigidBody center is in the center of the tilemap.
        RecenterTilemap();

        GenerateCollisions();

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (_mouseHover && mouseButton.IsPressed())
            {
                _mouseDrag = true;
                Freeze = true;
            } else if (_mouseDrag)
            {
                _mouseDrag = false;
                Freeze = false;
                ApplyCentralImpulse(Input.GetLastMouseVelocity());
            }
        }
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

    public override void _Draw()
    {
        if (DebugOptions.GetInstance().IsSet(DebugOptions.Flags.CenterOfMass))
        {
            DrawCircle(Position, 5f, new Color("#ff0000"));
        }

        if (DebugOptions.GetInstance().IsSet(DebugOptions.Flags.MovementVectors))
        {
            DrawLine(Position, LinearVelocity, new Color("#0000ff"), 1f);
        }
    }

    public static Vector2I TileToChunkPos(Vector2I tilePos)
    {
        return new Vector2I(
            (int)Math.Floor(tilePos.X / (float)ChunkSize), 
            (int)Math.Floor(tilePos.Y / (float)ChunkSize));
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
                    new Rect2I(x * ChunkSize, y * ChunkSize, ChunkSize, ChunkSize),
                    tileMap.TileSet.TileSize);
                chunk.Name = $"Chunk{x}_{y}";
                Chunks.Add(chunk.Bounds.Position, chunk);
            }
        }
    }

    private void RecenterTilemap()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));

        tileMap.Position -= tileMap.GetUsedRect().End / new Vector2(2, 2);
    }

    private void GenerateCollisions()
    {
        foreach (var chunk in Chunks.Values)
        {
            chunk.GenerateCollisions(GetNode<TileMapLayer>(nameof(LayerNames.Floor)));
            AddChild(chunk);
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