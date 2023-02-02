using System;
using JdLoginTool.Wpf.Model;
using Newtonsoft.Json;
using RestSharp;

namespace JdLoginTool.Wpf.Service
{
    public static class JdService
    { 
        private static string _clientUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/99.0.4844.47 Mobile/15E148 Safari/604.1";

        public static string ClientUserAgent
        {
            get => _clientUserAgent;
            set
            {
                _clientUserAgent = value;
            }
        }

        public static void ChangeUserAgent()
        {
      }

          //  "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1";
      
        
        public static UserInfoDetail GetUserInfo(string ck)
        {
            var client = new RestClient("https://me-api.jd.com/user_new/info/GetJDUserInfoUnion");
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

             client.UserAgent = ClientUserAgent;
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
            var json = response.Content;
            var result = JsonConvert.DeserializeObject<UserInfoDetail>(json);
            return result;
        }


        public static AddressList[] GetAddressList(string ck)
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
            client.UserAgent = ClientUserAgent;
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
            var json = response.Content.Replace("cbLoadAddressListA(", "");
            json = json.Remove(json.Length - 1);
            var result = JsonConvert.DeserializeObject<ResultObject>(json);
            return result.list;

        }

         

    }
}