using Avalonia.Media.Imaging;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FactoryPlanner.Assets
{
    public static class AssetManager
    {
        private static readonly Dictionary<int, string> s_iconPaths = new()
        {
            [TypePaths.AssemblerMk1] = ".\\Assets\\Icons\\Assembler.png",
            [TypePaths.ResourceSinkShop] = ".\\Assets\\Icons\\AWESOME_Shop.png",
            [TypePaths.ResourceSink] = ".\\Assets\\Icons\\AWESOME_Sink.png",
            [TypePaths.ConstructorMk1] = ".\\Assets\\Icons\\Constructor.png",
            [TypePaths.SmelterMk1] = ".\\Assets\\Icons\\Smelter.png",
        };

        private static readonly JsonDocument s_recipes = JsonDocument.Parse(File.ReadAllText(".\\Assets\\recipes.json"));

        public static string GetIconPath(int typePathHash)
        {
            return s_iconPaths.GetValueOrDefault(typePathHash, ".\\Assets\\Missing.png");
        }

        public static Bitmap GetIcon(int typePathHash)
        {
            string path = GetIconPath(typePathHash);
            return new Bitmap(path);
        }

        private static readonly JsonSerializerOptions s_serializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };
        public static Recipe? GetRecipe(string pathName)
        {
            JsonElement root = s_recipes.RootElement.GetProperty("recipes");
            if (!root.TryGetProperty(pathName[(pathName.LastIndexOf('.') + 1)..], out var element)) return null;

            return element.Deserialize<Recipe>(s_serializeOptions);
        }
    }
}
