using LiteDB;

namespace Mentor.Core.Data;

public class WindowStateEntity
{
    [BsonId]
    public string WindowName { get; set; } = string.Empty;
    
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

