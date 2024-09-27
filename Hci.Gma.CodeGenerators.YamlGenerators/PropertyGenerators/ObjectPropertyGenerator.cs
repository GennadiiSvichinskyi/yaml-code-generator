using System;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
public class ObjectPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        return nameof(Object);
    }
}
