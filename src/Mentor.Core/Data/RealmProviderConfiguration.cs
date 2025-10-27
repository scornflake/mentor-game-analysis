using Realms;

namespace Mentor.Core.Data;

public partial class RealmProviderConfiguration : IRealmObject
{
    [PrimaryKey]
    [MapTo("_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60;
    public bool IsActive { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

