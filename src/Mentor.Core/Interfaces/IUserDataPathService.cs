namespace Mentor.Core.Interfaces;

/// <summary>
/// Service for managing user data paths in a consistent manner across the application.
/// </summary>
public interface IUserDataPathService
{
    /// <summary>
    /// Gets the base path for all user data.
    /// </summary>
    /// <returns>Base path (e.g., AppData/Local/Mentor)</returns>
    string GetBasePath();

    /// <summary>
    /// Gets the path for storing game rules.
    /// </summary>
    /// <param name="gameName">Name of the game (e.g., "warframe")</param>
    /// <returns>Path to game rules directory (e.g., AppData/Local/Mentor/Data/rules/warframe)</returns>
    string GetRulesPath(string gameName);

    /// <summary>
    /// Gets the path for storing saved analysis HTML files.
    /// </summary>
    /// <returns>Path to saved analysis directory (e.g., AppData/Local/Mentor/Saved Analysis)</returns>
    string GetSavedAnalysisPath();

    /// <summary>
    /// Ensures the specified directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">Path to ensure exists</param>
    void EnsureDirectoryExists(string path);
}

