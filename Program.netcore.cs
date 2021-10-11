using CefSharp;
using CefSharp.Wpf;
using System;
using System.IO;

namespace JdLoginTool.Wpf
{
    public static class Program
    {
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            //For Windows 7 and above, app.manifest entries will take precedences of this call
            Cef.EnableHighDPISupport();


            //We are using our current exe as the BrowserSubProcess
            //Multiple instances will be spawned to handle all the 
            //Chromium proceses, render, gpu, network, plugin, etc.
            var subProcessExe = new CefSharp.BrowserSubprocess.BrowserSubprocessExecutable();
            var result = subProcessExe.Main(args);
            if (result > 0)
            {
                return result;
            }

            //We use our current exe as the BrowserSubProcess
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
                BrowserSubprocessPath = exePath,
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1"

            };

            //Example of setting a command line argument
            //Enables WebRTC
            settings.CefCommandLineArgs.Add("enable-media-stream");

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            var app = new App();
            app.InitializeComponent(); 
            return app.Run();
        } 
    }
}