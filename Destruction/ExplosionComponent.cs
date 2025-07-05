using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ShipTest.Core.Ecs;
using ShipTest.Globals;

namespace ShipTest.Destruction;

// Handles and draws all explosions occurring on the parent Entity.
public partial class ExplosionComponent : TileMapLayer, IComponent
{
    private static readonly TileSet ExplosionTiles = GD.Load<TileSet>("res://Resources/Tilesets/explosion.tres");
    private static readonly Vector2I RandomTileRange = new(7, 6);

    private readonly Timer _explosionTicker = new()
    {
        WaitTime = 0.025,
        ProcessCallback = Timer.TimerProcessCallback.Physics,
        Autostart = true
    };

    private Dictionary<Vector2I, int> _cellPressures = [];

    public ExplosionComponent(Vector2I explosionCenter, int explosionPressure)
    {
        ZIndex = 5;
        Name = "ExplosionComponent";

        StartExplosion(explosionCenter, explosionPressure);

        _explosionTicker.Timeout += ExplosionTicker_Timeout;
        AddChild(_explosionTicker);
    }

    // IComponent
    public T GetEntity<T>() where T : class
    {
        return GetParentOrNull<T>() ?? throw new EcsException($"Component {nameof(ExplosionComponent)} has no parent Entity!");
    }

    public override void _Ready()
    {
        TileSet = ExplosionTiles;
    }

    public override void _Process(double delta)
    {
        foreach (var cell in _cellPressures.Keys)
        {
            DebugDraw.Instance.Add(new DebugText(
                ToGlobal(MapToLocal(cell)) - new Vector2(8,8),
                new Color("#000000"),
                _cellPressures[cell].ToString(),
                DebugLayerFlags.Automata));
        }
    }

    public void StartExplosion(Vector2I position, int pressure)
    {
        SetCell(position, 0, new Vector2I(0, 0));
        SetOrAddPressure(position, pressure);
    }

    private void SetOrAddPressure(Vector2I cell, int pressure)
    {
        if (_cellPressures.Remove(cell, out var lastPressure))
        {
            _cellPressures.Add(cell, pressure + lastPressure);
        }
        else
        {
            _cellPressures.Add(cell, pressure);
        }
    }

    private void ProcessExplosions()
    {
        Dictionary<Vector2I, int> nextCellPressures = new();

        foreach (var cell in GetUsedCells())
        {
            var lastPressure = _cellPressures[cell];

            if (lastPressure > 5)
            {
                GetEntity<IDestructible>().DestroyCell(cell);
            }

            switch (lastPressure)
            {
                case > 1:
                {
                    var newPressure = SpreadPressure(cell);
                    nextCellPressures.TryAdd(cell, newPressure);
                    break;
                }
                case 1:
                    nextCellPressures.TryAdd(cell, 0);
                    break;
                default:
                    EraseCell(cell);
                    break;
            }
        }

        _cellPressures = nextCellPressures;

        return;

        int SpreadPressure(Vector2I cell)
        {
            var surroundingCells = GetSurroundingCells(cell);
            surroundingCells.Shuffle();

            var localPressure = _cellPressures[cell];

            foreach (var neighbor in surroundingCells)
            {
                if (localPressure <= 1)
                {
                    break;
                }

                if (_cellPressures.TryGetValue(neighbor, out var pressure))
                {
                    if (localPressure <= pressure)
                    {
                        continue;
                    }

                    IncrementPressure(neighbor, pressure, Math.Max(localPressure / 5, 1));
                }
                else
                {
                    SetCell(neighbor, 0, new Vector2I(0, 0));
                    IncrementPressure(neighbor, 0, Math.Max(localPressure / 5, 1));
                }
                localPressure -= Math.Max(localPressure / 5, 1);
            }

            return localPressure;
        }

        // Very similar to SetOrAddPressure but a little different.
        void IncrementPressure(Vector2I cell, int lastPressure, int by)
        {
            if (nextCellPressures.Remove(cell, out var alreadySet))
            {
                // The previous pressure was already transferred by someone else, we just increment it
                nextCellPressures.Add(cell, alreadySet + by);
            }
            else
            {
                // The previous pressure wasn't transferred over yet.
                nextCellPressures.Add(cell, lastPressure + by);
            }
        }
    }

    private void ExplosionTicker_Timeout()
    {
        ProcessExplosions();

        GetUsedCells().ToList().ForEach(cell => SetCell(cell, PressureToSourceId(cell), new Vector2I(
            (int)Math.Abs(GD.Randi() % RandomTileRange.X), 
            (int)Math.Abs(GD.Randi() % RandomTileRange.Y))));

        return;

        int PressureToSourceId(Vector2I cell)
        {
            return _cellPressures[cell] switch
            {
                <= 1 => 0,
                <= 5 => 1,
                _ => 2
            };
        }
    }
}
