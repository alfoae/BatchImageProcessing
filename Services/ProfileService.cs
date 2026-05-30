using ImageProcessing.Models;
using System.Text.Json;
using System.IO;

namespace ImageProcessing.Services
{
    public static class ProfileService
    {
        private static readonly string FilePath = "profiles.json";

        public static List<ProcessingProfile> Profiles { get; set; } = new();

        static ProfileService()
        {
            Load();
        }

        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                Save();
                return;
            }

            string json = File.ReadAllText(FilePath);

            Profiles = JsonSerializer.Deserialize<List<ProcessingProfile>>(json) ?? new List<ProcessingProfile>();
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(
                Profiles,
                new JsonSerializerOptions{WriteIndented = true});

            File.WriteAllText(FilePath, json);
        }
    }
}