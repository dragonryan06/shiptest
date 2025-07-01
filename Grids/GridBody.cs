using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Globals;

namespace ShipTest.Grids;

public partial class GridBody : RigidBody2D
{
    private bool _mouseHover = false;
    private bool _mouseDrag = false;

    public override void _Ready()
    {
        if (!HasNode(nameof(LayerNames.Floor)))
        {
            throw new InvalidOperationException("A GridBody instance must have at least a floor layer when readying!");
        }

        // We do this so the RigidBody center is in the center of the tilemap.
        RecenterTilemap();

        GenerateCollisions();

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left )
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

    private void RecenterTilemap()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));

        tileMap.Position -= tileMap.GetUsedRect().End / new Vector2(2, 2);
    }

    // https://gist.github.com/afk-mario/15b5855ccce145516d1b458acfe29a28
    private void GenerateCollisions()
    {
        var tileMap = GetNode<TileMapLayer>(nameof(LayerNames.Floor));

        List<Vector2[]> polygons = [];
        polygons.AddRange(tileMap.GetUsedCells().Select(GetCellPolygon));

        List<Vector2[]> deletingPolygons;

        do
        {
            deletingPolygons = [];

            for (var i = 0; i < polygons.Count; i++)
            {
                if (deletingPolygons.Contains(polygons[i]))
                {
                    continue;
                }

                AttemptMergeWithPrecedingPolygons(i);
            }

            polygons.RemoveAll(p => deletingPolygons.Contains(p));

        } while (deletingPolygons.Count != 0);

        GenerateResultingShapes();

        tileMap.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceHide;

        return;

        Vector2[] GetCellPolygon(Vector2I cell)
        {
            var polygon = tileMap.GetCellTileData(cell).GetCollisionPolygonPoints((int)CollisionLayers.Floor, 0);
            var translationResult = new Vector2[polygon.Length];

            for (var i = 0; i < polygon.Length; i++)
            {
                translationResult[i] = polygon[i] + cell * tileMap.TileSet.TileSize;
                translationResult[i] += tileMap.TileSet.TileSize / 2;
            }

            return translationResult;
        }

        void AttemptMergeWithPrecedingPolygons(int idx)
        {
            for (var j = 0; j < idx; j++)
            {
                if (deletingPolygons.Contains(polygons[j]))
                {
                    continue;
                }

                var mergeResult = Geometry2D.MergePolygons(polygons[idx], polygons[j]);

                if (mergeResult.Count > 1)
                {
                    continue;
                }

                polygons[j] = mergeResult[0];

                deletingPolygons.Add(polygons[idx]);
            }
        }

        void GenerateResultingShapes()
        {
            foreach (var polygon in polygons)
            {
                var shape = new CollisionPolygon2D();
                shape.Polygon = polygon;
                shape.Name = "GeneratedPolygon";
                AddChild(shape);
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