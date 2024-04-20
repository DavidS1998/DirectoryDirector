using System;
using System.Diagnostics;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;

namespace DirectoryDirector
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
        
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // The first argument is always the executable path
            var arguments = Environment.GetCommandLineArgs()[1..];
            
            // If args is empty, use test args
            string[] argsToUse = 
            {
                //"C:\\Users\\xofas\\Downloads\\TestFolder Thesis\\1"
                //"C:\\Users\\xofas\\Downloads\\TestFolder Thesis\\Nesting"
            };
            if (arguments.Length > 0) { argsToUse = arguments; }
            
            m_window = new MainWindow(argsToUse);
            m_window.Activate();
        }

        private Window m_window;
    }
}