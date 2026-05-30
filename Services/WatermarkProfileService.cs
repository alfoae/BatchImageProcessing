using ImageProcessing.Models;

using System.IO;
using System.Text.Json;

namespace ImageProcessing.Services
{
    public static class WatermarkProfileService
    {
        private static readonly string FilePath = "watermark_profiles.json";

        public static List<WatermarkProfile> Profiles { get; set; } = new();

        static WatermarkProfileService()
        {
            Load();
        }

        public static void Load()
        {
            if (!File.Exists(FilePath)) { Save(); return; }

            string json = File.ReadAllText(FilePath);
            Profiles = JsonSerializer.Deserialize<List<WatermarkProfile>>(json) ?? new List<WatermarkProfile>();
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(Profiles, new JsonSerializerOptions { WriteIndented = true }); 
            File.WriteAllText(FilePath, json);
        }
    }
}
