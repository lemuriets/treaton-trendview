using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace LogDecoder.CAN.Protocol.Definitions;

/// <summary>
/// Reads a value-message entry written either as a scalar shorthand
/// (<c>0: "text"</c> => status Ok) or as a mapping (<c>{ message, status }</c>).
/// </summary>
public sealed class ValueMessageConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(ValueMessageDefinition);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is Scalar scalar)
        {
            parser.MoveNext();
            return new ValueMessageDefinition { Message = scalar.Value, Status = PackageTechStatus.Info };
        }

        parser.Consume<MappingStart>();
        string? message = null;
        var status = PackageTechStatus.Info;
        while (parser.Current is not MappingEnd)
        {
            var key = parser.Consume<Scalar>().Value;
            var value = parser.Consume<Scalar>().Value;
            switch (key)
            {
                case "message":
                    message = value;
                    break;
                case "status":
                    status = Enum.Parse<PackageTechStatus>(value, ignoreCase: true);
                    break;
            }
        }
        parser.Consume<MappingEnd>();
        return new ValueMessageDefinition { Message = message, Status = status };
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        throw new NotSupportedException();
    }
}
