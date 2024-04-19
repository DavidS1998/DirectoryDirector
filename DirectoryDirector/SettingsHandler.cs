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

    private bool _closeOnApply;
    public bool CloseOnApply { 
        get { DeserializeSettings(); return _closeOnApply; } 
        set { _closeOnApply = value; SerializeSettings(); }
    }
    public bool ApplyToSubfolders { get; set; }

    private List<string> _favoriteFolders;
    public List<string> FavoriteFolders
    {
        get { DeserializeSettings(); return _favoriteFolders; } 
        set { _favoriteFolders = value; SerializeSettings(); }
    }

    
    // Data for serialization
    public class SettingsRoot
    {
        public int SizeY { get; set; }
        public int SizeX { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public bool CloseOnApply { get; set; }
        public List<string> FavoriteFolders { get; set; } = new();
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
            { "PositionY", _sizeAndPosition.Y },
            { "CloseOnApply", _closeOnApply },
            { "FavoriteFolders", _favoriteFolders }
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
        _closeOnApply = rootObject.CloseOnApply;
        _favoriteFolders = rootObject.FavoriteFolders;
    }
}