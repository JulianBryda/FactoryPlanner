using FactoryPlanner.FileReader.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Assets
{
    public static class AssetManager
    {
        private static readonly Dictionary<int, string> paths = new Dictionary<int, string>
        {
            [TypePaths.AssemblerMk1] = ".\\Assets\\Icons\\Assembler.png",
            [TypePaths.ResourceSinkShop] = ".\\Assets\\Icons\\AWESOME_Shop.png",
            [TypePaths.ResourceSink] = ".\\Assets\\Icons\\AWESOME_Sink.png",
            [TypePaths.ConstructorMk1] = ".\\Assets\\Icons\\Constructor.png",
            [TypePaths.SmelterMk1] = ".\\Assets\\Icons\\Smelter.png"
        };


        public static string GetAssetPath(int typePathHash)
        {
            return paths.GetValueOrDefault(typePathHash, "");
        }
    }
}
