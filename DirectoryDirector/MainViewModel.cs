using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Vanara.PInvoke;
using WinRT.Interop;
using FileAttributes = System.IO.FileAttributes;

namespace DirectoryDirector;

public class MainViewModel
{
    private const string Version = "v1.2";

    private string[] _folderList; // List of folders to apply the icon to
    private readonly SettingsHandler _settingsHandler;
    public SettingsHandler SettingsHandler => _settingsHandler;
    
    public MainViewModel(string[] folderList, Grid mainGrid, IcoData icoData)
    {
        _folderList = folderList;
        _settingsHandler = new SettingsHandler(mainGrid);
        icoData.UpdateFavorites(SettingsHandler.FavoriteFolders); 
    }
    
    public void UpdateFolderSelection(string[] folderList, TextBlock appTitleText)
    {
        _folderList = folderList;
        UpdateTitleText(appTitleText);
    }
    
    // Changes the list of selected folders, and updates the title text
    public void UpdateTitleText(TextBlock appTitleText)
    {
        // Text if no folders are selected, such as when opening the app from Start
        if (_folderList.Length == 0)
        {
            appTitleText.Inlines.Clear();
            appTitleText.Inlines.Add(new Run { Text = "Directory Director " + Version });
            appTitleText.Inlines.Add(new Run { Text = " - No folders selected", Foreground = new SolidColorBrush(Colors.Gray)});
            appTitleText.Inlines.Add(new Run { Text = " - Drag and drop folders here", Foreground = new SolidColorBrush(Colors.Yellow)});
            return;  
        } 

        // Format the title to display the base path and all selected folders
        string basePath = Path.GetDirectoryName(_folderList[0]);

        appTitleText.Inlines.Clear();
        appTitleText.Inlines.Add(new Run { Text = "Directory Director " + Version });
        appTitleText.Inlines.Add(new Run { Text = " - " + basePath + ": ", Foreground = new SolidColorBrush(Colors.Gray)});
        
        if (_settingsHandler.QueueFolders)
        {
            // Display the name of the first folder in the queue as green
            appTitleText.Inlines.Add(new Run { Text = "Next in queue: " + Path.GetFileName(_folderList[0]), Foreground = new SolidColorBrush(Colors.LimeGreen) });
            for (int i = 1; i < _folderList.Length; i++)
            {
                appTitleText.Inlines.Add(new Run { Text = ", " + Path.GetFileName(_folderList[i]), Foreground = new SolidColorBrush(Colors.White) });
            }
        }
        else
        {
            // Standard display
            string allNames = "";
            foreach (string folder in _folderList)
            {
                allNames += Path.GetFileName(folder) + ", ";
            }
            appTitleText.Inlines.Add(new Run { Text = allNames.TrimEnd(',', ' '), Foreground = new SolidColorBrush(Colors.White) });
        }
    }
    
    // Copies icon from path to selected folders
    public void CopyIcoFile(string icoPath, TextBlock textBlock)
    {
        if (_folderList.Length == 0) return;
        
        // If set, queue up all subfolders for folder icon change 
        string[] tempFolderList = _folderList;
        if (_settingsHandler.ApplyToSubfolders)
        {
            tempFolderList = tempFolderList.Concat(
                tempFolderList.SelectMany(folderPath => Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
            ).ToArray();
        }
        
        // Repeat for all selected folders
        foreach (string folderPath in tempFolderList)
        {
            CleanIcoFiles(folderPath);
            // Randomize to prevent name conflicts
            string icoName = Path.GetFileName(icoPath);
            string prefixedName = "DDirector - " + icoName;
            string newPathName = Path.Combine(folderPath, prefixedName);

            try
            {
                File.Copy(icoPath, newPathName, false);
                File.SetAttributes(newPathName, File.GetAttributes(newPathName) | FileAttributes.Hidden);
            }
            catch (Exception e)
            {
                // Triggers for system folders
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error: Could not update folder icon.",
                    Content = "The selected folder may be a system folder.\n\n" + e.Message,
                    CloseButtonText = "Close",
                    XamlRoot = textBlock.XamlRoot
                };
                errorDialog.ShowAsync().AsTask();
                continue;
            }
            UpdateDesktopIni(folderPath, Path.GetFileName(prefixedName));

            // Queue mode
            // Remove the folder from the list
            if (_settingsHandler.QueueFolders)
            {
                _folderList = _folderList.Where(folder => folder != folderPath).ToArray();
                UpdateFolderSelection(_folderList, textBlock);
                if (_settingsHandler.CloseOnApply && _folderList.Length == 0)
                {
                    CloseApp();
                }
                return;
            }
        }
        
        // Close the window if CloseOnApply is enabled
        if (_settingsHandler.CloseOnApply)
        {
            CloseApp();
        }
    }

    // Reverts the icon of selected folders back to default
    public void RevertIcoFile(TextBlock textBlock)
    {
        if (_folderList.Length == 0) return;
        
        // If set, queue up all subfolders for folder icon change 
        string[] tempFolderList = _folderList;
        if (_settingsHandler.ApplyToSubfolders)
        {
            tempFolderList = tempFolderList.Concat(
                tempFolderList.SelectMany(folderPath => Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
            ).ToArray();
        }
        
        // Repeat for all selected folders
        foreach (string folderPath in tempFolderList)
        {
            CleanIcoFiles(folderPath);
            UpdateDesktopIni(folderPath, "");
            
            // Queue mode
            // Remove the folder from the list
            if (_settingsHandler.QueueFolders)
            {
                _folderList = _folderList.Where(folder => folder != folderPath).ToArray();
                UpdateFolderSelection(_folderList, textBlock);
                if (_settingsHandler.CloseOnApply && _folderList.Length == 0)
                {
                    CloseApp();
                }
                return;
            }
        }
        
        if (_settingsHandler.CloseOnApply)
        {
            CloseApp();
        }
    }

    // Remove previous .ico files made by this software
    private void CleanIcoFiles(string folderPath)
    {
         string[] icoPaths = Directory.GetFiles(folderPath, "*.ico", SearchOption.TopDirectoryOnly);
        foreach (string icoPath in icoPaths)
        {
            // Delete only files set by this software
            if ((File.GetAttributes(icoPath) & FileAttributes.Hidden) != FileAttributes.Hidden) continue;
            if (!Path.GetFileName(icoPath).StartsWith("DDirector - ")) continue;
            try 
            { File.Delete(icoPath); }
            catch (Exception e) 
            { Debug.WriteLine(e); }
        }
    }

    // Update the desktop.ini file and notify the system
    private void UpdateDesktopIni(string path, string icoName)
    {
        Shell32.SHFOLDERCUSTOMSETTINGS pfcs = new Shell32.SHFOLDERCUSTOMSETTINGS
        {
            dwMask = Shell32.FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
            pszIconFile = icoName,
            dwSize = (uint) Marshal.SizeOf(typeof (Shell32.SHFOLDERCUSTOMSETTINGS)),
            cchIconFile = 0
        };
        Shell32.SHGetSetFolderCustomSettings(ref pfcs, path, Shell32.FCS.FCS_FORCEWRITE);
        Shell32.SHChangeNotify(Shell32.SHCNE.SHCNE_UPDATEDIR, Shell32.SHCNF.SHCNF_PATHW, path);
    }
    
    // File picker for user icons
    public async void OpenFilePicker(TextBlock textBlock, Window window)
    {
        if (_folderList.Length == 0) return;
        
        // Create a new file picker
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            FileTypeFilter = { ".ico", ".png" }
        };

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();

        // No file picked
        if (file == null) return;
        if (textBlock.DataContext is not IcoData icoData) return;

        var tempFlag = false;
        
        // If PNG, convert to ICO
        if (file.FileType == ".png")
        {
            tempFlag = true;
            await PngConverter.Convert(file.Path, file.Path.Replace(".png", ".ico"), 256, false);
            file = await StorageFile.GetFileFromPathAsync(file.Path.Replace(".png", ".ico"));
        }

        // Check if the exact same file exists within CachedIcons already
        var cachedIconsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException(), "CachedIcons"));
        var existingFile = await FindExistingFileAsync(cachedIconsFolder, file.Path);

        if (existingFile != null)
        {
            // Same file exists, use existing file
            CopyIcoFile(existingFile.Path, textBlock);
        }
        else
        {
            // Different file, copy over the new file
            var copiedFile = await file.CopyAsync(cachedIconsFolder, file.Name, NameCollisionOption.GenerateUniqueName);
            icoData.AddCustomIco(copiedFile.Path);
            CopyIcoFile(copiedFile.Path, textBlock);
        }
        
        // Delete the temporary ICO file
        if (tempFlag)
        {
            await file.DeleteAsync();
        }
    }
    
    // Check if an identical file already exists, no matter the name
    private async Task<StorageFile> FindExistingFileAsync(StorageFolder folder, string filePath)
    {
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var existingFiles = await folder.GetFilesAsync();
        foreach (var file in existingFiles)
        {
            var existingFileBytes = await File.ReadAllBytesAsync(file.Path);
            if (fileBytes.SequenceEqual(existingFileBytes))
            {
                return file; // Identical file found
            }
        }
        return null;
    }
    
    // Close application
    private void CloseApp()
    {
        Application.Current.Exit();
    }
}