using System.Text.RegularExpressions;

namespace JdLoginTool.Wpf.Utility
{
    public static class Helper
    {

        public static bool IsPhoneNumber(string phoneNumber)
        {
            return Regex.IsMatch(phoneNumber, @"^1(3[0-9]|5[0-9]|7[6-8]|8[0-9])[0-9]{8}$");
        }
        public static bool IsCaptcha(string captcha)
        {
            return Regex.IsMatch(captcha, @"^\d{6}$");
        }

        public static bool IsCk(string ck)
        {
            return ck.Contains("pt_key") && ck.Contains("pt_pin") && ck.Length > 20;
        }
    }
}