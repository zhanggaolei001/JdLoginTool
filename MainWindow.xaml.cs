using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Windows;
using CefSharp;
using Newtonsoft.Json;
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
                Browser.Address = "m.jd.com";
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
                var cks = visitor.Task.Result; 
                ck = cks.Where(cookie => cookie.Name == "pt_key" || cookie.Name == "pt_pin").Aggregate(ck, (current, cookie) => current + $"{cookie.Name}={System.Web.HttpUtility.UrlEncode(cookie.Value)};");
                if (ck.Contains("pt_key") && ck.Contains("pt_pin"))
                {
                    Clipboard.SetText(ck);
                    UploadToServer(ck);
                    UploadToQingLong(ck);
                    cm.DeleteCookies(".jd.com", "pt_key");
                    cm.DeleteCookies(".jd.com", "pt_pin");
                    Browser.Address = "m.jd.com";
                }
            }));
        }

        private string qlToken = "";
        private void UploadToQingLong(string ck)
        {
            var qlUrl = ConfigurationManager.AppSettings["qlUrl"];
            if (string.IsNullOrWhiteSpace(qlUrl))
            {
                return;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(qlToken))
                {
                    GetQingLongToken();
                }
                if (string.IsNullOrWhiteSpace(qlToken))
                {
                    MessageBox.Show("登陆青龙失败:获取Token失败");
                    return;
                }
                //todo:检测是新ck还是老ck,即是否是更新.
                //暂不实现,是否登陆重复先自己搞吧.


                var client = new RestClient($"{qlUrl}/open/envs");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {qlToken}");
                request.AddHeader("Content-Type", "application/json");
                var body = $"[{{\"name\":\"JD_COOKIE\",\"value\":\"{ck}\"}}]";
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);
                MessageBox.Show(response.Content, "上传青龙成功(Cookie已复制到剪切板)");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "上传青龙失败,Cookie已复制到剪切板,请自行添加处理");
            }
        }



        private void GetQingLongToken()
        {
            var qlUrl = ConfigurationManager.AppSettings["qlUrl"];
            var qlClientID = ConfigurationManager.AppSettings["qlClientID"];
            var qlClientSecret = ConfigurationManager.AppSettings["qlClientSecret"];
            var client = new RestClient($"{qlUrl}/open/auth/token?client_id={qlClientID}&client_secret={qlClientSecret}");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = JsonConvert.DeserializeObject<QLTokenResult>(response.Content);
                qlToken = result.data.token;
                //todo:保存 qlToken,下次运行先拿,并判断是否过期,过期删除,重新获取,后面再实现,目前应用场景影响不大.
            }
        }

        private static void UploadToServer(string ck)
        {
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
                    MessageBox.Show(ck, "Cookie已上传服务器");
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }
    }

    public class QLTokenResult
    {
        public int code { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string token { get; set; }
        public string token_type { get; set; }
        public int expiration { get; set; }
    }
    public class JDCookie
    {
        public String ptPin { get; set; }
        public String ptKey { get; set; }

        public static JDCookie parse(String ck)
        {
            JDCookie jdCookie = new JDCookie();
            String[] split = ck.Split(";");
            foreach (var s in split)
            {
                if (s.StartsWith("pt_key"))
                {
                    jdCookie.ptKey = (s.Split("=")[1]);
                }
                if (s.StartsWith("pt_pin"))
                {
                    jdCookie.ptPin = (s.Split("=")[1]);
                }
            }

            return jdCookie;
        }


        public override String ToString()
        {
            return "pt_key=" + ptKey + ";pt_pin=" + ptPin + ";";
        }
    }

}
