using Avalonia.Controls.Generators;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FactoryPlanner.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int progress;
        [ObservableProperty]
        private bool progressBarVisible = true;
        public ObservableCollection<MyItem> Items { get; set; } = [];


        private readonly SaveFileReader _saveFileReader;

        public MainWindowViewModel()
        {
            string newestSavePath = GetNewestSavePath();

            _saveFileReader = new SaveFileReader(@"C:\Users\Julian\source\repos\FactoryPlanner\FileReader\1.0 BABY_autosave_2.sav");
            _saveFileReader.OnProgressUpdate += SaveFileReader_OnProgressUpdate;
            _saveFileReader.OnFinish += SaveFileReader_OnFinish;
        }

        private void SaveFileReader_OnFinish(object sender)
        {
            int smelter = _saveFileReader.CountActCompHeader(TypePaths.SmelterMk1);
            int assembler = _saveFileReader.CountActCompHeader(TypePaths.AssemblerMk1);
            int constructor = _saveFileReader.CountActCompHeader(TypePaths.ConstructorMk1);
            int refinery = _saveFileReader.CountActCompHeader(TypePaths.OilRefinery);

            Items.Add(new MyItem() { Name = smelter.ToString()});
            Items.Add(new MyItem() { Name = assembler.ToString()});
            Items.Add(new MyItem() { Name = constructor.ToString()});
            Items.Add(new MyItem() { Name = refinery.ToString()});
        }

        private void SaveFileReader_OnProgressUpdate(object sender, float value)
        {
            Progress = (int)value;
            ProgressBarVisible = value < 100f;
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

    }

    public class MyItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
