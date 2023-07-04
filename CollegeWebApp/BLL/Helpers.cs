using System.Security.Cryptography;
using System.Text;

namespace CollegeWebApp.BLL
{
    public class Helpers
    {
        public static string ConvertToBase64String(IFormFile file)
        {
            if (file.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            return String.Empty;
        }

        public static byte[] ConvertToByteArray(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                byte[] byteArray = memoryStream.ToArray();
                return byteArray;
            }
        }

        public static string GeneratePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

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
