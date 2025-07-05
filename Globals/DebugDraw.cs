using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ShipTest.Globals;

public partial class DebugDraw : Node2D
{
    private readonly List<IDebugDrawing> _drawBuffer = [];

    private static Font _defaultFont;

    public static DebugDraw Instance { get; private set; }

    public DebugLayerFlags LayerState = DebugLayerFlags.Disabled;

    public void Add(IDebugDrawing drawing)
    {
        _drawBuffer.Add(drawing);

        QueueRedraw();
    }

    public override void _Ready()
    {
        ZIndex = 100;
        _defaultFont = ThemeDB.FallbackFont;
        Instance = this;
    }

    public override void _Draw()
    {
        foreach (var drawing in _drawBuffer.Where(d => (LayerState & d.Layer) != 0))
        {
            switch (drawing)
            {
                case DebugLine line:
                    DrawLine(
                        line.Start,
                        line.End,
                        line.Color,
                        line.Width);
                    break;
                case DebugPoint point:
                    DrawCircle(
                        point.Position,
                        point.Radius < 0
                            ? 1.0f
                            : point.Radius,
                        point.Color);
                    break;
                case DebugRect rect:
                    DrawRect(
                        rect.Rect,
                        rect.Color,
                        rect.Filled,
                        rect.Width);
                    break;
                case DebugText text:
                    DrawString(
                        _defaultFont,
                        text.Position,
                        text.Text,
                        modulate: text.Color,
                        fontSize: text.FontSize);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(drawing), 
                        "Bro, you can't just make up your own shape like that.");
            }
        }

        _drawBuffer.Clear();
    }
}

[Flags]
public enum DebugLayerFlags
{
    Disabled = 0x0,
    General = 0x1,
    Physics = 0x2,
    Automata = 0x4,
    Grids = 0x8
}

public interface IDebugDrawing
{
    public DebugLayerFlags Layer { get; }
}

public struct DebugPoint(
    Vector2 position,
    Color color,
    DebugLayerFlags layer = DebugLayerFlags.General,
    float radius = -1.0f) : IDebugDrawing
{
    public Vector2 Position = position;
    public Color Color = color;
    public DebugLayerFlags Layer { get; } = layer;
    public float Radius = radius;
}

public struct DebugLine(
    Vector2 start,
    Vector2 end,
    Color color,
    DebugLayerFlags layer = DebugLayerFlags.General,
    float width = -1.0f) : IDebugDrawing
{
    public Vector2 Start = start;
    public Vector2 End = end;
    public Color Color = color;
    public DebugLayerFlags Layer { get; } = layer;
    public float Width = width;
}

public struct DebugRect(
    Rect2 rect,
    Color color,
    DebugLayerFlags layer = DebugLayerFlags.General,
    bool filled = false,
    float width = -1.0f) : IDebugDrawing
{
    public Rect2 Rect = rect;
    public Color Color = color;
    public DebugLayerFlags Layer { get; } = layer;
    public bool Filled = filled;
    public float Width = width;
}

public struct DebugText(
    Vector2 position,
    Color color,
    string text,
    DebugLayerFlags layer = DebugLayerFlags.General,
    int fontSize = 8) : IDebugDrawing
{
    public Vector2 Position = position;
    public Color Color = color;
    public string Text = text;
    public DebugLayerFlags Layer { get; } = layer;
    public int FontSize = fontSize;
}
