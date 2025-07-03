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

    public string Name { get; } = name;

    // https://gist.github.com/afk-mario/15b5855ccce145516d1b458acfe29a28
    public List<GridFixture> GenerateCollisions(TileMapLayer tileMap)
    {
        List<Vector2[]> polygons = [];
        polygons.AddRange(tileMap.GetUsedCells().Where(cell => Bounds.HasPoint(cell)).Select(GetCellPolygon));

        if (polygons.Count == 0)
        {
            GD.PushError($"No polygon generated for {Name}!");
            return [];
        }

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

        tileMap.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceHide;

        return CreateFixtures();

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

        List<GridFixture> CreateFixtures()
        {
            List<GridFixture> fixtures = [];

            for (var i = 0; i < polygons.Count; i++)
            {
                var f = new GridFixture();
                f.Name = $"{Name}_Fixture{i}";
                f.Polygon = polygons[i];
                fixtures.Add(f);
            }

            return Fixtures = fixtures;
        }
    }
}
