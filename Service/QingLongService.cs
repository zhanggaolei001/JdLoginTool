using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Windows;
using JdLoginTool.Wpf.Model.Qinglong;
using Newtonsoft.Json;
using RestSharp;

namespace JdLoginTool.Wpf.Service
{
    public static class QingLongService
    {
        private static string qlToken { get; set; }
        public static void GetQingLongToken()
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
                var result = JsonConvert.DeserializeObject<QlTokenResult>(response.Content);
                qlToken = result.data.token;
                //todo:保存 qlToken,下次运行先拿,并判断是否过期,过期删除,重新获取,后面再实现,目前应用场景影响不大.
            }
        }
        public static void UploadToQingLong(string ck, string phoneNumber, bool? isMessageOn = false)
        {
            var qlUrl = ConfigurationManager.AppSettings["qlUrl"];
            if (string.IsNullOrWhiteSpace(qlUrl)) return;
            try
            {
                if (string.IsNullOrWhiteSpace(qlToken))
                {
                    QingLongService.GetQingLongToken();
                }
                if (string.IsNullOrWhiteSpace(qlToken))
                {
                    if (isMessageOn == true)
                    {

                    }
                    else
                    {
                        MessageBox.Show("登陆青龙失败:获取Token失败");

                    }
                    return;
                }
                var jck = QingLongJdCookie.Parse(ck);
                var input = new InputWindow();
                string remarks = phoneNumber ?? jck.ptPin;//正常应该就是手机号了,如果开发的有问题,拿错手机号会用ptPin
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
                if (isMessageOn == true)
                {

                }
                else
                {
                    MessageBox.Show(response.Content, "上传青龙成功(Cookie已复制到剪切板)");


                }
            }
            catch (Exception e)
            {
                if (isMessageOn == true)
                {

                }
                else
                {
                    MessageBox.Show(e.Message, "上传青龙失败,Cookie已复制到剪切板,请自行添加处理");

                }
            }
        }
        public static bool CheckIsNewUser(string qlUrl, string ck, out int id)
        {
            var newCk = QingLongJdCookie.Parse(ck);
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
            if (result.data.Any(jck => QingLongJdCookie.Parse(jck.value).ptPin == newCk.ptPin))
            {
                var firstOrDefault = result.data.FirstOrDefault(jck => newCk.ptPin != null && QingLongJdCookie.Parse(jck.value).ptPin == newCk.ptPin);
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




    }
}