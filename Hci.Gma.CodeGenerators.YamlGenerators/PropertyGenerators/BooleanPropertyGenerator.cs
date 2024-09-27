using System;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
public class BooleanPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        return nameof(Boolean);
    }
}
