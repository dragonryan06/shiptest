using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Globals;

namespace ShipTest.Grids;

public partial class GridChunk(Rect2I bounds, Vector2I tileSize) : CollisionPolygon2D
{
    public Rect2I Bounds { get; } = bounds;

    public Rect2I PixelBounds { get; } = new(bounds.Position * tileSize, bounds.Size * tileSize);

    // https://gist.github.com/afk-mario/15b5855ccce145516d1b458acfe29a28
    public void GenerateCollisions(TileMapLayer tileMap)
    {
        List<Vector2[]> polygons = [];
        polygons.AddRange(tileMap.GetUsedCells().Where(cell => Bounds.HasPoint(cell)).Select(GetCellPolygon));

        if (polygons.Count == 0)
        {
            GD.PushError($"No polygon generated for grid chunk {Name}!");
            return;
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

        if (polygons.Count > 1)
        {
            GD.PushWarning($"Grid chunk {Name} generated multiple polygons! Just taking the first for now. (implement fixture system pls)");
        }

        Polygon = polygons[0];

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
    }
}
