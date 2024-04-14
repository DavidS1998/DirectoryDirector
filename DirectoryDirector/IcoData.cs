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
    public ObservableCollection<string[]> IcoDataList { get; }
    public ObservableCollection<string[]> FavoriteList { get; }

    public IcoData()
    {
        // Find all .ico files in the CachedIcons folder, extract paths
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "CachedIcons");
        // TODO: Handle case where CachedIcons folder does not exist

        // Find all .ico files in the CachedIcons folder and extract paths
        string[] icoPaths = Directory.GetFiles(basePath, "*.ico");

        // Create a list of icon data, each containing the path and name
        IcoDataList = new ObservableCollection<string[]>();
        foreach (string icoPath in icoPaths)
        { IcoDataList.Add(new[] { icoPath, Path.GetFileName(icoPath) }); }
        
        // Add Assets/Add.svg to start of list
        FavoriteList = new ObservableCollection<string[]>();
        string assetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "Assets");
        FavoriteList.Insert(0, new[] { Path.Combine(assetsPath, "Add.svg"), "Add…" });
    }
    
    public void AddCustomIco(string icoPath)
    {
        // Add custom icon to the list
        IcoDataList.Add(new[] { icoPath, Path.GetFileName(icoPath) });
        PrintIcoData();
    }
    
    public void AddFavorite(string[] icoData)
    {
        FavoriteList.Add(icoData);
    }
    
    public void PrintIcoData()
    {
        foreach (string[] icoData in IcoDataList)
        {
            Debug.WriteLine(icoData[0] + " " + icoData[1]);
        }
    }
}