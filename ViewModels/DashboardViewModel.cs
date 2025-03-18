using Avalonia.Media.Imaging;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FactoryPlanner.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {

        public int Progress { get; set; }
        public bool ProgressBarVisible { get; set; } = true;
        public ObservableCollection<IconTextModel> IconTexts { get; set; } = [];
        public ObservableCollection<PatchNoteModel> PatchNotes { get; set; } = [];


        private readonly SaveFileReader _saveFileReader;
        private readonly string _patchNotesRequestContent = "{\"query\":\"query($date_range: Float, $search: String, $category: String, $sort: String, $status: String, $version_number: String, $answered: String!, $showHidden: Boolean! $skip: Int!, $limit: Int!) {\\n              allPatchNotes(date_range: $date_range, search: $search, category: $category, sort: $sort, status: $status, version_number: $version_number, answered: $answered, showHidden: $showHidden, skip: $skip, limit: $limit) {\\n                posts{\\n                  id\\n                  title\\n                  upvotes\\n                  categories\\n                  contents\\n                  creation_date\\n                  status\\n                  version_number\\n                  author {\\n                      username\\n                      role\\n                  }\\n                  comments{\\n                      id\\n                  }\\n                  isPinned\\n                  pinnedDate\\n                  countComments\\n                  answered\\n                  admin_data{\\n                    in_progress\\n                  }\\n                  hidden\\n                }\\n                totalDocs\\n                totalUpvotes\\n                totalComments\\n                totalPages\\n                limit\\n                hasPrevPage\\n                hasNextPage\\n              }\\n          }\\n        \",\"variables\":{\"date_range\":9999,\"search\":\"\",\"category\":\"\",\"sort\":\"upvotes-asc\",\"status\":\"\",\"answered\":\"all\",\"showHidden\":false,\"skip\":0,\"limit\":20},\"opName\":\"allPatchNotes\"}";


        public DashboardViewModel(IScreen screen) : base(screen)
        {
            string newestSavePath = GetNewestSavePath();

            _saveFileReader = new SaveFileReader(@"C:\Users\JulianBrydaVeloce\Source\Repos\FactoryPlanner\FileReader\1.0 BABY_autosave_2.sav");
            _saveFileReader.OnProgressUpdate += SaveFileReader_OnProgressUpdate;
            _saveFileReader.OnFinish += SaveFileReader_OnFinish;

            GetPatchNotes();
        }

        private void SaveFileReader_OnFinish(object sender)
        {
            TimeSpan playtime = TimeSpan.FromSeconds(_saveFileReader.Header.PlayedSeconds);

            IconTexts.Add(new IconTextModel() { Name = $"{playtime.Hours + playtime.Days * 24}h {playtime.Minutes}m", Image = new(".\\Assets\\Icons\\Playtime.png"), Width = 200, Height = 120 });


            int smelterCount = _saveFileReader.CountActCompHeader(TypePaths.SmelterMk1);
            int assemblerCount = _saveFileReader.CountActCompHeader(TypePaths.AssemblerMk1);
            int constructorCount = _saveFileReader.CountActCompHeader(TypePaths.ConstructorMk1);
            int refineryCount = _saveFileReader.CountActCompHeader(TypePaths.OilRefinery);

            IconTexts.Add(new IconTextModel() { Name = smelterCount.ToString(), Image = new(".\\Assets\\Icons\\Smelter.png"), Width = 200, Height = 120 });
            IconTexts.Add(new IconTextModel() { Name = assemblerCount.ToString(), Image = new(".\\Assets\\Icons\\Assembler.png"), Width = 200, Height = 120 });
            IconTexts.Add(new IconTextModel() { Name = constructorCount.ToString(), Image = new(".\\Assets\\Icons\\Constructor.png"), Width = 200, Height = 120 });
            IconTexts.Add(new IconTextModel() { Name = refineryCount.ToString(), Image = new(".\\Assets\\Icons\\Refinery.png"), Width = 200, Height = 120 });
        }

        private void SaveFileReader_OnProgressUpdate(object sender, float value)
        {
            Progress = (int)value;
            ProgressBarVisible = value < 100f;
        }

        private async void GetPatchNotes()
        {
            HttpClient client = new();
            var response = await client.PostAsync("https://questions.satisfactorygame.com/graphql",
                new StringContent(_patchNotesRequestContent, System.Text.Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                PatchNotes.Add(new PatchNoteModel() { Title = "Failed to load Patch Notes!" });
            }

            JsonNode? json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            if (json == null)
            {
                PatchNotes.Add(new PatchNoteModel() { Title = "Failed to load Patch Notes!" });
            }

            JsonArray array = (JsonArray)json["data"]["allPatchNotes"]["posts"];
            foreach (var item in array)
            {
                PatchNotes.Add(new PatchNoteModel()
                {
                    Id = (string)item["id"],
                    Title = (string)item["title"],
                    Content = RemoveHtmlTags(((string)item["contents"])[..300]) + "...",
                    DateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)item["creation_date"]).DateTime,
                    Version = (string)item["version_number"]
                });
            }

        }

        private static string RemoveHtmlTags(string text)
        {
            while (text.Contains('<'))
            {
                int startIndex = text.IndexOf('<');
                int endIndex = text.IndexOf('>') + 1;
                if (startIndex > endIndex)
                {
                    text = text[..text.IndexOf('<')];
                    break;
                }

                if (text[startIndex..endIndex] == "<br>")
                {
                    text = text.Insert(startIndex, "\n");
                    startIndex += "\n".Length;
                    endIndex += "\n".Length;
                }

                text = text.Remove(startIndex, endIndex - startIndex);
            }

            return text;
        }

        private static string GetNewestSavePath()
        {
            string? filePath = Environment.GetEnvironmentVariable("LocalAppdata");
            if (filePath == null) throw new ArgumentNullException("Failed to get Path to %Localappdata%!");

            filePath += "\\FactoryGame\\Saved\\SaveGames";

            foreach (var dir in Directory.GetDirectories(filePath))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.All(char.IsDigit))
                {
                    filePath = dir;
                    break;
                }
            }

            DateTime lastWrite = DateTime.MinValue;
            foreach (var file in Directory.GetFiles(filePath))
            {
                DateTime last = File.GetLastWriteTimeUtc(file);
                if (last > lastWrite)
                {
                    lastWrite = last;
                    filePath = file;
                }
            }

            return filePath;
        }

        public class IconTextModel
        {
            public required string Name { get; set; }
            public required Bitmap Image { get; set; }
            public required int Width { get; set; }
            public required int Height { get; set; }
        }
        public class PatchNoteModel
        {
            public string Id { get; set; } = string.Empty;
            public required string Title { get; set; }
            public string Content { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
            public string Version { get; set; } = string.Empty;
        }
    }
}
