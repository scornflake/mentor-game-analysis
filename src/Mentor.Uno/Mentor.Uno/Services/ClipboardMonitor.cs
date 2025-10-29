using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace Mentor.Uno.Services;

/// <summary>
/// Monitors the clipboard for image changes and notifies when a new image is detected.
/// </summary>
public class ClipboardMonitor : IDisposable
{
    private readonly ILogger<ClipboardMonitor> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;
    private string? _lastClipboardImageId;
    private bool _disposed = false;

    public event EventHandler<ClipboardImageEventArgs>? ImageDetected;

    public ClipboardMonitor(ILogger<ClipboardMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts monitoring the clipboard for image changes.
    /// </summary>
    public void StartMonitoring()
    {
        if (_monitoringTask != null && !_monitoringTask.IsCompleted)
        {
            _logger.LogWarning("Clipboard monitoring is already running");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = MonitorClipboardAsync(_cancellationTokenSource.Token);
        _logger.LogInformation("Clipboard monitoring started");
    }

    /// <summary>
    /// Stops monitoring the clipboard.
    /// </summary>
    public void StopMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        _monitoringTask?.Wait(TimeSpan.FromSeconds(1));
        _logger.LogInformation("Clipboard monitoring stopped");
    }

    private async Task MonitorClipboardAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, cancellationToken); // Check every 500ms

                var clipboardContent = Clipboard.GetContent();
                if (clipboardContent == null)
                {
                    continue;
                }

                // Check if clipboard contains an image
                if (!clipboardContent.Contains(StandardDataFormats.Bitmap))
                {
                    continue;
                }

                // Get a unique identifier for this clipboard image
                // We'll use a combination of timestamp and random data to detect changes
                var imageId = await GetClipboardImageIdAsync(clipboardContent);
                
                // Only process if this is a different image
                if (imageId == _lastClipboardImageId)
                {
                    continue;
                }

                _lastClipboardImageId = imageId;

                // Extract the image data
                var imageData = await ExtractImageFromClipboardAsync(clipboardContent);
                if (imageData != null && imageData.Length > 0)
                {
                    _logger.LogInformation("New image detected in clipboard, size: {Size} bytes", imageData.Length);
                    OnImageDetected(new ClipboardImageEventArgs(imageData));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring clipboard");
            }
        }
    }

    private async Task<string?> GetClipboardImageIdAsync(DataPackageView clipboardContent)
    {
        try
        {
            // Calculate a hash of the image data to detect changes
            // Read a sample of the image data to generate an ID
            var bitmapRef = await clipboardContent.GetBitmapAsync();
            if (bitmapRef != null)
            {
                using var stream = await bitmapRef.OpenReadAsync();
                if (stream != null && stream.Size > 0)
                {
                    // Read first 1024 bytes to create a hash-based ID
                    const int sampleSize = 1024;
                    var readSize = Math.Min((int)stream.Size, sampleSize);
                    var buffer = new byte[readSize];
                    
                    using var inputStream = stream.AsStreamForRead();
                    var bytesRead = await inputStream.ReadAsync(buffer, 0, readSize);
                    if (bytesRead > 0)
                    {
                        // Only use the bytes actually read
                        if (bytesRead < readSize)
                        {
                            var actualBuffer = new byte[bytesRead];
                            Array.Copy(buffer, actualBuffer, bytesRead);
                            buffer = actualBuffer;
                        }
                        // Create a simple hash from the first bytes
                        return Convert.ToHexString(buffer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get clipboard image ID");
        }

        // Fallback: use timestamp
        return DateTime.UtcNow.Ticks.ToString();
    }

    private async Task<byte[]?> ExtractImageFromClipboardAsync(DataPackageView clipboardContent)
    {
        try
        {
            // Get the bitmap reference
            var bitmapRef = await clipboardContent.GetBitmapAsync();
            if (bitmapRef == null)
            {
                return null;
            }

            // Open the stream and read the image data
            using var stream = await bitmapRef.OpenReadAsync();
            if (stream == null || stream.Size == 0)
            {
                return null;
            }

            // Read all bytes from the stream using .NET Stream API
            using var inputStream = stream.AsStreamForRead();
            var buffer = new List<byte>();
            var tempBuffer = new byte[4096];
            
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(tempBuffer, 0, tempBuffer.Length)) > 0)
            {
                var actualBytes = new byte[bytesRead];
                Array.Copy(tempBuffer, actualBytes, bytesRead);
                buffer.AddRange(actualBytes);
            }
            
            return buffer.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image from clipboard");
            return null;
        }
    }

    protected virtual void OnImageDetected(ClipboardImageEventArgs e)
    {
        ImageDetected?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopMonitoring();
        _semaphore.Dispose();
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Event arguments for clipboard image detection events.
/// </summary>
public class ClipboardImageEventArgs : EventArgs
{
    public byte[] ImageData { get; }

    public ClipboardImageEventArgs(byte[] imageData)
    {
        ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
    }
}

