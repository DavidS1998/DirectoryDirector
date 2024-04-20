using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
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
            _settingsHandler = new SettingsHandler();
            UpdateSelectedFolders(folderList);
            
            // Hide default title bar.
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            // Restore AppWindow's size and position
            AppWindow.MoveAndResize(_settingsHandler.SizeAndPosition);
            
            // Restore settings
            if (_settingsHandler.CloseOnApply)
            {
                CloseApplyButton.Icon = new SymbolIcon(Symbol.Accept);
            }
            else
            {
                CloseApplyButton.Icon = new SymbolIcon(Symbol.Cancel);
            }

            if (_settingsHandler.QueueFolders)
            {
                QueueButton.Icon = new SymbolIcon(Symbol.Accept);
                SubfoldersButton.IsEnabled = false;
            }
            else
            {
                QueueButton.Icon = new SymbolIcon(Symbol.Cancel);
                SubfoldersButton.IsEnabled = true;
            }
            
            if (MainGrid.DataContext is not IcoData icoData) return;
            icoData.UpdateFavorites(_settingsHandler.FavoriteFolders);
        }

        private void UpdateSelectedFolders(string[] folderList)
        {
            _folderList = folderList;

            if (folderList.Length == 0)
            {
                AppTitleTextBlock.Text = "Directory Director";
                return;  
            } 

            // Format the title to display the base path and all selected folders
            string basePath = Path.GetDirectoryName(folderList[0]);
            //string allNames = "";
            //foreach (string folder in folderList)
            //{ allNames += Path.GetFileName(folder) + ", "; }

            AppTitleTextBlock.Inlines.Clear();
            AppTitleTextBlock.Inlines.Add(new Run { Text = "Directory Director" });
            AppTitleTextBlock.Inlines.Add(new Run { Text = " - " + basePath + ": ", Foreground = new SolidColorBrush(Colors.Gray)});
            
            if (_settingsHandler.QueueFolders)
            {
                // Display the name of the first folder in the queue as green
                AppTitleTextBlock.Inlines.Add(new Run { Text = "Next in queue: " + Path.GetFileName(folderList[0]), Foreground = new SolidColorBrush(Colors.LimeGreen) });
                for (int i = 1; i < folderList.Length; i++)
                {
                    AppTitleTextBlock.Inlines.Add(new Run { Text = ", " + Path.GetFileName(folderList[i]), Foreground = new SolidColorBrush(Colors.White) });
                }
            }
            else
            {
                // Standard display
                string allNames = "";
                foreach (string folder in folderList)
                {
                    allNames += Path.GetFileName(folder) + ", ";
                }
                AppTitleTextBlock.Inlines.Add(new Run { Text = allNames.TrimEnd(',', ' '), Foreground = new SolidColorBrush(Colors.White) });
            }
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
                    /*
                    MessageDialog messageDialog = new MessageDialog("Error: " + e.Message);
                    messageDialog.ShowAsync(); // Needs await? */
                    continue;
                }
                UpdateDesktopIni(folderPath, Path.GetFileName(randomName));

                // Queue mode
                // Remove the folder from the list
                if (_settingsHandler.QueueFolders)
                {
                    _folderList = _folderList.Where(folder => folder != folderPath).ToArray();
                    UpdateSelectedFolders(_folderList);
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

        private void RevertIcoFile()
        {
            // Repeat for all selected folders
            foreach (string folderPath in _folderList)
            {
                CleanIcoFiles(folderPath);
                UpdateDesktopIni(folderPath, "");
                
                if (_settingsHandler.CloseOnApply)
                {
                    CloseApp();
                }
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
        private void CloseApp()
        {
            Application.Current.Exit();
        }
        
        // Decides what to do when a folder button is clicked
        private void GridClickHandler(string clickedTile)
        {
            if (_folderList.Length == 0) return;
            
            switch (clickedTile)
            {
                case "Select…":
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
        
        private void FolderButton_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // Find which icon was clicked
            StackPanel stackPanel = (StackPanel)sender;
            TextBlock textBlock = (TextBlock)stackPanel.Children[1];
            string clickedTile = textBlock.Text;
            if (MainGrid.DataContext is not IcoData icoData) return;
            if (clickedTile == "Select…" || clickedTile == "Revert") return;
            
            // Add to favorites if it doesn't exist, remove if it does
            if (_settingsHandler.FavoriteFolders.Contains(clickedTile))
            {
                Debug.WriteLine("Removing " + clickedTile);
                _settingsHandler.FavoriteFolders = _settingsHandler.FavoriteFolders.Where(folder => folder != clickedTile).ToList();
                icoData.UpdateFavorites(_settingsHandler.FavoriteFolders);
            }
            else
            {
                Debug.WriteLine("Adding " + clickedTile);
                _settingsHandler.FavoriteFolders = _settingsHandler.FavoriteFolders.Append(clickedTile).ToList();
                icoData.UpdateFavorites(_settingsHandler.FavoriteFolders);
            }
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
        
        private void QueueButton_OnClickButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_settingsHandler.QueueFolders)
            {
                // Disable
                QueueButton.Icon = new SymbolIcon(Symbol.Cancel);
                SubfoldersButton.IsEnabled = true;
                _settingsHandler.QueueFolders = false;
            }
            else
            {
                // Enable
                QueueButton.Icon = new SymbolIcon(Symbol.Accept);
                SubfoldersButton.IsEnabled = false;
                _settingsHandler.QueueFolders = true;
            }
            UpdateSelectedFolders(_folderList);
        }

        // UI to display when dragging over window
        private void MainGrid_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
        }

        // Drag and drop to change context
        private async void MainGrid_OnDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            var items = await e.DataView.GetStorageItemsAsync();
            string[] folderList = items.Select(item => item.Path)
                .Where(Directory.Exists)
                .ToArray();
            if (folderList.Length == 0) return;

            UpdateSelectedFolders(folderList);
        }

        // Open the CachedIcons folder in the File Explorer
        private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            string cachedIconsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                                  ?? throw new InvalidOperationException(), "CachedIcons");
            Process.Start("explorer.exe", cachedIconsPath);
        }
    }
}