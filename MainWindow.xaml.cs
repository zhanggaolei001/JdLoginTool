using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using HtmlAgilityPack;
using JdLoginTool.Wpf.Model;
using Newtonsoft.Json;
using RestSharp;

namespace JdLoginTool.Wpf
{
    public class MenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
        }
        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return false;
        }
        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
        }
        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
       public static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "cache.json");
        public List<UserInfo> UserList { get; set; } = new List<UserInfo>();
        public MainWindow()
        {
            /* "13250812637": "*3565",
               "17682487263": "*1515",
               "15236225781": "*2028",
               "15103793217": "*3545",
               "18317520679": "*0522"
               */

            if (!File.Exists(path))
            {
                File.Create(path);
            }
            var j = File.ReadAllText(path);
            if (!string.IsNullOrWhiteSpace(j))
            {
                var tmp = JsonConvert.DeserializeObject<List<UserInfo>>(j);
                if (tmp.Any())
                {
                    UserList = tmp;
                }
            }
            InitializeComponent();
            Browser.TitleChanged += Browser_TitleChanged;
            Browser.KeyUp += TryGetUserInputPhone;
            this.Loaded += (o, e) =>
            {
                Browser.Address = "m.jd.com";
            };
            Browser.MenuHandler = new MenuHandler();
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;

            this.Closing += (o, e) =>
            {
                var json = JsonConvert.SerializeObject(UserList, Formatting.Indented);
                Console.WriteLine(json);
                File.WriteAllText( path, json);
            };
        }



        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            JumpToLoginPage();
        }

        private async void TryGetUserInputPhone(object sender, KeyEventArgs e)
        {
            string script = "function get_phone(){\r\n" +
                            "return document.querySelector('#app>div>div:nth-child(3)>p:nth-child(1)>input').value;\r\n" +
                            "}\r\n" +
                            "get_phone();";
            await Browser.EvaluateScriptAsync(script).ContinueWith(new Action<Task<JavascriptResponse>>((respA) =>
            {
                var resp = respA.Result;    //respObj此时有两个属性: name、age
                dynamic respObj = resp.Result;
                PhoneNumber = (string)resp.Result;
            }));
        }
        private async void JumpToLoginPage()
        {
            var title = "";
            Browser.Dispatcher.Invoke(() => { title = Browser.Title; });
            Trace.WriteLine(title);
            if (title == "多快好省，购物上京东！")
            {
                string script = "document.querySelector('#msShortcutLogin').click()";
                await Browser.EvaluateScriptAsync(script).ContinueWith(new Action<Task<JavascriptResponse>>((respA) =>
                {
                    var resp = respA.Result;    //respObj此时有两个属性: name、age
                    dynamic respObj = resp.Result;
                    Trace.WriteLine((string)resp.Result);
                }));
            }

        }

        public String PhoneNumber { get; set; }
        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.CheckBox.IsChecked != true)
            {
                return;
            }
            string ck = "";
            this.Browser.Dispatcher.Invoke(new Action(() =>
           {

               ICookieManager cm = Browser.WebBrowser.GetCookieManager();
               var visitor = new TaskCookieVisitor();
               cm.VisitAllCookies(visitor);
               var cks = visitor.Task.Result;

               foreach (var cookie in cks)
               {
                   Regex reg = new Regex(@"[\u4e00-\u9fa5]");
                   if (reg.IsMatch(cookie.Value))
                   {
                       if (cookie.Name == "pt_key" || cookie.Name == "pt_pin") ck = ck + $"{cookie.Name}={System.Web.HttpUtility.UrlEncode(cookie.Value)};";

                   }
                   else
                   {
                       if (cookie.Name == "pt_key" || cookie.Name == "pt_pin") ck = ck + $"{cookie.Name}={cookie.Value};";
                   }

               }

               if (ck.Contains("pt_key") && ck.Contains("pt_pin"))
               {
                   try
                   {
                       Clipboard.SetText(ck);
                   }
                   catch (Exception exception)
                   {
                       Console.WriteLine(exception);
                       File.AppendAllText("cookies.txt", DateTime.Now.ToString() + ":" + ck);
                       MessageBox.Show(this, "复制到剪切板失败,(远程桌面下会出这个问题或重启电脑可能就好了),已经ck写入cookies.txt中,开始尝试上传.错误信息" + exception.Message);
                   }


                   UploadToServer(ck);
                   UploadToQingLong(ck);
                   GetAndSaveUserInfo(ck);
                   cm.DeleteCookies(".jd.com", "pt_key");
                   cm.DeleteCookies(".jd.com", "pt_pin");
                   Browser.Address = "m.jd.com";
               }

           }));

        }

        private void GetAndSaveUserInfo(string ck)
        {
            var client = new RestClient("https://wq.jd.com/deal/recvaddr/getrecvaddrlistV3?adid=&locationid=undefined&callback=cbLoadAddressListA&reg=1&encryptversion=1&r=0.8832760737226157&sceneval=2&appCode=ms0ca95114");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("authority", "wq.jd.com");
            request.AddHeader("accept", "*/*");
            request.AddHeader("accept-language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.AddHeader("cookie", ck);
            request.AddHeader("origin", "https://wqs.jd.com");
            request.AddHeader("referer", "https://wqs.jd.com/");
            request.AddHeader("sec-fetch-dest", "empty");
            request.AddHeader("sec-fetch-mode", "cors");
            request.AddHeader("sec-fetch-site", "same-site");
            client.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1";
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
            var json = response.Content.Replace("cbLoadAddressListA(", "");
            json = json.Remove(json.Length - 1);
            var result = JsonConvert.DeserializeObject<ResultObject>(json);
            foreach (var address in result.list)
            {
                if (address.default_address == "1")
                {
                    if (UserList.FirstOrDefault(u => u.Phone == phone) is { } user)
                    {
                        user.UsualAddressName = address.name;
                        user.AddressList = result.list;
                    }
                    else
                    {
                        UserList.Add(new UserInfo(phone)
                        {
                            UsualAddressName = address.name,
                            AddressList = result.list
                        });
                    }
                }
            }
        }

        public class ResultObject
        {
            public string errCode { get; set; }
            public string retCode { get; set; }
            public string msg { get; set; }
            public string nextUrl { get; set; }
            public string idc { get; set; }
            public string token { get; set; }
            public string dealRecord { get; set; }
            public string jdaddrid { get; set; }
            public string jdaddrname { get; set; }
            public string siteGray { get; set; }
            public string encryptCode { get; set; }
            public AddressList[] list { get; set; }
        }

        public class AddressList
        {
            public string label { get; set; }
            public string type { get; set; }
            public string rgid { get; set; }
            public string adid { get; set; }
            public string addrdetail { get; set; }
            public string addrfull { get; set; }
            public string name { get; set; }
            public string mobile { get; set; }
            public string phone { get; set; }
            public string postcode { get; set; }
            public string email { get; set; }
            public string idCard { get; set; }
            public string nameCode { get; set; }
            public string provinceId { get; set; }
            public string cityId { get; set; }
            public string countyId { get; set; }
            public string townId { get; set; }
            public string provinceName { get; set; }
            public string cityName { get; set; }
            public string countyName { get; set; }
            public string townName { get; set; }
            public string areacode { get; set; }
            public string need_upgrade { get; set; }
            public string default_address { get; set; }
            public string longitude { get; set; }
            public string latitude { get; set; }
            public string readOnly { get; set; }
        }

        private async void SetPhone(string phone)
        {
            try
            {
                var script = "if (!document.querySelector(`#app > div > p.policy_tip > input`).checked) {\r\n" +
                             "  \r\n" +
                             "  var xresult = document.evaluate(`//*[@id='app']/div/div[3]/p[1]/input`, document, null, XPathResult.ANY_TYPE, null);" +
                             $"  var p=xresult.iterateNext();p.value=`{phone}`;" +
                             "  p.dispatchEvent(new Event('input'));\r\n }";
                var result = await Browser.EvaluateScriptAsPromiseAsync(script);

            }
            catch (Exception e)
            {

            }
            finally
            {
                TextBox.Clear();
            }

        }
        private bool SetCaptcha(string captcha)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync(
                    $"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{captcha}\";p.dispatchEvent(new Event('input'));");
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                TextBox.Clear();
            }
        }
        private async Task<bool> ClickLoginButton()
        {
            try
            {
                await Browser.EvaluateScriptAsync(" var xresult = document.querySelector(\"#app > div > p.policy_tip > input\").click();");
                Thread.Sleep(500);
                var result = await Browser.EvaluateScriptAsync(" var xresult = document.evaluate(`//*[@id=\"app\"]/div/a[1]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.click();");

                return result.Success;
            }
            catch (Exception e)
            {

                return false;
            }
        }

        private string qlToken = "";
        private void UploadToQingLong(string ck)
        {
            var qlUrl = ConfigurationManager.AppSettings["qlUrl"];
            if (string.IsNullOrWhiteSpace(qlUrl)) return;
            try
            {
                if (string.IsNullOrWhiteSpace(qlToken))
                {
                    GetQingLongToken();
                }
                if (string.IsNullOrWhiteSpace(qlToken))
                {
                    MessageBox.Show(this, "登陆青龙失败:获取Token失败");
                    return;
                }
                var jck = JDCookie.parse(ck);
                var input = new InputWindow();
                string remarks = PhoneNumber ?? jck.ptPin;//正常应该就是手机号了,如果开发的有问题,拿错手机号会用ptPin
                input.Remarkers = remarks;
                if (input.ShowDialog() == true)
                {
                    remarks = input.Remarkers;
                }
                var client = new RestClient($"{qlUrl}/open/envs") { Timeout = -1 };
                var request = new RestRequest();
                request.AddHeader("Authorization", $"Bearer {qlToken}");
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("t", DateTimeOffset.Now.ToUnixTimeMilliseconds());
                var body = $"[{{\"name\":\"JD_COOKIE\",\"value\":\"{ck}\",\"remarks\":\"{remarks}\"}}]";
                if (CheckIsNewUser(qlUrl, ck, out var id))
                {
                    request.Method = Method.POST;
                }
                else
                {
                    body = $"{{\"name\":\"JD_COOKIE\",\"value\":\"{ck}\",\"remarks\":\"{remarks}\",\"id\":{id}}}";
                    request.Method = Method.PUT;
                }

                request.AddParameter("application/json", body, ParameterType.RequestBody);
                var response = client.Execute(request);
                Console.WriteLine(response.Content);
                MessageBox.Show(this, response.Content, "上传青龙成功(Cookie已复制到剪切板)");
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "上传青龙失败,Cookie已复制到剪切板,请自行添加处理");
            }
        }
        private bool CheckIsNewUser(string qlUrl, string ck, out int id)
        {
            var newCk = JDCookie.parse(ck);
            var client = new RestClient($"{qlUrl}/open/envs");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", $"Bearer {qlToken}");
            request.AddHeader("Content-Type", "application/json");
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<GetCookiesResult>(response.Content);
            if (result == null)
            {
                id = 0;
                return true;
            }
            if (result.code != 200)
            {
                throw new Exception($"请求返回失败,代码:{result.code}");
            }
            if (result.data.Any(jck => JDCookie.parse(jck.value).ptPin == newCk.ptPin))
            {
                var firstOrDefault = result.data.FirstOrDefault(jck => newCk.ptPin != null && JDCookie.parse(jck.value).ptPin == newCk.ptPin);
                if (firstOrDefault != null)
                {
                    id = firstOrDefault.id;
                }
                else
                {
                    id = 0;
                    return true;
                }

                return false;
            }
            id = 0;
            return true;
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

        private void UploadToServer(string ck)
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
                    MessageBox.Show(this, ck, "Cookie已上传服务器");
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, e.Message);
                }
            }
        }

        private void Browser_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Browser_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private static string phone = "";
        private void ButtonSetPhone_OnClick(object sender, RoutedEventArgs e)
        {

            phone = Clipboard.GetText();
            if (!IsPhoneNumber(phone))
            {
                phone = TextBox.Text;
            }
            if (IsPhoneNumber(phone))
            {
                SetPhone(phone);

                ClickGetCaptchaButton();
            }
            else
            {
                SetPhone("");
            }

        }

        //string GetId2_4(string phone)
        //{
        //    //todo:读取本地映射列表
        //};
        public static bool IsPhoneNumber(string phoneNumber)
        {
            return Regex.IsMatch(phoneNumber, @"^1(3[0-9]|5[0-9]|7[6-8]|8[0-9])[0-9]{8}$");
        }
        public static bool IsCaptcha(string captcha)
        {
            return Regex.IsMatch(captcha, @"^\d{6}$");
        }
        private bool ClickGetCaptchaButton()
        {
            try
            {
                var result = Browser.EvaluateScriptAsync("document.querySelector('#app div button').click()");

                return result.Result.Success;
            }
            catch (Exception e)
            {

                return false;
            }
        }
        private async void ButtonSetCaptcha_OnClick(object sender, RoutedEventArgs e)
        {
            var captcha = Clipboard.GetText();
            if (!IsCaptcha(captcha))
            {
                captcha = TextBox.Text;
            }
            if (IsCaptcha(captcha))
            {
                SetCaptcha(captcha);
                await ClickLoginButton();
            }
        }

        private void ButtonLogin_OnClick(object sender, RoutedEventArgs e)
        {
            JumpToLoginPage();
        }

        private void ButtonHandleId_OnClick(object sender, RoutedEventArgs e)
        {
            //todo 为了测试,先应该能够进行html打印
            //var html = Browser.GetSourceAsync().Result;
            // Trace.WriteLine(html);
            if (UserList.FirstOrDefault(u => u.Phone == phone) is UserInfo user)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.Id2_4))
                    {
                        return;
                    }
                    TextBox.Text = user.Id2_4;
                    //todo
                    for (var i = 0; i < TextBox.Text.Length; i++)
                    {
                        var js = $"document.querySelector(\"#app > div > div.wrap > div.input-box > div > div:nth-child({1 + i})\").innerText = {TextBox.Text[i]}";
                        Browser.EvaluateScriptAsPromiseAsync(js);
                    }
                }
                catch (Exception exception)
                { 
                    MessageBox.Show(exception.Message);
                }


                //document.querySelector("#app > div > div.wrap > div.input-box > div > div:nth-child(1)").innerText=1
                //document.querySelector("#app > div > div.wrap > div.input-box > div > div:nth-child(1)").innerText=1
                //document.querySelector("#app > div > div.wrap > div.input-box > div > div:nth-child(1)").innerText=1
                //document.querySelector("#app > div > div.wrap > div.input-box > div > div:nth-child(1)").innerText=1
                //todo:sendkey
            }
            //todo:获取本次登陆手机号的缓存(或textbox输入内容解析)的身份证信息,然后自动点击验证身份证,自动输入,自动执行
        }
        private bool SetUserId2_4(string id2_4)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync(
                    $"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{id2_4}\";p.dispatchEvent(new Event('input'));");
                if (UserList.FirstOrDefault(u => u.Phone == phone) is UserInfo user)
                {
                    user.Id2_4 = id2_4;
                }
                else
                {
                    UserList.Add(new UserInfo(phone) { Id2_4 = id2_4 });
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                TextBox.Clear();
            }
        }
        private bool SetAddressName(string addressName)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync(
                    $"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{addressName}\";p.dispatchEvent(new Event('input'));");
                if (UserList.FirstOrDefault(u => u.Phone == phone) is UserInfo user)
                {
                    user.UsualAddressName = addressName;
                }
                else
                {
                    UserList.Add(new UserInfo(phone) { UsualAddressName = addressName });
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                TextBox.Clear();
            }
        }

        private void ButtonBrowSource_OnClick(object sender, RoutedEventArgs e)
        {
            Browser.ViewSource();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonSetCK_OnClick(object sender, RoutedEventArgs e)
        {
            //todo:判定ck合法性,设置到浏览器
            var ck = Clipboard.GetText();
            if (!IsCk(ck))
            {
                ck = TextBox.Text;
            }
            if (IsCk(ck))
            {
                SetCk(ck);

                // GoToCart();
            }
            else
            {
                SetPhone("");
            }
        }

        private void SetCk(string ck)
        {//todo:浏览器设置ck,参考退会那个Python项目
            /*        # 写入Cookie
        self.browser.delete_all_cookies()
        for cookie in self.config['cookie'].split(";", 1):
            self.browser.add_cookie(
                {"name": cookie.split("=")[0].strip(" "), "value": cookie.split("=")[1].strip(";"), "domain": ".jd.com"}
            )
        self.browser.refresh()*/
            var cm = Cef.GetGlobalCookieManager();
            var cookie = JDCookie.parse(ck);
            cm.SetCookieAsync("https://m.jd.com/", new CefSharp.Cookie
            {
                Domain = ".jd.com",
                Name = "pt_pin",
                Value = cookie.ptPin,
            });
            cm.SetCookieAsync("https://m.jd.com/", new CefSharp.Cookie
            {
                Domain = ".jd.com",
                Name = "pt_key",
                Value = cookie.ptKey,
            });

            Browser.ReloadCommand.Execute(null);
        }

        private bool IsCk(string ck)
        {
            return ck.Contains("pt_key") && ck.Contains("pt_pin") && ck.Length > 20;
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
            try
            {

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
            catch (Exception e)
            {
                Console.WriteLine(e);
                jdCookie = new JDCookie();
            }
            return jdCookie;
        }


        public override String ToString()
        {
            return "pt_key=" + ptKey + ";pt_pin=" + ptPin + ";";
        }
    }

    public class GetCookiesResult
    {
        public int code { get; set; }
        public Datum[] data { get; set; }
    }






    public class Datum
    {
        public string value { get; set; }
        public int id { get; set; }
        public long created { get; set; }
        public int status { get; set; }
        public string timestamp { get; set; }
        public float position { get; set; }
        public string name { get; set; }
        public string remarks { get; set; }
    }

}
