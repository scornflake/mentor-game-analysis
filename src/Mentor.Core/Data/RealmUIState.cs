using Realms;

namespace Mentor.Core.Data;

public partial class RealmUIState : IRealmObject
{
    [PrimaryKey]
    [MapTo("_id")]
    public string Id { get; set; } = "ui_state_singleton";
    
    public string? LastImagePath { get; set; }
    public string? LastPrompt { get; set; }
    public string? LastProvider { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

