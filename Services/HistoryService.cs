using ImageProcessing.Models;
using System.Text.Json;
using System.IO;

namespace ImageProcessing.Services
{
    public static class HistoryService
    {
        private static readonly string FilePath =
            "history.json";

        public static List<TaskHistory> Tasks { get; private set; } = new();

        static HistoryService()
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

            Tasks = JsonSerializer.Deserialize<List<TaskHistory>>(json) ?? new List<TaskHistory>();
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(Tasks, new JsonSerializerOptions{WriteIndented = true});

            File.WriteAllText(FilePath, json);
        }

        public static void AddTask(string taskName, string status, int fileCount)
        {
            Tasks.Add(new TaskHistory
            {
                TaskName = taskName,
                Status = status,
                Date = DateTime.Now,
                FileCount = fileCount
            });

            Save();
        }
    }
}
