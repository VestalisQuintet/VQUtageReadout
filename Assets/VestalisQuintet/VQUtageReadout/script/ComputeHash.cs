using System.Text;
using System.Security.Cryptography;

namespace VestalisQuintet.VQUtageReadout
{
    public static class ComputeHash
    {

        public static string ComputeSha256Hash(string rawData)
        {
            // SHA256 のインスタンスを作成します。
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // テキストをバイト配列に変換します。
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // バイト配列を16進数の文字列に変換します。
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
