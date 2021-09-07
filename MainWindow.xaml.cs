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
            this.Loaded += (o, e) =>
            {
                Browser.Address = "https://plogin.m.jd.com/login/login?appid=300&returnurl=https://wq.jd.com/passport/LoginRedirect";
            };
        }

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string ck = "";
            this.Browser.Dispatcher.Invoke(new Action(() =>
            {
                ICookieManager cm = Browser.WebBrowser.GetCookieManager();
                var visitor = new TaskCookieVisitor();
                cm.VisitAllCookies(visitor);
                ck = visitor.Task.Result.Where(cookie => cookie.Name == "pt_key" || cookie.Name == "pt_pin").Aggregate(ck, (current, cookie) => current + $"{cookie.Name}={System.Web.HttpUtility.UrlEncode(cookie.Value)};");
            
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
                            var response = client.Execute(request);
                            Console.WriteLine(response.Content);
                            MessageBox.Show(ck, "Cookie已上传服务器,且已复制到剪切板");
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        } 
                    }
                    else
                    { 
                        MessageBox.Show(ck, "Cookie已复制到剪切板");
                    }
                    Application.Current.Shutdown();
                }
            }));
        }
    }
}
