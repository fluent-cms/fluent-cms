using System.Text;

namespace FluentCMS.Utils.Base64Url
{
    public static class Base64UrlEncoder
    {
        public static byte[] Decode(string input)
        {
            input = input.Replace('-', '+').Replace('_', '/');
            switch (input.Length % 4)
            {
                case 2: input += "=="; break;
                case 3: input += "="; break;
            }
            return Convert.FromBase64String(input);
        }

        public static string Encode(string input)
        {
            var output = Convert.ToBase64String(Encoding.Unicode.GetBytes(input));
            output = output.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return output;
        }
    }
}