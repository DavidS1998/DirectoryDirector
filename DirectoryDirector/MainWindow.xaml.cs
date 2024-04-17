using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Vanara.PInvoke;
using WinRT.Interop;
using FileAttributes = System.IO.FileAttributes;
using Path = System.IO.Path;    

namespace DirectoryDirector
{
    public partial class MainWindow : Window
    {
        // TODO: Next: Favorites, delete with context menu? Sorting of standard list 

        private string[] _folderList; // List of folders to apply the icon to
        private SettingsHandler _settingsHandler;
        
        // Initiated from context menu
        public MainWindow(string[] folderList)
        {
            // Initialization
            InitializeComponent();
            UpdateSelectedFolders(folderList);
            _settingsHandler = new SettingsHandler();
            
            // Hide default title bar.
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            // Restore AppWindow's size and position
            AppWindow.MoveAndResize(_settingsHandler.SizeAndPosition);
            
            // Restore settings
            if (_settingsHandler.CloseOnApply)
            { CloseApplyButton.Icon = new SymbolIcon(Symbol.Accept); } 
            else
            { CloseApplyButton.Icon = new SymbolIcon(Symbol.Cancel); }
        }

        private void UpdateSelectedFolders(string[] folderList)
        {
            _folderList = folderList;

            // Format the title to display the base path and all selected folders
            string basePath = Path.GetDirectoryName(folderList[0]);
            string allNames = "";
            foreach (string folder in folderList)
            { allNames += Path.GetFileName(folder) + ", "; }
            AppTitleTextBlock.Text = "Directory Director - " + basePath + "\\ (" + allNames.TrimEnd(',', ' ') + ")";
        }
        
        // Copies icon from path to selected folders
        private void CopyIcoFile(string icoPath)
        {
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
                string randomName = Path.GetRandomFileName().Replace(".", "") + ".ico";
                string randomNewName = Path.Combine(folderPath, randomName);
                //string pathOfCopy = Path.Combine(folderPath, Path.GetFileName(icoPath));

                try
                {
                    File.Copy(icoPath, randomNewName, false);
                    File.SetAttributes(randomNewName, File.GetAttributes(randomNewName) | FileAttributes.Hidden);
                }
                catch (Exception e)
                {
                    // Triggers for system folders
                    // Show a message box with the error
                    // TODO: Actually show the message box
                    Debug.WriteLine(e);
                    MessageDialog messageDialog = new MessageDialog("Error: " + e.Message);
                    messageDialog.ShowAsync(); // Needs await?
                    continue;
                }
                UpdateDesktopIni(folderPath, Path.GetFileName(randomName));
            }
            
            // Close the window if CloseOnApply is enabled
            if (_settingsHandler.CloseOnApply)
            {
                Close();
            }
        }

        private void RevertIcoFile()
        {
            // Repeat for all selected folders
            foreach (string folderPath in _folderList)
            {
                CleanIcoFiles(folderPath);
                UpdateDesktopIni(folderPath, "");
            }
        }

        // Remove leftover .ico files
        private void CleanIcoFiles(string folderPath)
        {
            string[] icoPaths = Directory.GetFiles(folderPath, "*.ico", SearchOption.TopDirectoryOnly);
            foreach (string icoPath in icoPaths)
            {
                if ((File.GetAttributes(icoPath) & FileAttributes.Hidden) != FileAttributes.Hidden) continue;
                try 
                { File.Delete(icoPath); }
                catch (Exception e) 
                { Debug.WriteLine(e); }
            }
        }

        // Update the desktop.ini file and notify the system
        private void UpdateDesktopIni(string path, string icoName)
        {
            Shell32.SHFOLDERCUSTOMSETTINGS pfcs = new Shell32.SHFOLDERCUSTOMSETTINGS()
            {
                dwMask = Shell32.FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
                pszIconFile = icoName,
                dwSize = (uint) Marshal.SizeOf(typeof (Shell32.SHFOLDERCUSTOMSETTINGS)),
                cchIconFile = 0
            };
            Shell32.SHGetSetFolderCustomSettings(ref pfcs, path, Shell32.FCS.FCS_FORCEWRITE);
            Shell32.SHChangeNotify(Shell32.SHCNE.SHCNE_UPDATEDIR, Shell32.SHCNF.SHCNF_PATHW, path);
        }
        
        // File picker for user added icons
        private async void OpenFilePicker()
        {
            // Create a new file picker
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                FileTypeFilter = { ".ico" }
            };

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();

            // No file picked
            if (file == null) return;
            if (MainGrid.DataContext is not IcoData icoData) return;

            // Check if the exact same file exists within CachedIcons already
            var cachedIconsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException(), "CachedIcons"));
            var existingFile = await FindExistingFileAsync(cachedIconsFolder, file.Path);

            if (existingFile != null)
            {
                // Same file exists, use existing file
                CopyIcoFile(existingFile.Path);
            }
            else
            {
                // Different file, copy over the new file
                var copiedFile = await file.CopyAsync(cachedIconsFolder, file.Name, NameCollisionOption.GenerateUniqueName);
                icoData.AddCustomIco(copiedFile.Path);
                CopyIcoFile(copiedFile.Path);
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
        private void Close()
        {
            //MainWindow_OnClosed();
            Application.Current.Exit();
        }
        
        // Decides what to do when a folder button is clicked
        private void GridClickHandler(string clickedTile)
        {
            switch (clickedTile)
            {
                case "Add…":
                    OpenFilePicker();
                    break;
                case "Revert":
                    RevertIcoFile();
                    break;
                default:
                {
                    string path = clickedTile;
                    path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                        ?? throw new InvalidOperationException(), "CachedIcons", path);
                    CopyIcoFile(path);
                    break;
                }
            }
        }

        // Handle the click event for the custom folder buttons
        private void FolderButton_OnMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            // Find which icon was clicked
            StackPanel stackPanel = (StackPanel)sender;
            TextBlock textBlock = (TextBlock)stackPanel.Children[1];
            string clickedTile = textBlock.Text;
            
            GridClickHandler(clickedTile);
        }
        
        // Hover enter animation
        private void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                stackPanel.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x22, 0x22, 0x22));
            }
        }

        // Hover exit animation
        private void StackPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                stackPanel.Background = new SolidColorBrush(Colors.Black);
            }
        }
        
        // Saves settings on exit
        private void MainWindow_OnClosed(object sender, WindowEventArgs args)
        {
            // Save the size and position to the app's settings
            var size = AppWindow.Size;
            var position = AppWindow.Position;
            RectInt32 rect = new RectInt32((int)position.X, (int)position.Y, (int)size.Width, (int)size.Height);
            _settingsHandler.SizeAndPosition = rect;
        }
        
        // AppBar button to toggle close on apply
        private void CloseApplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_settingsHandler.CloseOnApply)
            {
                // Disable
                CloseApplyButton.Icon = new SymbolIcon(Symbol.Cancel);
                _settingsHandler.CloseOnApply = false;
            }
            else
            {
                // Enable
                CloseApplyButton.Icon = new SymbolIcon(Symbol.Accept);
                _settingsHandler.CloseOnApply = true;
            }
        }

        private void SubfoldersButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_settingsHandler.ApplyToSubfolders)
            {
                // Disable
                SubfoldersButton.Icon = new SymbolIcon(Symbol.Cancel);
                _settingsHandler.ApplyToSubfolders = false;
            }
            else
            {
                // Enable
                SubfoldersButton.Icon = new SymbolIcon(Symbol.Accept);
                _settingsHandler.ApplyToSubfolders = true;
            }
        }
    }
}