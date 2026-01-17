using System.Text.Json;
using System.Text.Json.Serialization;
using ProgramX.Azure.FunctionApp.Osm.Model;
using ProgramX.Azure.FunctionApp.Osm.Model.Osm.Responses;

namespace ProgramX.Azure.FunctionApp.Osm.JsonConverters;

/// <summary>
/// When getting meeting/evenings, the primary_leader property may be a <see cref="Leader"/> object or <c>false</c>.
/// This converter handles both cases when applied to an effected property.
/// </summary>
public class BooleanOrLeaderJsonPropertyConverter : JsonConverter<Leader?>
{
    public override Leader? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.False)
        {
            reader.GetBoolean(); // consume the value to advance the reader
            return null;
        }
        
        if (reader.TokenType == JsonTokenType.Null) return null;
        
        // deserialize the complex object
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Use the serializer to handle the complex type automatically
            return JsonSerializer.Deserialize<Leader>(ref reader, options);
        }
        
        throw new NotSupportedException($"Cannot convert {reader.TokenType} to {nameof(Leader)}");
    }

    public override void Write(Utf8JsonWriter writer, Leader? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteBooleanValue(false);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}