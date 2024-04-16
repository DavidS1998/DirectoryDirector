using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Windows.Graphics;
using Windows.UI.Popups;

namespace DirectoryDirector;

public class SettingsHandler
{
    // Size and position✅
    // Close on apply toggle
    // Favorited icons
    
    
    // Saves window state between sessions
    private RectInt32 _sizeAndPosition;
    public RectInt32 SizeAndPosition
    {
        get { DeserializeSettings(); return _sizeAndPosition; }
        set { _sizeAndPosition = value; SerializeSettings(); }
    }
    
    // Data for serialization
    public class SettingsRoot
    {
        public int SizeY { get; set; }
        public int SizeX { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
    }
    
    // Serialize to JSON
    private void SerializeSettings()
    {
        // Properties
        var dictionary = new Dictionary<string, object>
        {
            { "SizeY", _sizeAndPosition.Height },
            { "SizeX", _sizeAndPosition.Width },
            { "PositionX", _sizeAndPosition.X },
            { "PositionY", _sizeAndPosition.Y }
        };
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(dictionary, options);
        string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "appsettings.json");
        File.WriteAllText(settingsPath, json);
    }
    
    // Deserialize from JSON
    private void DeserializeSettings()
    {
        string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "appsettings.json");
        if (!File.Exists(settingsPath)) return;
        var json = File.ReadAllText(settingsPath);
        var rootObject = JsonSerializer.Deserialize<SettingsRoot>(json);
        if (rootObject == null) return;

        // Properties
        _sizeAndPosition = new RectInt32
        {
            Height = rootObject.SizeY,
            Width = rootObject.SizeX,
            X = rootObject.PositionX,
            Y = rootObject.PositionY
        };
        
    }
}