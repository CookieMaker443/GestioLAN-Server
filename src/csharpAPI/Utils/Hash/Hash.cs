using BCrypt.Net;

namespace csharpAPI.Utils.Hash
{
    public class Hash
    {
        // Semplice metodo di hashing (non sicuro, solo a scopo dimostrativo) - fatto con AI
        public static string SimpleHash(string input)
        {
            // Usa una semplice somma dei codici ASCII come "hash"
            int hash = 0;
            foreach (char c in input)
            {
                hash += (int)c;
            }
            return hash.ToString();
        }

        // Wrapper della libreria di hashing BCrypt.Net
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}