using System.IO;
using ImageProcessing.Models;
using System.Text.Json;

namespace ImageProcessing.Services
{
    public static class AuthService
    {
        public static User? CurrentUser { get; set; }
        private static readonly string FilePath = "users.json";

        public static List<User> Users { get; private set; } = new();

        static AuthService()
        {
            Load();
        }

        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                Users = new List<User>();
                Save();
                return;
            }

            string json = File.ReadAllText(FilePath);
            Users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();

            bool changed = false;
            foreach (var u in Users)
            {
                if (string.IsNullOrWhiteSpace(u.Password)) continue;

                if (!u.Password.StartsWith("$2"))
                {
                    u.Password = BCrypt.Net.BCrypt.HashPassword(u.Password);
                    changed = true;
                }
            }

            if (changed) Save();
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(Users, new JsonSerializerOptions{WriteIndented = true});

            File.WriteAllText(FilePath, json);
        }

        public static bool Register(string login, string password)
        {
            if (Users.Any(x => x.Login == login)) return false;

            Users.Add(new User{Login = login, Password = BCrypt.Net.BCrypt.HashPassword(password), Role = "User"});

            Save();

            return true;
        }

        public static User? Login(string login, string password)
        {
            User? user = Users.FirstOrDefault(x => x.Login == login);

            if (user == null) return null;

            bool valid = BCrypt.Net.BCrypt.Verify( password, user.Password);

            return valid ? user : null;
        }
    }
}
