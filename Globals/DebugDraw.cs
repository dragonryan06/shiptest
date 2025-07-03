using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ShipTest.Globals;

public partial class DebugDraw : Node2D
{
    private readonly List<DebugPoint> _points = [];
    private readonly List<DebugLine> _lines = [];
    private readonly List<DebugText> _text = [];

    private static Font _defaultFont;

    public static DebugDraw Instance { get; private set; }

    public DebugLayerFlags LayerState = DebugLayerFlags.Disabled;

    public void Add(IDebugDrawing drawing)
    {
        switch (drawing)
        {
            case DebugPoint point:
                _points.Add(point);
                break;
            case DebugLine line:
                _lines.Add(line);
                break;
            case DebugText text:
                _text.Add(text);
                break;
            default:
                throw new ArgumentException("Bro, you can't just make up your own shape like that.");
        }

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
        foreach (var point in _points.Where(point => (LayerState & point.Layer) != 0))
        {
            DrawCircle(
                point.Position,
                point.Radius < 0
                    ? 1.0f
                    : point.Radius,
                point.Color);
        }

        foreach (var line in _lines.Where(line => (LayerState & line.Layer) != 0))
        {
            DrawLine(
                line.Start,
                line.End,
                line.Color,
                line.Width);
        }

        foreach (var text in _text.Where(text => (LayerState & text.Layer) != 0))
        {
            DrawString(
                _defaultFont,
                text.Position,
                text.Text,
                modulate: text.Color,
                fontSize: text.FontSize);
        }

        _points.Clear();
        _lines.Clear();
        _text.Clear();
    }
}

[Flags]
public enum DebugLayerFlags
{
    Disabled = 0x0,
    General = 0x1,
    Physics = 0x2
}

public interface IDebugDrawing
{
}

public struct DebugPoint(
    Vector2 position,
    Color color,
    DebugLayerFlags layer = DebugLayerFlags.General,
    float radius = -1.0f) : IDebugDrawing
{
    public Vector2 Position = position;
    public Color Color = color;
    public DebugLayerFlags Layer = layer;
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
    public DebugLayerFlags Layer = layer;
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
    public DebugLayerFlags Layer = layer;
    public int FontSize = fontSize;
}
