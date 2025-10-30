// using System.Runtime.InteropServices;
// using WinRT.Interop;
// using System.Drawing;

// namespace Mentor.Uno.Platforms;

// public static class WindowsExtensions
// {
//     [System.Runtime.Versioning.SupportedOSPlatform("windows")]
//     public static void SetWindowIcon(this Microsoft.UI.Xaml.Window window)
//     {
//         // Only run on Windows at runtime
//         if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//         {
//             return;
//         }

//         try
//         {
//             // Get the window handle
//             var hwnd = WindowNative.GetWindowHandle(window);
//             if (hwnd == IntPtr.Zero)
//             {
//                 return;
//             }

//             // Get the icon file path relative to the app directory
//             var appPath = AppContext.BaseDirectory;
//             var iconPath = System.IO.Path.Combine(appPath, "Assets", "Icons", "icon.png");

//             if (System.IO.File.Exists(iconPath))
//             {
//                 // Load PNG and convert to icon using System.Drawing
//                 using (var bitmap = new Bitmap(iconPath))
//                 {
//                     // Create icon handles for small and large sizes
//                     var hIconSmall = bitmap.GetHicon();
//                     var hIconLarge = bitmap.GetHicon();

//                     // Set both small and large icons
//                     SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hIconSmall);
//                     SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, hIconLarge);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             System.Diagnostics.Debug.WriteLine($"Failed to set window icon: {ex.Message}");
//         }
//     }

//     #region Win32 P/Invoke declarations

//     private const int ICON_SMALL = 0;
//     private const int ICON_BIG = 1;
//     private const uint WM_SETICON = 0x0080;

//     [DllImport("user32.dll", CharSet = CharSet.Auto)]
//     private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

//     #endregion
// }
