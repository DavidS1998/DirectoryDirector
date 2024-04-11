using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // Initiated without arguments
        public MainWindow()
        {
            Debug.WriteLine("no arguments received");
            InitializeComponent();
        }
        
        // Initiated from context menu
        public MainWindow(string[] args)
        {
            Debug.WriteLine("arguments received");
            InitializeComponent();
            
            /*
            // Write each argument to the TextBlock Folders, with a newline between each
            string text = "";
            foreach (string arg in args)
            {
                text += arg + "\n";
            }
            Folders.Text = text;
            */
            
            //ReadDesktopIni(args[0]);
        }
        

        void ReadDesktopIni(string path)
        {
            // Check if desktop.ini exists
            string desktopIniPath = Path.Combine(path, "desktop.ini");
            if (!System.IO.File.Exists(desktopIniPath))
            {
                Debug.WriteLine("desktop.ini not found");
                return;
            }
            
            // Read desktop.ini
            string[] lines = System.IO.File.ReadAllLines(desktopIniPath);
            foreach (string line in lines)
            {
                Debug.WriteLine(line);
            }
        }
        
        
    }
}