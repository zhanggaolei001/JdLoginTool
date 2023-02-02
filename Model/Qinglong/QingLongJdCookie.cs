using System;

namespace JdLoginTool.Wpf.Model.Qinglong
{
    public class QingLongJdCookie
    {
        public string ptPin { get; set; }
        public string ptKey { get; set; }

        public static QingLongJdCookie Parse(String ck)
        {
            QingLongJdCookie jdCookie = new QingLongJdCookie();
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
                jdCookie = new QingLongJdCookie();
            }
            return jdCookie;
        }


        public override String ToString()
        {
            return "pt_key=" + ptKey + ";pt_pin=" + ptPin + ";";
        }
    }
}