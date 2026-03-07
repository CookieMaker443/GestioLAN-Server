using System.Text;


namespace GestioLan.API.Utils.Helpers;

    public static class StringHelper
    {
        // Funzione per convertire una stringa in un formato adatto per la ricerca con LIKE
        public static readonly Random _random = new Random();
        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            // NOTA: StringBuilder è una "lavagna" su cui si costruisce la stringa, è più efficiente di concatenare direttamente le stringhe
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[_random.Next(chars.Length)]);
            }
            return result.ToString();
        }
    }
