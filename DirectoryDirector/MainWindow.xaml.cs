using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;
using Path = System.IO.Path;    

namespace DirectoryDirector;

public partial class MainWindow : Window
{
    private MainViewModel _mainViewModel;
    private IcoData _icoData;

    // Initiated from context menu
    public MainWindow(string[] folderList)
    {
        // Initialization
        InitializeComponent();
        
        var icoData = new IcoData();
        _icoData = icoData;
        MainGrid.DataContext = _icoData;

        _mainViewModel = new MainViewModel(folderList, MainGrid, icoData);
        _mainViewModel.UpdateFolderSelection(folderList, AppTitleTextBlock);

        // Set app icon
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(myWndId);
        appWindow.SetIcon(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                      ?? throw new InvalidOperationException(), "Assets", "Small.ico"));
        
        // Hide default title bar.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        
        // Restore AppWindow's size and position
        AppWindow.MoveAndResize(_mainViewModel.SettingsHandler.SizeAndPosition);
        
        // Restore settings
        CloseApplyButton.Icon = _mainViewModel.SettingsHandler.CloseOnApply 
            ? new SymbolIcon(Symbol.Accept) : new SymbolIcon(Symbol.Cancel);

        if (_mainViewModel.SettingsHandler.QueueFolders)
        {
            QueueButton.Icon = new SymbolIcon(Symbol.Accept);
            SubfoldersButton.IsEnabled = false;
        }
        else
        {
            QueueButton.Icon = new SymbolIcon(Symbol.Cancel);
            SubfoldersButton.IsEnabled = true;
        }
        
        // Check for updates
        DownloadButton.Visibility = _mainViewModel.IsLatestVersion() ? Visibility.Collapsed : Visibility.Visible;
    }
    
    // Decides what to do when a folder button is clicked
    private void GridClickHandler(string clickedTile, string folderName = "")
    {
        switch (clickedTile)
        {
            case "Select…":
                _mainViewModel.OpenFilePicker(AppTitleTextBlock, this);
                break;
            case "Revert":
                _mainViewModel.RevertIcoFile(AppTitleTextBlock);
                break;
            default:
            {
                // If folderName is empty, it indicates it's from the default folder (root)
                string path;
                if (string.IsNullOrEmpty(folderName))
                {
                    path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                        ?? throw new InvalidOperationException(), "CachedIcons", clickedTile);
                }
                else
                {
                    // Construct the path including the folder name/subfolder
                    path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                        ?? throw new InvalidOperationException(), "CachedIcons", folderName, clickedTile);
                }
            
                _mainViewModel.CopyIcoFile(path, AppTitleTextBlock);
                break;
            }
        }
    }
    
    
    
    // Handle the click event for the custom folder buttons
    private void FolderButton_OnMouseLeftButtonDown(object sender, RoutedEventArgs e)
    {
        // Find which icon was clicked
        StackPanel stackPanel = (StackPanel)sender;
        IconItem clickedIcon = (IconItem)stackPanel.DataContext;
        string clickedTile = clickedIcon.IconName;
        string folderName = clickedIcon.FolderName == "Default" ? "." : clickedIcon.FolderName;
        
        GridClickHandler(clickedTile, folderName);
    }

    
    // Handle favoriting icons on right click
    private void FolderButton_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Find which icon was clicked
        if (MainGrid.DataContext is not IcoData icoData) return;

        StackPanel stackPanel = (StackPanel)sender;
        Image image = stackPanel.Children[0] as Image;
        TextBlock textBlock = stackPanel.Children[1] as TextBlock;

        string clickedTile = textBlock?.Text ?? "";
        string? fullIconPath = (image?.Source as BitmapImage)?.UriSource?.LocalPath;

        if (string.IsNullOrEmpty(fullIconPath) || clickedTile is "Select…" or "Revert")
            return;

        // Get relative path from CachedIcons
        string basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "CachedIcons");
        string relativePath = Path.GetRelativePath(basePath, fullIconPath);

        // Toggle favorite
        var favorites = _mainViewModel.SettingsHandler.FavoriteFolders;
        if (favorites.Contains(relativePath))
        {
            Debug.WriteLine("Removing " + relativePath);
            _mainViewModel.SettingsHandler.FavoriteFolders = favorites.Where(f => f != relativePath).ToList();
        }
        else
        {
            Debug.WriteLine("Adding " + relativePath);
            _mainViewModel.SettingsHandler.FavoriteFolders = favorites.Append(relativePath).ToList();
        }

        icoData.UpdateFavorites(_mainViewModel.SettingsHandler.FavoriteFolders);
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
        RectInt32 rect = new RectInt32(position.X, position.Y, size.Width, size.Height);
        _mainViewModel.SettingsHandler.SizeAndPosition = rect;
    }
    
    // AppBar button to toggle close on apply
    private void CloseApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_mainViewModel.SettingsHandler.CloseOnApply)
        {
            // Disable
            CloseApplyButton.Icon = new SymbolIcon(Symbol.Cancel);
            _mainViewModel.SettingsHandler.CloseOnApply = false;
        }
        else
        {
            // Enable
            CloseApplyButton.Icon = new SymbolIcon(Symbol.Accept);
            _mainViewModel.SettingsHandler.CloseOnApply = true;
        }
    }

    // AppBar button to toggle subfolder setting
    private void SubfoldersButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_mainViewModel.SettingsHandler.ApplyToSubfolders)
        {
            // Disable
            SubfoldersButton.Icon = new SymbolIcon(Symbol.Cancel);
            _mainViewModel.SettingsHandler.ApplyToSubfolders = false;
        }
        else
        {
            // Enable
            SubfoldersButton.Icon = new SymbolIcon(Symbol.Accept);
            _mainViewModel.SettingsHandler.ApplyToSubfolders = true;
        }
    }
    
    // AppBar button to toggle queue mode setting
    private void QueueButton_OnClickButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_mainViewModel.SettingsHandler.QueueFolders)
        {
            // Disable
            QueueButton.Icon = new SymbolIcon(Symbol.Cancel);
            SubfoldersButton.IsEnabled = true;
            _mainViewModel.SettingsHandler.QueueFolders = false;
        }
        else
        {
            // Enable
            QueueButton.Icon = new SymbolIcon(Symbol.Accept);
            SubfoldersButton.IsEnabled = false;
            _mainViewModel.SettingsHandler.QueueFolders = true;
        }
        _mainViewModel.UpdateTitleText(AppTitleTextBlock);
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

        _mainViewModel.UpdateFolderSelection(folderList, AppTitleTextBlock);
    }

    // Open the CachedIcons folder in the File Explorer
    private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        string cachedIconsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                              ?? throw new InvalidOperationException(), "CachedIcons");
        Process.Start("explorer.exe", cachedIconsPath);
    }    
    
    // Open the CachedIcons folder in the File Explorer
    private void ReleasesLinkButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Open www.google.com in the default browser
        Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/DavidS1998/DirectoryDirector/releases"));
    }

    // Resize icons
    private void SizeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (MainGrid.DataContext is not IcoData data) return;
        data.IconWidth = e.NewValue;
    }

    // Filter icons
    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_icoData == null) return;

        string query = SearchBox.Text.Trim().ToLowerInvariant();
        _icoData.FilterIcoList(query);
    }

    // Any typed characters will automatically go into the search box
    private void MainGrid_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Ignore key events if already focused or not a character key
        if (SearchBox.FocusState == FocusState.Keyboard)
            return;

        // Filter: Only redirect letter, digit, and symbol keys
        var key = e.Key;

        // Ignore navigation/control keys
        if (key == VirtualKey.Tab || key == VirtualKey.Space ||
            key == VirtualKey.Left || key == VirtualKey.Right ||
            key == VirtualKey.Up || key == VirtualKey.Down ||
            key == VirtualKey.Shift || key == VirtualKey.Control ||
            key == VirtualKey.Escape || key == VirtualKey.Enter)
            return;
        
        // If it's a valid text entry key, set focus to the search box
        SearchBox.Focus(FocusState.Keyboard);
    }
}