using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using CefSharp;
using JdLoginTool.Wpf.Service;
using Newtonsoft.Json;

namespace JdLoginTool.Wpf.Model
{
    public class User : INotifyPropertyChanged
    {
        private DateTime _notifyDateTime;

        [Description("建议再次登陆时间")]
        public DateTime NotifyDateTime
        {
            get
            {
                return _notifyDateTime;
            }
            set
            {
                _notifyDateTime = value;
                RaisePropertyChanged();
            }
        }
        private string _userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 15_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 PrivaBrowser-iOS/0.75 Version/75 Safari/605.1.15";
        [Browsable(false)]
        public string UserAgent
        {
            get { return _userAgent; }
            set
            {
                if (value != null)
                {
                    _userAgent = value;
                }  
                RaisePropertyChanged();
            }
        }


        [Description("手机号")]
        public string Phone { get; set; }
        [Description("账号Pin")]
        public string Pin
        {
            get
            {
                if (UserInfoData != null)
                {
                    return UserInfoData.data.userInfo.baseInfo.curPin;
                }
                else
                {
                    return "";
                }
            }
        }
        [Description("身份证2+4")]
        public string Id2_4 { get; set; }
        [Description("vip")]
        public string isPlusVip
        {
            get
            {
                var tmp = "";
                try
                {
                    if (UserInfoData != null) tmp = UserInfoData.data.userInfo.isPlusVip;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return tmp;
            }
        }
        [Description("京豆数量")]
        public int beanNum
        {
            get
            {
                var tmp = 0;
                try
                {
                    if (UserInfoData != null) tmp = int.Parse(UserInfoData.data.assetInfo.beanNum);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return tmp;
            }
        }
        [Description("剩余有效时间")]
        public int OnLineDaysLeft
        {
            get
            {
                if (this.Expires == null)
                {
                    return -1;
                }
                else
                {
                    return ((DateTime)Expires - DateTime.Now).Days;
                }
            }
        }


        private bool isLogin = true;
        [Description("是否在线")]
        public bool IsLogin
        {
            get
            {
                return isLogin;
            }
            set
            {
                isLogin = value;
                RaisePropertyChanged();
            }
        }
        [Description("ck字符串")]
        public string CookieString
        {
            get
            {
                if (Cookies == null)
                {
                    return serverCkString;
                }
                return Cookies.Any() ? string.Join(";", Cookies.Select(c => $"{c.Name}={c.Value};")) : "";
            }
            set { serverCkString = value; }
        }
        private string serverCkString = "";

        [Browsable(false)]
        public Cookie[] Cookies
        {
            get => _cookies;
            set
            {
                _cookies = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(OnLineDaysLeft));
                RaisePropertyChanged(nameof(Expires));
            }
        }
        [Description("CK过期时间")]
        public DateTime? Expires => Cookies?.FirstOrDefault()?.Expires;

        [Description("昵称")]
        public string NickName
        {
            get
            {
                if (UserInfoData != null)
                {
                    return UserInfoData.data.userInfo.baseInfo.nickname;
                }
                else
                {
                    return "";
                }
            }
        }

        private UserInfoDetail _userInfoData;
        private Cookie[] _cookies;
        private AddressList[] _addressList;

        [Description("用户信息详情")]
        public UserInfoDetail UserInfoData
        {
            get
            {
                return _userInfoData;
            }
            set
            {
                _userInfoData = value;
                RaisePropertyChanged();
            }
        }

        [Description("地址列表")]
        public AddressList[] AddressList
        {
            get => _addressList;
            set
            {
                _addressList = value;
                RaisePropertyChanged();
            }
        }

        public User(string phone, DateTime loginDate)
        {
            this.Phone = phone;
            this.NotifyDateTime = loginDate;
        }

        public User()
        {

        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class UserInfoDetail
    {
        public Data data { get; set; }
        public string msg { get; set; }
        public string retcode { get; set; }
        public long timestamp { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this.data);
        }
    }

    public class Data
    {
        public Jdvvipcocooninfo JdVvipCocoonInfo { get; set; }
        public Jdvvipinfo JdVvipInfo { get; set; }
        public Assetinfo assetInfo { get; set; }
        public Favinfo favInfo { get; set; }
        public Gamebubblelist[] gameBubbleList { get; set; }
        public Growhelpercoupon growHelperCoupon { get; set; }
        public Kplinfo kplInfo { get; set; }
        public Orderinfo orderInfo { get; set; }
        public Plusfloor plusFloor { get; set; }
        public Pluspromotion plusPromotion { get; set; }
        public Tfadvertinfo tfAdvertInfo { get; set; }
        public Userinfo userInfo { get; set; }
        public Userlifecycle userLifeCycle { get; set; }
    }

    public class Jdvvipcocooninfo
    {
        public string JdVvipCocoonStatus { get; set; }
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
    public class Jdvvipinfo
    {
        public string jdVvipStatus { get; set; }
    }

    public class Assetinfo
    {
        public string accountBalance { get; set; }
        public Baitiaoinfo baitiaoInfo { get; set; }
        public string beanNum { get; set; }
        public Btffkinfo btFfkInfo { get; set; }
        public string couponNum { get; set; }
        public string couponRed { get; set; }
        public string redBalance { get; set; }
    }

    public class Baitiaoinfo
    {
        public string availableLimit { get; set; }
        public string baiTiaoStatus { get; set; }
        public string bill { get; set; }
        public string billOverStatus { get; set; }
        public string outstanding7Amount { get; set; }
        public string overDueAmount { get; set; }
        public string overDueCount { get; set; }
        public string unpaidForAll { get; set; }
        public string unpaidForMonth { get; set; }
    }

    public class Btffkinfo
    {
        public string appId { get; set; }
        public string linkUrl { get; set; }
        public string numText { get; set; }
        public string numUnitText { get; set; }
        public string status { get; set; }
        public string subtitle { get; set; }
        public string title { get; set; }
    }

    public class Favinfo
    {
        public string contentNum { get; set; }
        public string favDpNum { get; set; }
        public string favGoodsNum { get; set; }
        public string favShopNum { get; set; }
        public string footNum { get; set; }
        public string isGoodsRed { get; set; }
        public string isShopRed { get; set; }
    }

    public class Growhelpercoupon
    {
        public int addDays { get; set; }
        public int batchId { get; set; }
        public int couponKind { get; set; }
        public int couponModel { get; set; }
        public int couponStyle { get; set; }
        public int couponType { get; set; }
        public float discount { get; set; }
        public int limitType { get; set; }
        public int msgType { get; set; }
        public float quota { get; set; }
        public int roleId { get; set; }
        public int state { get; set; }
        public int status { get; set; }
    }

    public class Kplinfo
    {
        public string kplInfoStatus { get; set; }
        public string mopenbp17 { get; set; }
        public string mopenbp22 { get; set; }
    }

    public class Orderinfo
    {
        public string commentCount { get; set; }
        public object[] logistics { get; set; }
        public string orderCountStatus { get; set; }
        public string receiveCount { get; set; }
        public string waitPayCount { get; set; }
    }

    public class Plusfloor
    {
        public Lefttab[] leftTabs { get; set; }
        public Midtab[] midTabs { get; set; }
        public Righttab[] rightTabs { get; set; }
    }

    public class Lefttab
    {
        public int contentType { get; set; }
        public string imageUrl { get; set; }
        public string link { get; set; }
        public string subTitle { get; set; }
        public string title { get; set; }
    }

    public class Midtab
    {
        public int contentType { get; set; }
        public string imageUrl { get; set; }
        public string link { get; set; }
        public string subTitle { get; set; }
        public string title { get; set; }
    }

    public class Righttab
    {
        public int contentType { get; set; }
        public string imageUrl { get; set; }
        public string link { get; set; }
        public string subTitle { get; set; }
        public string title { get; set; }
    }

    public class Pluspromotion
    {
        public int status { get; set; }
    }

    public class Tfadvertinfo
    {
        public string status { get; set; }
    }

    public class Userinfo
    {
        public Baseinfo baseInfo { get; set; }
        public string isHideNavi { get; set; }
        public string isHomeWhite { get; set; }
        public string isJTH { get; set; }
        public string isKaiPu { get; set; }
        public string isPlusVip { get; set; }
        public string isQQFans { get; set; }
        public string isRealNameAuth { get; set; }
        public string isWxFans { get; set; }
        public string jvalue { get; set; }
        public string orderFlag { get; set; }
        public Plusinfo plusInfo { get; set; }
        public string tmpActWaitReceiveCount { get; set; }
        public string xbKeepLink { get; set; }
        public string xbKeepOpenStatus { get; set; }
        public string xbKeepScore { get; set; }
        public string xbScore { get; set; }
    }

    public class Baseinfo
    {
        public string accountType { get; set; }
        public string baseInfoStatus { get; set; }
        public string curPin { get; set; }
        public string definePin { get; set; }
        public string headImageUrl { get; set; }
        public string levelName { get; set; }
        public string nickname { get; set; }
        public string userLevel { get; set; }
    }

    public class Plusinfo
    {
    }

    public class Userlifecycle
    {
        public string identityId { get; set; }
        public string lifeCycleStatus { get; set; }
        public string trackId { get; set; }
    }

    public class Gamebubblelist
    {
        public Carouselinfo[] carouselInfos { get; set; }
        public string key { get; set; }
        public string title { get; set; }
    }

    public class Carouselinfo
    {
        public string icon { get; set; }
        public string text { get; set; }
    }

}