using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CefSharp;
using JdLoginTool.Wpf.Model;
using JdLoginTool.Wpf.Model.Qinglong;
using JdLoginTool.Wpf.Service;
using JdLoginTool.Wpf.Utility;
using Newtonsoft.Json;
using RestSharp;
using Cookie = CefSharp.Cookie;

namespace JdLoginTool.Wpf
{
    public class JsObj
    {
        public static MainWindow Main { get; set; }

        public void SetJsBox(string s)
        {
            Main.SetJsBox(s);
        }
        public bool Test()
        {
            return true;
        }
    }
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Visibility _IsSimpleMode = Visibility.Collapsed;

        public Visibility IsSimpleMode
        {
            get { return _IsSimpleMode; }
            set
            {
                _IsSimpleMode = value;
                RaisePropertyChanged();
            }
        }

        public static string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "cache.json");

        public static string CookiePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "config.sh");

        public ObservableCollection<User> UserList
        {
            get => _userList;
            set
            {
                _userList = value;
                RaisePropertyChanged();
            }
        }

        public void SetJsBox(string s)
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    this.JsBox.Text = s;
                })
                ;
        }
        public MainWindow()
        {
            InitializeComponent();
            DateTimePicker.Value = DateTime.Now;
            this.DataContext = this;
            string j = "[]";
            if (!File.Exists(CachePath))
            {
                File.Create(CachePath, 1024 * 10, FileOptions.RandomAccess);
            }
            else
            {
                j = File.ReadAllText(CachePath);
            }

            if (File.Exists("ua.txt"))
            {
                this._defaultUa = File.ReadAllText("ua.txt");
            }
            if (File.Exists("url.txt"))
            {
                UrlBox.Text = File.ReadAllText("url.txt");
            }
            if (File.Exists("js.txt"))
            {
                JsBox.Text = File.ReadAllText("js.txt");
            }

            if (!string.IsNullOrWhiteSpace(j))
            {
                try
                {
                    var tmp = JsonConvert.DeserializeObject<List<User>>(j);
                    if (tmp.Any())
                    {
                        foreach (var userInfo in tmp)
                        {
                            UserList.Add(userInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
            CefSharpSettings.WcfEnabled = false;
            Browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true; JsObj.Main = this;
            Browser.JavascriptObjectRepository.Register("gui", new JsObj(), true);//这个地方相当于注册了一个BO（浏览器对象，和window对象是平级的）

            Browser.TitleChanged += Browser_TitleChanged;
            Browser.KeyUp += TryGetUserInputPhone;
            this.Loaded += (o, e) => { Browser.Address = "m.jd.com"; };
            Browser.MenuHandler = new MenuHandler();
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;

            this.Closing += (o, e) =>
            {
                var json = JsonConvert.SerializeObject(UserList, Formatting.Indented);
                Console.WriteLine(json);
                File.WriteAllText(CachePath, json);
                this.stop = true;
                this.exit = true;

            };
            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                this.UrlBox.Text =
             "https://coupon.m.jd.com/coupons/show.action?key=c0m2c5s1o3a04f8c8c925cac41c942bb&roleId=88645899&time=1668045928261#183_871058406";

            }


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
                var resp = respA.Result; //respObj此时有两个属性: name、age
                Phone = (string)resp.Result;
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
                    var resp = respA.Result; //respObj此时有两个属性: name、age
                    Trace.WriteLine((string)resp.Result);
                }));
            }

        }



        ICookieManager CookieManager;

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (CookieManager == null)
            {
                CookieManager = Browser.WebBrowser.GetCookieManager();
            }

            if (this.LoginMode.IsChecked != true) return;
            if (!GetBrowserCk()) return;
            ClearBrowserCk();
            JumpToLoginPage();
        }

        private bool GetBrowserCk()
        {
            bool result = false;
            this.Browser.Dispatcher.Invoke(new Action(() =>
            {
                var ckList = GetJingdongCk();
                var ckString = ckList.ToCkString();
                if (ckString.Contains("pt_key") && ckString.Contains("pt_pin"))
                {
                    try
                    {
                        Clipboard.SetText(ckString);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        File.AppendAllText("cookies.log", DateTime.Now.ToString() + ":" + ckString);
                        if (MessageOn.IsChecked != true)
                        {
                            MessageBox.Show(this,
                                "复制到剪切板失败,(远程桌面下会出这个问题或重启电脑可能就好了),已经ck写入cookies.txt中,开始尝试上传.错误信息" + exception.Message);
                        }
                    }

                    UploadToServer(ckString);
                    var ptPin = QingLongJdCookie.Parse(ckString).ptPin;
                    var user = FindOrAddUser(ptPin);
                    user.Cookies = ckList.ToArray();

                    if (!string.IsNullOrWhiteSpace(Phone) && Helper.IsPhoneNumber(Phone))
                    {
                        user.Phone = Phone;
                    }

                    QingLongService.UploadToQingLong(ckString, Phone, this.MessageOn.IsChecked);

                    result = true;
                }
                else
                {
                    result = false;
                }
            }));
            return result;
        }

        private void ClearBrowserCk()
        {
            CookieManager.DeleteCookies(".jd.com", "pt_key");
            CookieManager.DeleteCookies(".jd.com", "pt_pin");
            Browser.Address = "m.jd.com";
            Phone = "";
        }

        private List<Cookie> GetJingdongCk()
        {
            var visitor = new TaskCookieVisitor();
            CookieManager.VisitAllCookies(visitor);
            var cks = visitor.Task.Result;
            return cks.Where(cookie => cookie.Name == "pt_key" || cookie.Name == "pt_pin" || cookie.Name == "exp")
                .ToList();
        }




        private User FindOrAddUser(string ptPin)
        {
            var now = DateTime.Now;
            //todo:检测中英文问题.
            var user = UserList.FirstOrDefault(u => u.Pin == ptPin
                                                    || System.Web.HttpUtility.UrlEncode(u.Pin) == ptPin
                                                    || u.Pin == System.Web.HttpUtility.UrlEncode(ptPin));
            if (user == null)
            {
                user = new User(Phone, now.AddDays(29));
                UserList.Add(user);
            }

            return user;
        }

        private async void WritePhone(string phone)
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
                LogLabel.Content = e.Message;
            }
            finally
            {
                TextBox.Clear();
            }

        }

        private bool WriteCaptcha(string captcha)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync(
                    $"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{captcha}\";p.dispatchEvent(new Event('input'));");
                return true;
            }
            catch (Exception e)
            {
                LogLabel.Content = e.Message;
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
                Thread.Sleep(500);
                var result = await Browser.EvaluateScriptAsync(
                    " var xresult = document.evaluate(`//*[@id=\"app\"]/div/a[1]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.click();");

                return result.Success;
            }
            catch (Exception e)
            {
                LogLabel.Content = e.Message;
                return false;
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
                        Timeout = 1000
                    };
                    var request = new RestRequest(method == "post" ? Method.POST : Method.GET);
                    var response = client.Execute(request);
                    Console.WriteLine(response.Content);
                    if (MessageOn.IsChecked == true)
                    {

                    }
                    else
                    {
                        MessageBox.Show(this, ck, "Cookie已上传服务器");

                    }
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

        public static string Phone = "";


        private void ButtonSetPhone_OnClick(object sender, RoutedEventArgs e)
        {
            Phone = Clipboard.GetText(); 
            Clipboard.Clear();
            if (!Helper.IsPhoneNumber(Phone))
            {
                Phone = TextBox.Text;
            }
          
            if (Helper.IsPhoneNumber(Phone))
            {
                WritePhone(Phone);
                try
                {
                    Browser.EvaluateScriptAsync(
                        " var xresult = document.querySelector(\"#app > div > p.policy_tip > input\").click();");
                    Thread.Sleep(500);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                ClickGetCaptchaButton();
            }
            else
            {
                WritePhone("");
            }

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
                LogLabel.Content = e.Message;
                return false;
            }
        }

        private async void ButtonSetCaptcha_OnClick(object sender, RoutedEventArgs e)
        {
            var captcha = Clipboard.GetText(); Clipboard.Clear();
            if (!Helper.IsCaptcha(captcha))
            {
                captcha = TextBox.Text;
            }

            if (Helper.IsCaptcha(captcha))
            {
                WriteCaptcha(captcha);
                await ClickLoginButton();
            }
        }

        private async void ButtonHandleId_OnClick(object sender, RoutedEventArgs e)
        {
            if (UserList.FirstOrDefault(u => u.Phone == Phone) is { } user)
            {
                try
                {
                    var id_2d_clip = Clipboard.GetText();
                    if (string.IsNullOrEmpty(user.Id2_4) &&
                        (!string.IsNullOrEmpty(TextBox.Text) || !string.IsNullOrEmpty(id_2d_clip)))
                    {
                        user.Id2_4 = string.IsNullOrEmpty(TextBox.Text) ? id_2d_clip : TextBox.Text;
                    }

                    if (string.IsNullOrEmpty(user.Id2_4))
                    {
                        return;
                    }

                    if (user.Id2_4.Length == 6)
                    {
                        user.Id2_4 = user.Id2_4.Insert(2, " ");
                    }

                    for (var i = 0; i < user.Id2_4.Length; i++)
                    {
                        var js =
                            $"document.querySelector(\"#app > div > div.wrap > div.input-box > div > div:nth-child({1 + i})\").innerText = {user.Id2_4[i]}";
                        var r = await Browser.EvaluateScriptAsPromiseAsync(js);
                        Console.WriteLine($"{r.Message}");
                        if (i == 1)
                        {
                            i++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (MessageOn.IsChecked == true)
                    {

                    }
                    else
                    {
                        MessageBox.Show(exception.Message);
                    }
                }

                //todo:sendkey
            }

            //todo:获取本次登陆手机号的缓存(或textbox输入内容解析)的身份证信息,然后自动点击验证身份证,自动输入,自动执行
        }

        private bool WriteUserId2_4(string id2_4)
        {
            try
            {
                Browser.EvaluateScriptAsPromiseAsync(
                    $"var xresult = document.evaluate(`//*[@id=\"authcode\"]`, document, null, XPathResult.ANY_TYPE, null);var p=xresult.iterateNext();p.value=\"{id2_4}\";p.dispatchEvent(new Event('input'));");
                if (UserList.FirstOrDefault(u => u.Phone == Phone) is User user)
                {
                    user.Id2_4 = id2_4;
                }
                else
                {
                    // UserList.Add(new User(phone, DateTime.Now,ck) { Id2_4 = id2_4 });
                }

                return true;
            }
            catch (Exception e)
            {
                LogLabel.Content = e.Message;
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

        private async void ButtonSetCK_OnClick(object sender, RoutedEventArgs e)
        {
            //todo:判定ck合法性,设置到浏览器
            var ck = Clipboard.GetText();
            if (!Helper.IsCk(ck))
            {
                ck = TextBox.Text;
            }

            if (Helper.IsCk(ck))
            {
                await SetBrowserCk(ck);
                //GetBrowserCk();
            }
            else
            {
                WritePhone("");
            }
        }

        private async Task SetBrowserCk(string ck)
        {
            var cookie = QingLongJdCookie.Parse(ck);
            await CookieManager.SetCookieAsync("https://m.jd.com/", new CefSharp.Cookie
            {
                Domain = ".jd.com",
                Name = "pt_pin",
                Value = cookie.ptPin,
            });
            await CookieManager.SetCookieAsync("https://m.jd.com/", new CefSharp.Cookie
            {
                Domain = ".jd.com",
                Name = "pt_key",
                Value = cookie.ptKey,
            });
            Browser.ReloadCommand.Execute(null);
        }

        public List<string> CookieStringList = new List<string>();
        private ObservableCollection<User> _userList = new ObservableCollection<User>();

        private void ButtonReadAllCK_OnClick(object sender, RoutedEventArgs e)
        {
            ReadAllCK();
        }

        private void ReadAllCK()
        {
            try
            {
                if (!File.Exists(CookiePath))
                {
                    File.Create(CookiePath);
                }

                CookieStringList = File.ReadAllLines(CookiePath).ToList();
                CkIndex = 0;
                if (string.IsNullOrWhiteSpace(JsBox.Text))
                {
                    this.JsBox.Text =
                 "var btn=document.evaluate('id(\"vueContainer\")/div[@class=\"coupon - btns\"]/div[@class=\"btn\"]', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;btn.click();";

                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        public int CkIndex { get; set; } = 0;


        private void AutoLoopRun()
        {
            if (IsRunning)
            {
                return;
            }
            Task.Factory.StartNew(async () =>
            {
                IsRunning = true;
                while (CkIndex < CookieStringList.Count && !stop)
                {
                    try
                    {
                        var ck = "";
                        var js = "";
                        var url = "";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.TextBox.Text = CookieStringList[CkIndex++];
                            ck = this.TextBox.Text;
                            js = this.JsBox.Text;
                            url = this.UrlBox.Text;
                        });
                        await DoAction(ck, js, url);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);

                    }

                }

                IsRunning = false;
            });
        }

        private bool _IsRunning = false;

        public bool IsRunning
        {
            get { return _IsRunning; }
            set
            {
                _IsRunning = value;
                RaisePropertyChanged();
            }
        }

        public static string GetElement()
        {
            return "";
            //todo:加载事件绑定(附加条件),监听鼠标右键也行,执行js获取鼠标位置的element(tostring)就是xpath,注入根据xpath获取element代码,

        }

        private async Task DoAction(string ck, string js, string url)
        {
            try
            {
                if (Helper.IsCk(ck))
                {
                    await SetBrowserCk(ck);
                    Browser.Load(url);
                    Thread.Sleep(1000 * 3);
                    if (!string.IsNullOrWhiteSpace(js))
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            while (!stop && !EvaluateScript(js))
                            {
                                Thread.Sleep(1000 * 2);
                                if (stop)
                                {
                                    break;
                                }
                            }
                            Thread.Sleep(1000 * 2);
                        });
                    }
                }
                else
                {
                    WritePhone("");
                }
            }
            catch (Exception exception)
            {
                if (MessageOn.IsChecked == true)
                {
                }
                else
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private void ButtonGoToUrl_OnClick(object sender, RoutedEventArgs e)
        {
            Browser.Address = this.UrlBox.Text;
        }

        private void ButtonDevTools_OnClick(object sender, RoutedEventArgs e)
        {
            Browser.ShowDevTools();
        }

        private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LoginMode.IsChecked == true)
            {
                var phone = ((sender as DataGrid)?.SelectedItem as User)?.Phone;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    TextBox.Text = ((sender as DataGrid).SelectedItem as User).Phone;
                }

                var userAgent = ((sender as DataGrid)?.SelectedItem as User)?.UserAgent;
                if (!string.IsNullOrWhiteSpace(userAgent) && this.DefaultUA != userAgent)
                {
                    this.DefaultUA = userAgent;
                }
            }
        }

        private void ButtonCheckAllLogin_OnClick(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            Task.Factory.StartNew(() =>
            {
                foreach (var user in UserList)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        LogLabel.Content = $"{user.NickName}-{UserList.IndexOf(user)}/{UserList.Count}";
                    });
                    CheckAndUpdateUserState(user);
                }

                this.Dispatcher.Invoke(() =>
                {
                    LogLabel.Content = $"执行完成";
                    b.IsEnabled = true;
                });

            });

        }
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var tb = sender as ToggleButton;
            if (tb.IsChecked == true)
            {
                this.Width = 1405;
            }
            else
            {
                this.Width = 405;
            }
        }

        public void ButtonCheckLogin_OnClick(object sender, RoutedEventArgs e)
        {
            var user = this.DataGrid.SelectedItem as User;
            LogLabel.Content = $"{user.NickName}";
            CheckAndUpdateUserState(user);
        }

        public static ObservableCollection<string> UAs
        {
            get => _uAs;
            set
            {
                _uAs = value;
            }
        }
        private string _defaultUa;

        public string DefaultUA
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultUa))
                {
                    _defaultUa = UAs.FirstOrDefault();
                }
                return _defaultUa;
            }
            set
            {
                _defaultUa = value;
                RaisePropertyChanged();
                JdService.ClientUserAgent = value;
                File.WriteAllText("ua.txt", value);
                if (MessageBox.Show("已切换新的UserAgent,是否重启?", "确认", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    var fileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    Process.Start(fileName.Replace("dll", "exe"));
                    this.Dispatcher.Invoke((ThreadStart)delegate ()
                        {
                            Application.Current.Shutdown();
                        }
                    );
                }
            }
        }

        public MyCommand SaveCommand
        {
            get
            {
                return new MyCommand(() =>
           {
               this.IsSimpleMode = Visibility.Visible;
           });
            }
        }

        private static void CheckAndUpdateUserState(User user)
        {
            var re = JdService.GetUserInfo(user.CookieString);
            if (re.msg == "success" && re.retcode == "0")
            {
                user.UserInfoData = re;
                user.IsLogin = true;
                if (user.AddressList == null || !user.AddressList.Any())
                {
                    user.AddressList = JdService.GetAddressList(user.CookieString);
                }
            }
            else
            {
                user.IsLogin = false;
            }
        }

        private bool EvaluateScript(string js)
        {
            try
            {
                var r = Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var javascriptResponse = Browser.EvaluateScriptAsync(js, new TimeSpan(0, 0, 1)).Result;
                        Console.WriteLine(javascriptResponse.Message);
                        return javascriptResponse.Success;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return true;
                    }
                });
                return r;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

        }
        private void ButtonExecuteJS_OnClick(object sender, RoutedEventArgs e)
        {
            EvaluateScript(JsBox.Text);
        }
        private void ButtonEvaluateAll_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonEvaluate_OnClick(object sender, RoutedEventArgs e)
        {

        }


        private void Mode2RB_OnChecked(object sender, RoutedEventArgs e)
        {
            ReadAllCK();
        }

        private void ButtonCronLoopRun_OnClick(object sender, RoutedEventArgs e)
        {
            stop = false;
            var dateTime = DateTimePicker.Value;
            Task.Factory.StartNew(() =>
            {

                while (DateTime.Now <= dateTime && !stop)
                {
                    Thread.Sleep(1000);
                }
                if (DateTime.Now >= dateTime && !stop)
                {
                    AutoLoopRun();
                }
            });
        }

        private bool exit = false;
        private bool stop = false;
        private static ObservableCollection<string> _uAs = new ObservableCollection<string>()
        {"Mozilla/5.0 (iPhone; CPU iPhone OS 15_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 PrivaBrowser-iOS/0.75 Version/75 Safari/605.1.15",
      "Mozilla/5.0 (iPhone; CPU iPhone OS 15_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/105.0.5195.98 Mobile/15E148 Safari/604.1",
      "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/99.0.4844.47 Mobile/15E148 Safari/604.1",
     "Mozilla/5.0 (Linux; Android............like Gecko) Chrome/92.0.4515.105 HuaweiBrowser/12.1.2.311 Mobile Safari/537.36",

      "jdapp;iPhone;11.2.6;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.2.5;;;Mozilla/5.0 (Linux; Android 9; Mi Note 3 Build/PKQ1.181007.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/045131 Mobile Safari/537.36",
  "jdapp;android;11.2.4;;;Mozilla/5.0 (Linux; Android 10; GM1910 Build/QKQ1.190716.003; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;android;11.2.2;;;Mozilla/5.0 (Linux; Android 9; 16T Build/PKQ1.190616.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;iPhone;11.2.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.1.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.1.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.1.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.0.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.0.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.0.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.2.8;;;Mozilla/5.0 (Linux; Android 9; MI 6 Build/PKQ1.190118.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;android;11.2.6;;;Mozilla/5.0 (Linux; Android 11; Redmi K30 5G Build/RKQ1.200826.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045511 Mobile Safari/537.36",
  "jdapp;iPhone;11.2.5;;;Mozilla/5.0 (iPhone; CPU iPhone OS 11_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15F79",
  "jdapp;android;11.2.4;;;Mozilla/5.0 (Linux; Android 10; M2006J10C Build/QP1A.190711.020; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;android;11.2.2;;;Mozilla/5.0 (Linux; Android 10; M2006J10C Build/QP1A.190711.020; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;android;11.2.0;;;Mozilla/5.0 (Linux; Android 10; ONEPLUS A6000 Build/QKQ1.190716.003; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045224 Mobile Safari/537.36",
  "jdapp;android;11.1.4;;;Mozilla/5.0 (Linux; Android 9; MHA-AL00 Build/HUAWEIMHA-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;android;11.1.2;;;Mozilla/5.0 (Linux; Android 8.1.0; 16 X Build/OPM1.171019.026; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;android;11.1.0;;;Mozilla/5.0 (Linux; Android 8.0.0; HTC U-3w Build/OPR6.170623.013; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;iPhone;11.0.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_0_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.0.2;;;Mozilla/5.0 (Linux; Android 10; LYA-AL00 Build/HUAWEILYA-AL00L; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;iPhone;11.0.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;10.5.0;;;Mozilla/5.0 (Linux; Android 8.1.0; MI 8 Build/OPM1.171019.026; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/045131 Mobile Safari/537.36",
  "jdapp;android;11.2.8;;;Mozilla/5.0 (Linux; Android 10; Redmi K20 Pro Premium Edition Build/QKQ1.190825.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045227 Mobile Safari/537.36",
  "jdapp;iPhone;11.2.5;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.2.2;;;Mozilla/5.0 (Linux; Android 11; Redmi K20 Pro Premium Edition Build/RKQ1.200826.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045513 Mobile Safari/537.36",
  "jdapp;android;11.2.0;;;Mozilla/5.0 (Linux; Android 10; MI 8 Build/QKQ1.190828.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045227 Mobile Safari/537.36",
  "jdapp;iPhone;11.1.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
    "jdapp;android;11.0.1;;;Mozilla/5.0 (Linux; Android 10; ONEPLUS A5010 Build/QKQ1.191014.012; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;iPhone;11.1.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.1.0;;;Mozilla/5.0 (Linux; Android 10; Mi Note 5 Build/PKQ1.181007.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/045131 Mobile Safari/537.36",
  "jdapp;android;11.0.4;;;Mozilla/5.0 (Linux; Android 11; LIO-AN00 Build/HUAWEILIO-AN00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;android;11.0.2;;;Mozilla/5.0 (Linux; Android 10; SKW-A0 Build/SKYW2001202CN00MQ0; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;iPhone;11.0.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;10.5.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.8;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.5;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 13_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.1.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.1.2;;;Mozilla/5.0 (Linux; Android 9; MI 6 Build/PKQ1.190118.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;android;11.1.0;;;Mozilla/5.0 (Linux; Android 12; Redmi K30 5G Build/RKQ1.200826.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045511 Mobile Safari/537.36",
  "jdapp;iPhone;11.0.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 11_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15F79",
  "jdapp;android;11.0.2;;;Mozilla/5.0 (Linux; Android 10; M2006J10C Build/QP1A.190711.020; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;android;11.0.0;;;Mozilla/5.0 (Linux; Android 12; HWI-AL00 Build/HUAWEIHWI-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;android;10.5.4;;;Mozilla/5.0 (Linux; Android 10; ANE-AL00 Build/HUAWEIANE-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045224 Mobile Safari/537.36",
  "jdapp;android;10.5.2;;;Mozilla/5.0 (Linux; Android 9; ELE-AL00 Build/HUAWEIELE-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;android;10.5.0;;;Mozilla/5.0 (Linux; Android 10; LIO-AL00 Build/HUAWEILIO-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;android;11.2.8;;;Mozilla/5.0 (Linux; Android 10; SM-G9750 Build/QP1A.190711.020; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/044942 Mobile Safari/537.36",
  "jdapp;iPhone;11.2.5;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_0_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.2.4;;;Mozilla/5.0 (Linux; Android 12; EVR-AL00 Build/HUAWEIEVR-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045230 Mobile Safari/537.36",
  "jdapp;iPhone;11.2.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.2.0;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.1.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.1.2;;;Mozilla/5.0 (Linux; Android 8.1.0; MI 8 Build/OPM1.171019.026; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/78.0.3904.108 MQQBrowser/6.2 TBS/045131 Mobile Safari/537.36",
  "jdapp;android;11.1.0;;;Mozilla/5.0 (Linux; Android 9; HLK-AL00 Build/HONORHLK-AL00; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045227 Mobile Safari/537.36",
  "jdapp;iPhone;11.0.4;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;iPhone;11.0.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
  "jdapp;android;11.0.0;;;Mozilla/5.0 (Linux; Android 10; LYA-AL10 Build/HUAWEILYA-AL10; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045513 Mobile Safari/537.36",
  "jdapp;android;10.5.4;;;Mozilla/5.0 (Linux; Android 10; MI 9 Build/QKQ1.190825.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/76.0.3809.89 MQQBrowser/6.2 TBS/045227 Mobile Safari/537.36",
  "jdapp;iPhone;10.5.2;;;Mozilla/5.0 (iPhone; CPU iPhone OS 14_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148;supportJDSHWK/1",
        };

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            stop = !stop;
        }

        private void ButtonGetElement_OnClick(object sender, RoutedEventArgs e)
        {
            Browser.EvaluateScriptAsync(File.ReadAllText("helper.js"));
            //todo:定时去  Clipboard.GetText();获取xpath,用于循环执行时候
        }

        private void ButtonChangeUA_OnClick(object sender, RoutedEventArgs e)
        {
            JdService.ChangeUserAgent();

        }


        private void UrlBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlBox.Text) || !UrlBox.Text.Contains("http"))
            {
                return;
            }
            File.WriteAllText("url.txt", UrlBox.Text);
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => { ListenClipboard(); });
        }
        private void ListenClipboard()
        {
            var isChecked =true ;
            this.Dispatcher.Invoke(() =>
            {
                isChecked= AutoListenCheckBox.IsChecked == true;
            });

            while (!exit&& isChecked)
            {
                this.Dispatcher.Invoke(() =>
                {
                    
                    if (AutoListenCheckBox.IsChecked != true)
                    {
                       return;
                    }
                    var text = Clipboard.GetText();
                    if (Helper.IsPhoneNumber(text))
                    {
                        ButtonSetPhone_OnClick(null, null);
                    }

                    if (Helper.IsCaptcha(text))
                    {
                        ButtonSetCaptcha_OnClick(null, null);
                    }
                    isChecked = AutoListenCheckBox.IsChecked == true;
                });

                Thread.Sleep(500);
            }
        }
    }


    public static class ListExtension
    {
        public static string ToCkString(this IEnumerable<Cookie> cks)
        {
            cks = cks.OrderBy(c => c.Name).ToList();
            var re = "";
            if (cks.Any())
            {
                foreach (var ck in cks)
                {
                    Regex reg = new Regex(@"[\u4e00-\u9fa5]");
                    if (reg.IsMatch(ck.Value))
                    {
                        re += $"{ck.Name}={System.Web.HttpUtility.UrlEncode(ck.Value)};";
                    }
                    else
                    {
                        re += $"{ck.Name}={ck.Value};";
                    }
                }
            }

            return re;
        }
    }
    public class MyCommand : ICommand
    {
        private Action _execute;
        private Func<bool> _canExecute;

        public MyCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}



