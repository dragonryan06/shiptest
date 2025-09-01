using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Globals;

namespace ShipTest.Grids;

public class GridChunk(string name, Rect2I bounds, Vector2I tileSize)
{
    public Rect2I Bounds { get; } = bounds;

    public Rect2I PixelBounds { get; } = new(bounds.Position * tileSize, bounds.Size * tileSize);

    public List<GridFixture> Fixtures { get; private set; } = [];

    // When a chunk is dirty, later in the frame each fixture in it will try to pathfind to its neighbors
    // to make sure they're still neighboring.
    public bool IsDirty { get; set; } = true;

    public string Name { get; } = name;

    // https://gist.github.com/afk-mario/15b5855ccce145516d1b458acfe29a28
    public List<GridFixture> GenerateCollisions(TileMapLayer tileMap)
    {
        List<Polygon> polygons = [];
        polygons.AddRange(tileMap.GetUsedCells().Where(cell => Bounds.HasPoint(cell)).Select(GetCellPolygon));

        if (polygons.Count == 0)
        {
            return [];
        }

        List<Polygon> deletingPolygons;

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

        tileMap.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceHide;

        IsDirty = true;

        return CreateFixtures();

        Polygon GetCellPolygon(Vector2I cell)
        {
            var polygon = tileMap.GetCellTileData(cell).GetCollisionPolygonPoints((int)CollisionLayers.Floor, 0);
            var translationResult = new Vector2[polygon.Length];

            for (var i = 0; i < polygon.Length; i++)
            {
                translationResult[i] = polygon[i] + cell * tileMap.TileSet.TileSize;
                translationResult[i] += tileMap.TileSet.TileSize / 2;
            }

            return new Polygon(
                translationResult,
                [cell]);
        }

        void AttemptMergeWithPrecedingPolygons(int idx)
        {
            for (var j = 0; j < idx; j++)
            {
                if (deletingPolygons.Contains(polygons[j]))
                {
                    continue;
                }

                var mergeResult = Geometry2D.MergePolygons(polygons[idx].Points, polygons[j].Points);

                if (mergeResult.Count > 1)
                {
                    continue;
                }
                
                polygons[j].ContainedCells.AddRange(polygons[idx].ContainedCells);
                polygons[j] = new Polygon(mergeResult[0], polygons[j].ContainedCells);

                deletingPolygons.Add(polygons[idx]);
            }
        }

        List<GridFixture> CreateFixtures()
        {
            List<GridFixture> fixtures = [];

            for (var i = 0; i < polygons.Count; i++)
            {
                fixtures.Add(new GridFixture(
                    $"{Name}_Fixture{i}",
                    polygons[i].Points,
                    polygons[i].ContainedCells,
                    tileMap.MapToLocal(polygons[i].ContainedCells[0])));
            }

            // Need to wipe references to the old fixtures in others' Neighbors properties
            Fixtures.ForEach(f => f.Dispose());

            return Fixtures = fixtures;
        }
    }

    private readonly struct Polygon(Vector2[] points, List<Vector2I> containedCells) : IEquatable<Polygon>
    {
        public readonly Vector2[] Points = points;
        public readonly List<Vector2I> ContainedCells = containedCells;

        public bool Equals(Polygon other)
        {
            return ContainedCells.Equals(other.ContainedCells);
        }

        public override bool Equals(object obj)
        {
            return obj is Polygon other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ContainedCells.GetHashCode();
        }
    }
}
