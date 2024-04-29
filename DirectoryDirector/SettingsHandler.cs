using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Windows.Graphics;
using Microsoft.UI.Xaml.Controls;

namespace DirectoryDirector;

public class SettingsHandler
{
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

    private bool _queueFolders;
    public bool QueueFolders
    {
        get { DeserializeSettings(); return _queueFolders; }
        set { _queueFolders = value; SerializeSettings(); }
    }
    
    public bool ApplyToSubfolders { get; set; }

    private List<string> _favoriteFolders;
    public List<string> FavoriteFolders
    {
        get { DeserializeSettings(); return _favoriteFolders; } 
        set { _favoriteFolders = value; SerializeSettings(); }
    }
    
    // Used for error messages
    public Grid MainGrid { get; set; }
    
    public SettingsHandler(Grid grid)
    {
        MainGrid = grid;
    }

    
    // Data for serialization
    public class SettingsRoot
    {
        public int SizeY { get; set; }
        public int SizeX { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public bool CloseOnApply { get; set; }
        public bool QueueFolders { get; set; }
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
            { "QueueFolders", _queueFolders },
            { "FavoriteFolders", _favoriteFolders }
        };
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(dictionary, options);
        try
        {
            string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "appsettings.json");
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception e)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error: appsettings.json could either not be accessed or found. Settings will not be saved between sessions.",
                Content = "Is this application placed in a system directory? \n\n" + e.Message,
                CloseButtonText = "Close"
            };
            errorDialog.XamlRoot = MainGrid.XamlRoot;
            errorDialog.ShowAsync().AsTask();
        }
    }
    
    // Deserialize from JSON
    private void DeserializeSettings()
    {
        try
        {
            string settingsPath =
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(),
                    "appsettings.json");
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
            // Value validity checks
            if (_sizeAndPosition.Height <= 600) { _sizeAndPosition.Height = 600; }
            if (_sizeAndPosition.Width <= 600) { _sizeAndPosition.Width = 1000; }
            if (_sizeAndPosition.X < 0) { _sizeAndPosition.X = 0; }
            if (_sizeAndPosition.Y < 0) { _sizeAndPosition.Y = 0; }

            _closeOnApply = rootObject.CloseOnApply;
            _queueFolders = rootObject.QueueFolders;
            _favoriteFolders = rootObject.FavoriteFolders;
        } 
        catch (Exception e)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error: appsettings.json could either not be accessed or found. Settings will not be saved between sessions.",
                Content = "Is this application placed in a system directory? \n\n" + e.Message,
                CloseButtonText = "Close",
                XamlRoot = MainGrid.XamlRoot
            };
            errorDialog.ShowAsync().AsTask();
        }
    }
}