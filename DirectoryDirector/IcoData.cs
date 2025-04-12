using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DirectoryDirector;

public class IcoGroup
{
    public string FolderName { get; set; }
    public ObservableCollection<IconItem> Icons { get; set; }
}

public class IconItem
{
    public string FolderName { get; set; }
    public string IconPath { get; set; }
    public string IconName { get; set; }

    public IconItem(string folderName, string iconPath, string iconName)
    {
        FolderName = folderName;
        IconPath = iconPath;
        IconName = iconName;
    }
}

public class IcoData : INotifyPropertyChanged
{
    public SmartCollection<IcoGroup> IcoDataList { get; set; }
    public SmartCollection<IconItem> FavoriteList { get; set; }
    private List<IcoGroup> _allGroups = new();
    
    // Resize handling
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private double _iconWidth = 150;
    public double IconWidth
    {
        get => _iconWidth;
        set
        {
            _iconWidth = value;
            OnPropertyChanged(nameof(IconWidth));
            OnPropertyChanged(nameof(IconHeight));
        }
    }
    public double IconHeight => IconWidth / 15.0 * 17.0;
    
    // TODO: Handle case where CachedIcons folder does not exist

    public IcoData()
    {
        IcoDataList = new SmartCollection<IcoGroup>();
        FavoriteList = new SmartCollection<IconItem>();
        
        CreateIcoList();
        
        IcoDataList.Clear();
        IcoDataList.AddRange(_allGroups);
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
                Icons = new ObservableCollection<IconItem>()
            };
            // Add it to the start of the list to keep it first
            IcoDataList.Insert(0, group);
        }

        // Add the new icon to the default group
        group.Icons.Add(new IconItem(defaultGroupName, icoPath, Path.GetFileNameWithoutExtension(icoPath)));
    }
    
    public void CreateIcoList()
    {
        var groupedIcons = new List<IcoGroup>();

        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                       ?? throw new InvalidOperationException(), "CachedIcons");
        if (!Directory.Exists(basePath)) return;

        var subFolders = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories).Prepend(basePath);

        foreach (string folder in subFolders)
        {
            var icons = new ObservableCollection<IconItem>();
            string relativeFolderName = Path.GetRelativePath(basePath, folder);
            string folderDisplayName = relativeFolderName == "." ? "Default" : relativeFolderName;

            foreach (string icoPath in Directory.GetFiles(folder, "*.ico"))
            {
                // Skip if already in favorite list
                if (FavoriteList.All(fav => fav.IconPath != icoPath))
                {
                    icons.Add(new IconItem(folderDisplayName, icoPath, Path.GetFileNameWithoutExtension(icoPath)));
                }
            }

            if (icons.Count > 0)
            {
                groupedIcons.Add(new IcoGroup
                {
                    FolderName = Path.GetRelativePath(basePath, folder) == "." ? "Default" : Path.GetRelativePath(basePath, folder),
                    Icons = icons
                });
            }
        }

        // ✅ Set the full list AFTER collecting all icons
        _allGroups = groupedIcons;

        IcoDataList.Clear();
        IcoDataList.AddRange(_allGroups);
    }

    
    public void UpdateFavorites(List<string> cachedIconName)
    {
        FavoriteList.Clear();

        // Default options
        string assetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "Assets");
        FavoriteList.Add(new IconItem("Assets", Path.Combine(assetsPath, "Add.svg"), "Select…"));
        FavoriteList.Add(new IconItem("Assets", Path.Combine(assetsPath, "Revert.svg"), "Revert"));

        string basePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException(), "CachedIcons");

        foreach (string icoPath in cachedIconName)
        {
            string fullPath = Path.Combine(basePath, icoPath);
            if (!File.Exists(fullPath)) continue; // Skip if somehow the file isn't there

            string folderName = Path.GetDirectoryName(icoPath)?.Replace("\\", "/") ?? "";
            string groupName = string.IsNullOrEmpty(folderName) ? "Default" : folderName;
            string displayName = Path.GetFileName(Path.GetFileNameWithoutExtension(icoPath));

            FavoriteList.Add(new IconItem(groupName, fullPath, displayName));
        }

        // Refresh main list to remove duplicates across both lists
        CreateIcoList();
    }
    
    public void FilterIcoList(string query)
    {
        if (_allGroups == null) return;
        
        if (string.IsNullOrWhiteSpace(query))
        {
            // Reset to the full list
            IcoDataList.Clear();
            IcoDataList.AddRange(_allGroups);
            return;
        }

        var filtered = _allGroups
            .Select(g => new IcoGroup
            {
                FolderName = g.FolderName,
                Icons = new ObservableCollection<IconItem>(
                    g.Icons.Where(icon =>
                        IsSubsequence(query, icon.IconName) ||
                        IsSubsequence(query, g.FolderName)
                    ))
            })
            .Where(g => g.Icons.Any())
            .ToList();


        IcoDataList.Clear();
        IcoDataList.AddRange(filtered);
    }
    
    // Order subsequence match for friendler searching
    private bool IsSubsequence(string query, string target)
    {
        int q = 0;
        query = query.ToLower();
        target = target.ToLower();

        foreach (char c in target)
        {
            if (q < query.Length && query[q] == c)
            {
                q++;
            }
        }

        return q == query.Length;
    }

}