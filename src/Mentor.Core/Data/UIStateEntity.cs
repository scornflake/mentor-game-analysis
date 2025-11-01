using LiteDB;

namespace Mentor.Core.Data;

public class UIStateEntity
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    
    public string Name { get; init; } = "default";
    public string? LastImagePath { get; set; }
    public string? LastPrompt { get; set; }
    public string? LastProvider { get; set; }
    public string? LastGameName { get; set; }
    public bool SaveAnalysisAutomatically { get; set; } = false;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

