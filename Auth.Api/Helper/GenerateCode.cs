using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Helper
{
    public static class GenerateCode
    {
        public static string Generate(int length = 6)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);

            var code = new StringBuilder(length);
            foreach (var b in bytes)
            {
                code.Append((b % 10).ToString());
            }

            return code.ToString();
        }
    }
}
