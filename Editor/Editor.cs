using Godot;
using Godot.Collections;

namespace ShipTest.Editor;

public partial class Editor : Node2D
{
    private readonly Array _parts;

    public Editor()
    {
        _parts = InitializeParts();

        return;
        
        Array InitializeParts()
        {
            var jsonString = FileAccess.Open("res://Resources/Scenes/Editor/parts.json", 
                FileAccess.ModeFlags.Read).GetAsText();
            return Json.ParseString(jsonString).As<Array>();
        }
    }
}