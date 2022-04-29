namespace JdLoginTool.Wpf.Model
{
    public class UserInfo
    {
        public string Phone { get; set; }
        public string Id2_4 { get; set; }
        public string NickName { get; set; }
        public string AddressName { get; set; }

        public UserInfo(string phone)
        {
            this.Phone = phone;

        }
    }
}