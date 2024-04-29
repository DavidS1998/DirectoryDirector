using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DirectoryDirector;

public class IcoData
{
    public SmartCollection<string[]> IcoDataList { get; set; }
    public SmartCollection<string[]> FavoriteList { get; set; }
    
    // TODO: Handle case where CachedIcons folder does not exist

    public IcoData()
    {
        IcoDataList = new SmartCollection<string[]>();
        FavoriteList = new SmartCollection<string[]>();
        
        CreateIcoList();
    }
    
    public void AddCustomIco(string icoPath)
    {
        // Add custom icon to the list
        IcoDataList.Add(new[] { icoPath, Path.GetFileName(icoPath) });
    }

    private void CreateIcoList()
    {
        var tempCollection = new Collection<string[]>();
        
        // Find all .ico files in the CachedIcons folder, extract paths
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "CachedIcons");
        string[] icoPaths = Directory.GetFiles(basePath, "*.ico");

        // Create a list of icon data, each containing the path and name
        foreach (string icoPath in icoPaths)
        {
            string fileName = Path.GetFileName(icoPath);
            // Only add non-favorited icons to this list
            if (FavoriteList.All(favorite => favorite[0] != icoPath))
            {
                tempCollection.Add(new[] { icoPath, fileName });
            }
        }
        // Done to prevent a flood of NotifyCollectionChanged events
        IcoDataList.Clear();
        IcoDataList.AddRange(tempCollection);
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