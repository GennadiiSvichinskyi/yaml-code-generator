using System;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
internal class BooleanPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        return nameof(Boolean);
    }
}
