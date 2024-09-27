using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.Extensions;
public static class StringExtensions
{
    public static string CheckNullableType(this string type, YamlMappingNode mappingNode)
    {
        if (mappingNode.Any(x => x.Key.ToString() == Constants.NodeNames.Nullable))
        {
            var nullable = (YamlScalarNode?)mappingNode[Constants.NodeNames.Nullable];
            if (nullable is not null && string.Equals(nullable.Value, true.ToString(), System.StringComparison.CurrentCultureIgnoreCase))
            {
                type = $"{type}?";
            }
        }

        return type;
    }

    public static string ToCamelCase(this string str)
    {
        str = char.ToUpperInvariant(str[0]) + str.Substring(1);
        while (str.IndexOf('_') > 0)
        {
            var underscore = str.IndexOf('_');
            str = str.Substring(0, underscore) + char.ToUpperInvariant(str[underscore + 1]) + str.Substring(underscore + 2);
        }

        str.Replace("_", "");

        return str;
    }
}
