using Realms;

namespace Mentor.Core.Data;

public partial class RealWebtoolToolConfiguration: IRealmObject
{
    [PrimaryKey]
    [MapTo("_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string ToolName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "";
    public int Timeout { get; set; } = 30;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}