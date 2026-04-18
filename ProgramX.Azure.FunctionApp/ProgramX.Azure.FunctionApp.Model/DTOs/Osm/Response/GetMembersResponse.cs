using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response;

public class GetMembersResponse
{
    [JsonPropertyName("items")]
    public List<MemberDto> Items { get; set; }
}