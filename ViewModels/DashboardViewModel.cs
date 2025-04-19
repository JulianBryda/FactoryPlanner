using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text.Json.Nodes;

namespace FactoryPlanner.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private int _progress;
        public int Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        private bool _progressBarVisible = true;
        public bool ProgressBarVisible
        {
            get => _progressBarVisible;
            set => this.RaiseAndSetIfChanged(ref _progressBarVisible, value);
        }

        private bool _stablePatchNotes = true;
        public bool StablePatchNotes
        {
            get => _stablePatchNotes;
            set => this.RaiseAndSetIfChanged(ref _stablePatchNotes, value);
        }

        private List<PatchNoteModel> _patchNotes = [];
        public List<PatchNoteModel> PatchNotes
        {
            get => _patchNotes;
            set => this.RaiseAndSetIfChanged(ref _patchNotes, value);
        }

        public ObservableCollection<IconTextModel> IconTexts { get; set; } = [];
        public ReactiveCommand<Unit, Unit> PatchNoteFilterCommand { get; }


        private readonly List<PatchNoteModel> _fullPatchNotes = [];
        private readonly SaveFileReader _saveFileReader;
        private readonly string _patchNotesRequestContent = "{\"query\":\"query($date_range: Float, $search: String, $category: String, $sort: String, $status: String, $version_number: String, $answered: String!, $showHidden: Boolean! $skip: Int!, $limit: Int!) {\\n              allPatchNotes(date_range: $date_range, search: $search, category: $category, sort: $sort, status: $status, version_number: $version_number, answered: $answered, showHidden: $showHidden, skip: $skip, limit: $limit) {\\n                posts{\\n                  id\\n                  title\\n                  upvotes\\n                  categories\\n                  contents\\n                  creation_date\\n                  status\\n                  version_number\\n                  author {\\n                      username\\n                      role\\n                  }\\n                  comments{\\n                      id\\n                  }\\n                  isPinned\\n                  pinnedDate\\n                  countComments\\n                  answered\\n                  admin_data{\\n                    in_progress\\n                  }\\n                  hidden\\n                }\\n                totalDocs\\n                totalUpvotes\\n                totalComments\\n                totalPages\\n                limit\\n                hasPrevPage\\n                hasNextPage\\n              }\\n          }\\n        \",\"variables\":{\"date_range\":9999,\"search\":\"\",\"category\":\"\",\"sort\":\"upvotes-asc\",\"status\":\"\",\"answered\":\"all\",\"showHidden\":false,\"skip\":0,\"limit\":20},\"opName\":\"allPatchNotes\"}";


        public DashboardViewModel(IScreen screen) : base(screen)
        {
            _saveFileReader = new SaveFileReader();
            _saveFileReader.OnProgressUpdate += SaveFileReader_OnProgressUpdate;
            _saveFileReader.OnFinish += SaveFileReader_OnFinish;

            PatchNoteFilterCommand = ReactiveCommand.Create(() =>
            {
                PatchNotes = [.. _fullPatchNotes.Where(o => o.IsStable == StablePatchNotes)];
            });

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
                PatchNotes = [new PatchNoteModel() { Title = "Failed to load Patch Notes!" }];
                return;
            }

            JsonNode? json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            if (json == null)
            {
                PatchNotes = [new PatchNoteModel() { Title = "Failed to load Patch Notes!" }];
                return;
            }

            JsonArray array = (JsonArray)json["data"]["allPatchNotes"]["posts"];
            foreach (var item in array)
            {
                _fullPatchNotes.Add(new PatchNoteModel()
                {
                    Id = (string)item["id"],
                    Title = (string)item["title"],
                    Content = SetHtmlColor((string)item["contents"], "#ddd"),
                    DateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)item["creation_date"]).DateTime,
                    Version = (string)item["version_number"],
                    IsStable = !((string)item["version_number"]).Contains("Experimental:")
                });
            }
            PatchNotes = [.. _fullPatchNotes.Where(o => o.IsStable == StablePatchNotes)];

        }

        private string SetHtmlColor(string text, string color)
        {
            return $"<div style=\"color: {color};\">{text}</div>";
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
            public bool IsStable { get; set; } = false;
        }
    }
}
