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
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
        private static readonly List<RessourceNode> s_nodes = JsonSerializer.Deserialize<List<RessourceNode>>(File.ReadAllText(".\\Assets\\nodes.json")) ?? throw new Exception("Could not deserialize nodes.json!");

        public static string GetIconPath(int typePathHash)
        {
            return s_iconPaths.GetValueOrDefault(typePathHash, ".\\Assets\\Missing.png");
        }

        public static Bitmap GetItemIcon(string itemName)
        {
            string path = $".\\Assets\\Icons\\Items\\{itemName}.png";
            if (!File.Exists(path)) path = ".\\Assets\\Missing.png";

            return new Bitmap(path);
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

        public static RessourceNode? GetRessourceNode(string pathName)
        {
            return s_nodes.Find(o => o.NodePathName == pathName);
        }
    }
}
