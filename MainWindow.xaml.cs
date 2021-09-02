using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using CefSharp;
using RestSharp;

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
            string ck = "";
            this.Browser.Dispatcher.Invoke(new Action(() =>
            {
                ICookieManager cm = Browser.WebBrowser.GetCookieManager();
                var visitor = new TaskCookieVisitor();
                cm.VisitAllCookies(visitor);
                ck = visitor.Task.Result.Where(cookie => cookie.Name == "pt_key" || cookie.Name == "pt_pin").Aggregate(ck, (current, cookie) => current + $"{cookie.Name}={cookie.Value};");
                if (ck.Contains("pt_key") && ck.Contains("pt_pin"))
                {
                    Clipboard.SetText(ck);
                    var upload = ConfigurationManager.AppSettings["upload"] == "true";
                    var ckServer = ConfigurationManager.AppSettings["server"]; 
                    if (upload && !string.IsNullOrWhiteSpace(ckServer))
                    { 
                        var method = ConfigurationManager.AppSettings["method"];
                        try
                        {
                            var client = new RestClient(ckServer + ck)
                            {
                                Timeout = -1
                            };
                            var request = new RestRequest(method == "post" ? Method.POST : Method.GET);
                            client.Execute(request);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }

                    }
                    MessageBox.Show(ck, "Cookie已复制到剪切板");
                    Application.Current.Shutdown();
                }
            }));
        }
    }
}
