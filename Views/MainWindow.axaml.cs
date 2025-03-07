using Avalonia.Controls;
using FactoryPlanner.FileReader;

namespace FactoryPlanner.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SaveFileReader reader = new("C:\\Users\\Julian\\source\\repos\\FactoryPlanner\\FileReader\\1.0 BABY_autosave_2.sav");

            string test = "";
        }
    }
}