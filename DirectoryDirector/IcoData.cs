using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DirectoryDirector;

public class IcoData
{
    public ObservableCollection<string[]> IcoDataList { get; set; }
    public ObservableCollection<string[]> FavoriteList { get; set; }
    
    // TODO: Handle case where CachedIcons folder does not exist

    public IcoData()
    {
        IcoDataList = new ObservableCollection<string[]>();
        FavoriteList = new ObservableCollection<string[]>();
        
        CreateIcoList();
    }
    
    public void AddCustomIco(string icoPath)
    {
        // Add custom icon to the list
        IcoDataList.Add(new[] { icoPath, Path.GetFileName(icoPath) });
    }

    private void CreateIcoList()
    {
        IcoDataList.Clear();
        
        // Find all .ico files in the CachedIcons folder, extract paths
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "CachedIcons");
        string[] icoPaths = Directory.GetFiles(basePath, "*.ico");

        // Create a list of icon data, each containing the path and name
        foreach (string icoPath in icoPaths)
        { IcoDataList.Add(new[] { icoPath, Path.GetFileName(icoPath) }); }
        
        // Remove any entries also in the favorites list / Okay default behavior?
        foreach (string[] favorite in FavoriteList)
        {
            if (IcoDataList.Any(x => x[0] == favorite[0]))
            {
                IcoDataList.Remove(IcoDataList.First(x => x[0] == favorite[0]));
            }
        }
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
        
        // Refresh main list to remove duplicates
        CreateIcoList();
    }
}