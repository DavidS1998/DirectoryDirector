using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // If args is empty, use test args
            string[] argsToUse = new string[]
            {
                "C:\\Users\\xofas\\Downloads\\TestFolder Thesis\\1", 
                "C:\\Users\\xofas\\Downloads\\TestFolder Thesis\\2"
            };
            if (args.Length > 0) { argsToUse = args; }
            
            
            MainWindow mainWindow = new MainWindow(argsToUse);
            mainWindow.Show();

            
            App app = new App();
            app.InitializeComponent();
            app.Run();
            
            
        }
    }
}