using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DirectoryDirector;

public class IcoData
{
    public List<string[]> IcoDataList { get; }

    public IcoData()
    {
        // Find all .ico files in the CachedIcons folder, extract paths
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "CachedIcons");
        // TODO: Handle case where CachedIcons folder does not exist

        // Find all .ico files in the CachedIcons folder and extract paths
        string[] icoPaths = Directory.GetFiles(basePath, "*.ico");

        // Create a list of icon data, each containing the path and name
        IcoDataList = icoPaths.Select(icoPath => new[] { icoPath, Path.GetFileName(icoPath) }).ToList();
    }
}