using Hci.Gma.CodeGenerators.YamlGenerators.Extensions;
using Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators
{
    [Generator]
    public class YamlDotNetGenerator : IIncrementalGenerator
    {
        private IPropertyGeneratorProvider _propertyGeneratorProvider;

        public YamlDotNetGenerator()
        {
            _propertyGeneratorProvider = new PropertyGeneratorProvider();
        }
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var yamlFiles = context
                .AdditionalTextsProvider
                .Where(static (file) => file.Path.EndsWith(".yaml"));

            IncrementalValueProvider<string?> projectDirProvider = context.AnalyzerConfigOptionsProvider
                .Select(static (provider, _) => { 
                    provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? rootNamespace);

                    return rootNamespace;
                });

            IncrementalValuesProvider<(AdditionalText, string?)> combined = yamlFiles.Combine(projectDirProvider);
            // read their contents and save their name
            IncrementalValuesProvider<(string name, YamlMappingNode content, string? dir)> namesAndContents = combined.Select((yaml, cancellationToken) =>
                (name: Path.GetFileNameWithoutExtension(yaml.Item1.Path), content: GetYamlContent(yaml.Item1.GetText(cancellationToken)), dir: yaml.Item2));
           
            // generate a class that contains their values as const strings
            context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
            {
                var name = nameAndContent.name;
                var mapping = nameAndContent.content;
                var dir = nameAndContent.dir;
                
                foreach (var entry in mapping.Children)
                {
                    var componentsName = ((YamlScalarNode)entry.Key).Value;
                    if (componentsName != Constants.NodeNames.Components) continue;
                    var node = mapping.Children[componentsName];
                    foreach (var yamlNode in  ((YamlMappingNode)node).Children)
                    {
                        var nodeName = ((YamlScalarNode)yamlNode.Key).Value;
                        if (nodeName == Constants.NodeNames.Schemas)
                        {
                            spc.AddSource($@"Dtos_{name}.g.cs", CreateRecordDefinitions((YamlMappingNode)yamlNode.Value, dir));
                        }

                        if (nodeName == Constants.NodeNames.Messages)
                        {
                            spc.AddSource($@"Messages_{name}.g.cs", CreateMessageDefinitions((YamlMappingNode)yamlNode.Value, dir));
                        }
                    }
                   
                }
            });
        }

        private YamlMappingNode GetYamlContent(SourceText? yamlText)
        {
            if (yamlText is not null)
            {
                // Setup the input
                var input = new StringReader(yamlText.ToString());
                // Load the stream
                var yaml = new YamlStream();
                yaml.Load(input);

                return (YamlMappingNode)yaml.Documents[0].RootNode;
            }

            return new YamlMappingNode();
        }

        private string CreateRecordDefinitions(YamlMappingNode schemasValue, string? dir = "Generated")
        {
            var result = new StringBuilder();
            result.Append($@"
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
#nullable enable
namespace {dir}.Dtos
{{");
            foreach (var schema in schemasValue)
            {
                var node = schema.Value as YamlMappingNode;
                if (node is not null && node.Any(x => x.Key.ToString() == Constants.NodeNames.Enum))
                {
                    result.Append(GenerateEnum(node, schema.Key));
                }
                else if (node is not null
                         && node.Any(x => x.Key.ToString() == Constants.NodeNames.Type
                                          && ((YamlScalarNode)node[Constants.NodeNames.Type]).Value == "array"))
                {
                    result.Append(GenerateArray(node, schema.Key,  GenerateAdditionalProperties));
                }
                else
                {
                    result.Append(GenerateDto(schema, GenerateAdditionalProperties));
                }                
            }
            result.Append($@"
}}");
            result.AppendLine();
            return result.ToString();
        }

        private string CreateMessageDefinitions(YamlMappingNode schemasValue, string? dir = "Generated")
        {
            var result = new StringBuilder();
            result.Append($@"
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
#nullable enable
namespace {dir}.Dtos
{{");
            foreach (var schema in schemasValue)
            {
                var node = schema.Value as YamlMappingNode;
              
                if (node is not null
                         && node.Any(x => x.Key.ToString() == Constants.NodeNames.Type
                                          && ((YamlScalarNode)node[Constants.NodeNames.Type]).Value == "array"))
                {
                    result.Append(GenerateArray(node, schema.Key,  GenerateAdditionalProperties));
                }
                else
                {
                    result.Append(GenerateMessage(schema, GenerateAdditionalProperties));
                }                
            }
            result.Append($@"
}}");
            result.AppendLine();
            return result.ToString();
        }

        private static string GenerateAdditionalProperties(YamlMappingNode? node)
        {
            var result = new StringBuilder();
            if (node is not null && node.Any(x => x.Key.ToString() == Constants.NodeNames.AdditionalProperties))
            {
                if (node[Constants.NodeNames.AdditionalProperties] is YamlScalarNode { Value: "true" })
                {
                    result.Append($@"
        private IDictionary<{nameof(String)}, {nameof(Object)}> _additionalProperties = new Dictionary<{nameof(String)}, {nameof(Object)}>();
        
        public IDictionary<{nameof(String)}, {nameof(Object)}> AdditionalProperties
        {{
            get {{ return _additionalProperties; }}
            set {{ _additionalProperties = value; }}
        }}");
                }
            }

            return result.ToString();
        }

        private string GenerateArray(YamlMappingNode node, YamlNode key, Func<YamlMappingNode?, string> postGenerator)
        {
            var result = new StringBuilder();
            var typeNode = (YamlScalarNode)node[Constants.NodeNames.Type];
            if (typeNode.Value == "array")
            {
                result.Append($@"
    public class {key}
    {{");
                var generator = _propertyGeneratorProvider.GetPropertyGenerator(typeNode.Value);
                var type = generator.GetType(node);
                result.Append($@"
        public {type.CheckNullableType(node)} Items {{ get; set; }}");
                result.Append(postGenerator(node));
                result.Append($@"
    }}");
            }

            return result.ToString();
        }

        private string GenerateEnum(YamlMappingNode node, YamlNode key)
        {
            
            var result = new StringBuilder();
            var enumNode = node[Constants.NodeNames.Enum];
            result.Append($@"
    public enum {key}
    {{");
            foreach (var item in ((YamlSequenceNode)enumNode).Children)
            {
                var camelCaseKey = item.ToString().ToCamelCase();
                result.Append($@"
        {camelCaseKey},
            ");
            }           
            result.Append($@"
    }}");

            return result.ToString();
        }

        private string GenerateMessage(KeyValuePair<YamlNode, YamlNode> schema, Func<YamlMappingNode?, string> postGenerator)
        {
            var result = new StringBuilder();
            var dtoContent = schema.Value as YamlMappingNode;
            if (dtoContent != null &&
                dtoContent.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
            {
                // ignore referenced objects as they have to be in the separated *.shared.yaml file
                return result.ToString();
            }
            var (inheritance, allOfProperties, additionalProperties) = AddInheritance(dtoContent);
            result.Append($@"
    public partial class {schema.Key} {inheritance}
    {{");
            if (dtoContent != null && 
                dtoContent.Any(x => x.Key.ToString() == Constants.NodeNames.Properties))
            {
                var properties = dtoContent[Constants.NodeNames.Properties];
                GenerateProperties(result, properties, allOfProperties);                
            }
            if (allOfProperties is not null)
            {
                GenerateProperties(result, new YamlMappingNode(), allOfProperties);                
            }
            if (additionalProperties is not null)
            {
                result.Append(additionalProperties);            
            }
            result.Append(postGenerator(dtoContent));
            result.Append($@"
    }}");
            result.AppendLine();
            return result.ToString();
        }
        // TODO: enums
        private string GenerateDto(KeyValuePair<YamlNode, YamlNode> schema, Func<YamlMappingNode?, string> postGenerator)
        {
            var result = new StringBuilder();
            var dtoContent = schema.Value as YamlMappingNode;
            if (dtoContent != null &&
                dtoContent.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
            {
                // ignore referenced objects as they have to be in the separated *.shared.yaml file
                return result.ToString();
            }
            var (inheritance, allOfProperties, additionalProperties) = AddInheritance(dtoContent);
            result.Append($@"
    public class {schema.Key} {inheritance}
    {{");
            if (dtoContent != null && 
                dtoContent.Any(x => x.Key.ToString() == Constants.NodeNames.Properties))
            {
                var properties = dtoContent[Constants.NodeNames.Properties];
                GenerateProperties(result, properties, allOfProperties);                
            }
            if (allOfProperties is not null)
            {
                GenerateProperties(result, new YamlMappingNode(), allOfProperties);                
            }
            if (additionalProperties is not null)
            {
                result.Append(additionalProperties);            
            }
            result.Append(postGenerator(dtoContent));
            result.Append($@"
    }}");
            result.AppendLine();
            return result.ToString();
        }

        private void GenerateProperties(StringBuilder result, YamlNode properties, YamlNode? allOfProperties)
        {
            foreach (var property in ((YamlMappingNode)properties).Children)
            {
                GenerateProperty(result, property.Key, property.Value);
            }

            if (allOfProperties == null) return;

            {
                foreach (var property in (YamlMappingNode)allOfProperties)
                {
                    GenerateProperty(result, property.Key, property.Value);
                }
            }
        }

        private void GenerateProperty(StringBuilder result, YamlNode propertyKey, YamlNode propertyValue)
        {
            if (propertyValue is YamlMappingNode mappingNode 
                && mappingNode.Any(x => x.Key.ToString() == Constants.NodeNames.Type))
            {
                var typeNode = (YamlScalarNode)mappingNode[Constants.NodeNames.Type];
                if (typeNode.Value != null)
                {
                    _propertyGeneratorProvider = new PropertyGeneratorProvider();
                    var generator = _propertyGeneratorProvider.GetPropertyGenerator(typeNode.Value);
                    var type = generator.GetType(mappingNode);                   
                    var camelCaseKey = propertyKey.ToString().ToCamelCase();
                    result.Append($@"
        [JsonProperty(""{propertyKey}"")]
        public {type.CheckNullableType(mappingNode)} {camelCaseKey} {{ get; set; }} {CheckDefault(mappingNode)}");
                    result.AppendLine();
                }
            }
            else if (propertyValue is YamlMappingNode mappingNodeAllOf 
                     && mappingNodeAllOf.Any(x => x.Key.ToString() == Constants.NodeNames.AllOf))
            {              
                var allOfNode =  (YamlSequenceNode)mappingNodeAllOf[Constants.NodeNames.AllOf];
                  foreach (var allOfChild in allOfNode.Children)
                {
                    if (allOfChild is YamlMappingNode allOfChildMapping &&
                        allOfChildMapping.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
                    {
                        var refNode = (YamlScalarNode)allOfChildMapping[Constants.NodeNames.Reference];
                        var type = refNode.Value?.Substring(refNode.Value.LastIndexOf('/') + 1); 
                        if( type is not null ) type = type.CheckNullableType(mappingNodeAllOf);
                        var camelCaseKey = propertyKey.ToString().ToCamelCase();
                        result.Append($@"
        [JsonProperty(""{propertyKey}"")]                      
        public {type} {camelCaseKey} {{ get; set; }} {CheckDefault(mappingNodeAllOf)}");
                        result.AppendLine();
                    }                  
                }                      
            }
            else
            {
                if (propertyValue is YamlMappingNode mappingNodeRef 
                    && mappingNodeRef.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
                {
                    var refNode =  (YamlScalarNode)mappingNodeRef[Constants.NodeNames.Reference];
                    var type = refNode.Value?.Substring( refNode.Value.LastIndexOf('/') + 1);
                    if( type is not null ) type = type.CheckNullableType(mappingNodeRef);
                    var camelCaseKey = propertyKey.ToString().ToCamelCase();
                    result.Append($@"
        [JsonProperty(""{propertyKey}"")]                   
        public {type} {camelCaseKey} {{ get; set; }} {CheckDefault(mappingNodeRef)}");
                    result.AppendLine();
                }
            }
        }

        private string? CheckDefault(YamlMappingNode mappingNode, string? enumType = null, bool isEnum = false)
        {
           if (mappingNode.Any(x => x.Key.ToString() == Constants.NodeNames.Default))
           {
               var defaultNode = (YamlScalarNode)mappingNode[Constants.NodeNames.Default];
               if (isEnum && defaultNode.Value != null)
               {
                   var camelCaseDefault = defaultNode.ToString().ToCamelCase();
                   return $" = \"{enumType}.{camelCaseDefault}\";";
               }

               return $" = {defaultNode.Value};";
           }

           return null;
        }

        private static (string?, YamlNode?, string?) AddInheritance(YamlMappingNode? dtoContentNode)
        {
            string? inheritance = null;
            string? additionalProperties = null;
            YamlNode? allOfProperties = null;
            if (dtoContentNode == null) return (inheritance, allOfProperties, additionalProperties);
            if (dtoContentNode.Any(x => x.Key.ToString() == Constants.NodeNames.AllOf))
            {
                if (dtoContentNode[Constants.NodeNames.AllOf] is not YamlSequenceNode allOfNode)
                    return (inheritance, allOfProperties, additionalProperties);
                foreach (var allOfChild in allOfNode.Children)
                {
                    var allOfChildMapping = allOfChild as YamlMappingNode;
                    if (allOfChildMapping is not null &&
                        allOfChildMapping.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
                    {
                        var refNode = (YamlScalarNode)allOfChildMapping[Constants.NodeNames.Reference];
                        var typeParam = refNode.Value?.Substring(refNode.Value.LastIndexOf('/') + 1);
                        inheritance = $": {typeParam}";
                    }
                    if (allOfChildMapping is not null && 
                        allOfChildMapping.Any(x => x.Key.ToString() == Constants.NodeNames.Properties))
                    {
                        allOfProperties = allOfChildMapping[Constants.NodeNames.Properties];
                    }
                    if (allOfChildMapping is not null && 
                        allOfChildMapping.Any(x => x.Key.ToString() == Constants.NodeNames.AdditionalProperties))
                    {
                        additionalProperties = GenerateAdditionalProperties(allOfChildMapping);
                    }
                }
            }
            else if (dtoContentNode.Any(x => x.Key.ToString() == Constants.NodeNames.AdditionalProperties))
            {
                if (dtoContentNode[Constants.NodeNames.AdditionalProperties] is YamlMappingNode additionalPropertiesNode)
                {
                    inheritance = GenerateAdditionalPropertiesInheritance(additionalPropertiesNode);
                }
            }

            return (inheritance, allOfProperties, additionalProperties);
        }

        private static string GenerateAdditionalPropertiesInheritance( YamlMappingNode additionalPropertiesNode)
        {
            if (additionalPropertiesNode.All(x => x.Key.ToString() != Constants.NodeNames.Type))
            {
                return $": Dictionary<{nameof(String)}, {nameof(String)}>";
            }
            var typeAdditionalProperties = (YamlScalarNode?)additionalPropertiesNode[Constants.NodeNames.Type];
            IPropertyGenerator generator = typeAdditionalProperties?.Value switch
            {
                "string" => new StringPropertyGenerator(),
                "integer" => new IntegerPropertyGenerator(),
                "number" => new NumberPropertyGenerator(),
                "boolean" => new BooleanPropertyGenerator(),
                "object" => new ObjectPropertyGenerator(),
                "array" => new ArrayPropertyGenerator(),
                _ => new ObjectPropertyGenerator()
            };
            return typeAdditionalProperties == null
                ? $": Dictionary<{nameof(String)}, {nameof(String)}>"
                : $": Dictionary<{nameof(String)}, {generator.GetType(additionalPropertiesNode)}>";
        }
    }
}
