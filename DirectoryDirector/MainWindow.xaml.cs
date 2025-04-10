using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Path = System.IO.Path;    

namespace DirectoryDirector;

public partial class MainWindow : Window
{
    private MainViewModel _mainViewModel;

    // Initiated from context menu
    public MainWindow(string[] folderList)
    {
        // Initialization
        InitializeComponent();
        
        var icoData = new IcoData();
        MainGrid.DataContext = icoData;

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
    private void GridClickHandler(string clickedTile)
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
                string path = clickedTile;
                path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                    ?? throw new InvalidOperationException(), "CachedIcons", path);
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
        TextBlock textBlock = (TextBlock)stackPanel.Children[1];
        string clickedTile = textBlock.Text;
        
        GridClickHandler(clickedTile);
    }
    
    // Handle favoriting icons on right click
    private void FolderButton_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Find which icon was clicked
        StackPanel stackPanel = (StackPanel)sender;
        TextBlock textBlock = (TextBlock)stackPanel.Children[1];
        string clickedTile = textBlock.Text;
        if (MainGrid.DataContext is not IcoData icoData) return;
        if (clickedTile == "Select…" || clickedTile == "Revert") return;
        
        // Add to favorites if it doesn't exist, remove if it does
        if (_mainViewModel.SettingsHandler.FavoriteFolders.Contains(clickedTile))
        {
            Debug.WriteLine("Removing " + clickedTile);
            _mainViewModel.SettingsHandler.FavoriteFolders = _mainViewModel.SettingsHandler.FavoriteFolders.Where(folder => folder != clickedTile).ToList();
            icoData.UpdateFavorites(_mainViewModel.SettingsHandler.FavoriteFolders);
        }
        else
        {
            Debug.WriteLine("Adding " + clickedTile);
            _mainViewModel.SettingsHandler.FavoriteFolders = _mainViewModel.SettingsHandler.FavoriteFolders.Append(clickedTile).ToList();
            icoData.UpdateFavorites(_mainViewModel.SettingsHandler.FavoriteFolders);
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
}