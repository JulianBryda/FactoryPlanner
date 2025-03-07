using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader.Structure
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class IgnorePropertyAttribute : Attribute
    {
    }
}
