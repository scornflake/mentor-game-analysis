using Mentor.Core.Data;
using Mentor.Core.Interfaces;
using Microsoft.UI.Xaml;

namespace Mentor.Uno.Helpers;

public class WindowStateHelper
{
    private const double DefaultWidth = 1280;
    private const double DefaultHeight = 900;
    private const double MinWidth = 800;
    private const double MinHeight = 600;
    private readonly ILogger<WindowStateHelper> _logger;

    public WindowStateHelper(ILogger<WindowStateHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Restores the window position and size from saved state
    /// </summary>
    public async Task RestoreWindowStateAsync(Window window, string windowName, IConfigurationRepository repository, double? defaultWidth = null, double? defaultHeight = null)
    {
        try
        {
            var state = await repository.GetWindowStateAsync(windowName);
            // _logger.LogInformation("Restoring window state for {WindowName}", windowName);
            if (state != null)
            {
                var appWindow = window.AppWindow;
                if (appWindow != null)
                {
                    // Validate dimensions
                    var width = Math.Max(state.Width, MinWidth);
                    var height = Math.Max(state.Height, MinHeight);

                    // Resize window
                    appWindow.Resize(new Windows.Graphics.SizeInt32
                    {
                        Width = (int)width,
                        Height = (int)height
                    });

                    // Move window (if valid position)
                    if (state.X >= 0 && state.Y >= 0)
                    {
                        appWindow.Move(new Windows.Graphics.PointInt32
                        {
                            X = (int)state.X,
                            Y = (int)state.Y
                        });
                    }
                    
                    _logger.LogInformation("Restored window '{WindowName}' to position ({X}, {Y}) and size ({Width}x{Height})", windowName, state.X, state.Y, width, height);
                }
            }
            else
            {
                // Set default size for new windows
                var appWindow = window.AppWindow;
                if (appWindow != null)
                {
                    var width = defaultWidth ?? DefaultWidth;
                    var height = defaultHeight ?? DefaultHeight;
                    
                    appWindow.Resize(new Windows.Graphics.SizeInt32
                    {
                        Width = (int)width,
                        Height = (int)height
                    });
                }
            }
        }
        catch (Exception)
        {
            // Silently fail - window state restoration is not critical
        }
    }

    /// <summary>
    /// Saves the window position and size
    /// </summary>
    public async Task SaveWindowStateAsync(Window window, string windowName, IConfigurationRepository repository)
    {
        try
        {
            var appWindow = window.AppWindow;
            if (appWindow != null)
            {
                var position = appWindow.Position;
                var size = appWindow.Size;

                var state = new WindowStateEntity
                {
                    WindowName = windowName,
                    X = position.X,
                    Y = position.Y,
                    Width = size.Width,
                    Height = size.Height
                };

                await repository.SaveWindowStateAsync(state);
            }
        }
        catch (Exception)
        {
            // Silently fail - saving window state is not critical
        }
    }

    /// <summary>
    /// Sets up automatic window state tracking (saves on position/size changes)
    /// </summary>
    public void SetupWindowStateTracking(Window window, string windowName, IConfigurationRepository repository)
    {
        var appWindow = window.AppWindow;
        if (appWindow == null) return;

        // Track position changes
        appWindow.Changed += async (sender, args) =>
        {
            if (args.DidPositionChange || args.DidSizeChange)
            {
                await SaveWindowStateAsync(window, windowName, repository);
            }
        };
    }
}

