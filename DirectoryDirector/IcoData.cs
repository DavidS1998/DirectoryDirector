using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DirectoryDirector;

public class IcoGroup
{
    public string FolderName { get; set; }
    public ObservableCollection<string[]> Icons { get; set; }
}

public class IcoData
{
    public SmartCollection<IcoGroup> IcoDataList { get; set; }
    public SmartCollection<string[]> FavoriteList { get; set; }
    
    // TODO: Handle case where CachedIcons folder does not exist

    public IcoData()
    {
        IcoDataList = new SmartCollection<IcoGroup>();
        FavoriteList = new SmartCollection<string[]>();
        
        CreateIcoList();
    }
    
    public void AddCustomIco(string icoPath)
    {
        // Name for the default group
        const string defaultGroupName = "Default"; 

        // Find the default group or create one if it doesn't exist
        var group = IcoDataList.FirstOrDefault(g => g.FolderName == defaultGroupName);
        if (group == null)
        {
            group = new IcoGroup
            {
                FolderName = defaultGroupName,
                Icons = new ObservableCollection<string[]>()
            };
            // Add it to the start of the list to keep it first
            IcoDataList.Insert(0, group);
        }

        // Add the new icon to the default group
        group.Icons.Add(new[] { icoPath, Path.GetFileName(icoPath) });
    }
    
    private void CreateIcoList()
    {
        var groupedIcons = new SmartCollection<IcoGroup>();
    
        // Find all .ico files in the CachedIcons folder, extract paths
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "CachedIcons");
        if (!Directory.Exists(basePath)) return;
        var subFolders = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories).Prepend(basePath);

        // Create a list of icon data, each containing the path, name, and associated folder
        foreach (string folder in subFolders)
        {
            var icons = new ObservableCollection<string[]>();

            foreach (string icoPath in Directory.GetFiles(folder, "*.ico"))
            {
                // Only add non-favorited icons to this list
                if (FavoriteList.All(favorite => favorite[0] != icoPath))
                {
                    icons.Add(new[] { icoPath, Path.GetFileName(icoPath) });
                }
            }

            // Add groups of icons corresponding to subfolders
            if (icons.Count > 0)
            {
                groupedIcons.Add(new IcoGroup
                {
                    // Top-level group name is "Default"
                    FolderName = Path.GetRelativePath(basePath, folder) == "." ? "Default" : Path.GetRelativePath(basePath, folder),
                    Icons = icons
                });
            }
        }
        
        // Done to prevent a flood of NotifyCollectionChanged events
        IcoDataList.Clear();
        IcoDataList.AddRange(groupedIcons);
    }
    
    public void UpdateFavorites(List<string> cachedIconName)
    {
        FavoriteList.Clear();
        
        // Default options
        string assetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "Assets");
        FavoriteList.Add(new[] { Path.Combine(assetsPath, "Add.svg"), "Select…" });
        FavoriteList.Add(new[] { Path.Combine(assetsPath, "Revert.svg"), "Revert" });
        
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "CachedIcons");
        foreach (string icoPath in cachedIconName)
        {
            FavoriteList.Add(new[] { basePath + "\\" + icoPath, Path.GetFileName(icoPath) });
        }
        
        // Refresh main list to remove duplicates across both lists
        CreateIcoList();
    }
}