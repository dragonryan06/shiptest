using System;
using System.Collections.Generic;
using Godot;
using ShipTest.Core;
using ShipTest.Explosive;
using ShipTest.Globals;

namespace ShipTest.Grids;

[Entity]
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

        SetCenterOfMass();

        GenerateCollisions();

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

                if (HasNode("ExplosionComponent")) // TODO: maybe an Entity.HasComp(Component) would be cool? or maybe something that injects the component if it isn't present.
                {
                    GetNode<ExplosionComponent>("ExplosionComponent").StartExplosion(
                        GetNode<TileMapLayer>(nameof(LayerNames.Floor)).LocalToMap(GetLocalMousePosition()),
                        100);
                }
                else
                {
                    AddChild(new ExplosionComponent(
                        GetNode<TileMapLayer>(nameof(LayerNames.Floor)).LocalToMap(GetLocalMousePosition()),
                            100));
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

    private void SetCenterOfMass()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));
        CenterOfMassMode = CenterOfMassModeEnum.Custom;

        var usedRect = tileMap.GetUsedRect();
        CenterOfMass = new Rect2(
            tileMap.MapToLocal(usedRect.Position) - new Vector2(0.75f, 0.75f) * tileMap.TileSet.TileSize, 
            tileMap.MapToLocal(usedRect.Size)).GetCenter();
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