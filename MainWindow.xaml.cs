using System;
using System.Linq;
using System.Windows;
using CefSharp;

namespace JdLoginTool.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Browser.TitleChanged += Browser_TitleChanged;
        }

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string str = "";
            this.Browser.Dispatcher.Invoke(new Action(() =>
            {
                ICookieManager cm = Browser.WebBrowser.GetCookieManager();
                var visitor = new TaskCookieVisitor();
                cm.VisitAllCookies(visitor);
                str = visitor.Task.Result.Where(cookie => cookie.Name == "pt_key" || cookie.Name == "pt_pin").Aggregate(str, (current, cookie) => current + $"{cookie.Name}={cookie.Value};");
                if (str.Contains("pt_key") && str.Contains("pt_pin"))
                {
                    Clipboard.SetText(str);
                    MessageBox.Show(str, "Cookie已复制到剪切板");
                    Application.Current.Shutdown();
                }
            }));
        }
    }
}
