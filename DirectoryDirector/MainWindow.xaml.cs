using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Vanara.PInvoke;
using Path = System.IO.Path;    

namespace DirectoryDirector
{
    public partial class MainWindow : Window
    {
        private string[] folderList; // List of folders to apply the icon to
        
        // Initiated from context menu
        public MainWindow(string[] folderList)
        {
            this.folderList = folderList;
            Debug.WriteLine("arguments received");
            Debug.WriteLine(folderList);
            InitializeComponent();
            
            //AppTitleTextBlock.Text = string.Join(", ", folderList);
            
            // Hide default title bar.
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }
        
        // Copies icon from path to selected folders
        private void CopyIcoFile(string icoPath)
        {
            // Repeat for all selected folders
            foreach (string folderPath in folderList)
            {
                string pathOfCopy = Path.Combine(folderPath, Path.GetFileName(icoPath));
                
                CleanIcoFiles(folderPath);

                try
                {
                    File.Copy(icoPath, pathOfCopy, false);
                    Debug.WriteLine("Folder button clicked");
                    File.SetAttributes(pathOfCopy, File.GetAttributes(pathOfCopy) | FileAttributes.Hidden);
                }
                catch (Exception e)
                {
                    // Triggers for system folders
                    // Show a message box with the error
                    MessageDialog messageDialog = new MessageDialog("Error: " + e.Message);
                    continue;
                }

                UpdateDesktopIni(folderPath, Path.GetFileName(icoPath));
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

        // Handle the click event for the custom folder buttons
        private void FolderButton_OnMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            // Get the clicked item, a StackPanel containing a TextBlock and an Image
            StackPanel stackPanel = (StackPanel)sender;
            TextBlock textBlock = (TextBlock)stackPanel.Children[1];
            string path = textBlock.Text;
            
            // Append the file name to the path of CachedIcons within the exe directory
            path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                                ?? throw new InvalidOperationException(), "CachedIcons", path);
            
            CopyIcoFile(path);
        }
        
        private void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                stackPanel.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x22, 0x22, 0x22));
            }
        }

        private void StackPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                stackPanel.Background = new SolidColorBrush(Colors.Black);
            }
        }
    }
}