using System.Runtime.InteropServices;

namespace Mentor.Uno.Platforms;

public static class MacOSExtensions
{
    public static void ConfigureMacOSMenu()
    {
        // Only run on macOS at runtime
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        try
        {
            // Use P/Invoke to set up macOS menu with Cmd-Q support
            ConfigureNativeMenu();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to configure macOS menu: {ex.Message}");
        }
    }

    private static void ConfigureNativeMenu()
    {
        // Get the shared NSApplication instance
        var app = objc_msgSend_retIntPtr(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
        
        if (app == IntPtr.Zero)
        {
            return;
        }

        // Create main menu
        var mainMenu = objc_msgSend_retIntPtr(objc_msgSend_retIntPtr(objc_getClass("NSMenu"), sel_registerName("alloc")), sel_registerName("init"));
        
        // Create app menu (first menu item)
        var appMenuItem = objc_msgSend_retIntPtr(objc_msgSend_retIntPtr(objc_getClass("NSMenuItem"), sel_registerName("alloc")), sel_registerName("init"));
        var appMenu = objc_msgSend_retIntPtr(objc_msgSend_retIntPtr(objc_getClass("NSMenu"), sel_registerName("alloc")), sel_registerName("init"));
        
        // Set submenu
        objc_msgSend_voidIntPtrIntPtr(appMenuItem, sel_registerName("setSubmenu:"), appMenu);
        
        // Add app menu item to main menu
        objc_msgSend_voidIntPtrIntPtr(mainMenu, sel_registerName("addItem:"), appMenuItem);
        
        // Create Quit menu item with Cmd-Q
        var quitString = CreateNSString("Quit Mentor");
        var qKey = CreateNSString("q");
        var quitMenuItem = objc_msgSend_retIntPtr(
            objc_msgSend_retIntPtr(objc_getClass("NSMenuItem"), sel_registerName("alloc")),
            sel_registerName("initWithTitle:action:keyEquivalent:"),
            quitString,
            sel_registerName("terminate:"),
            qKey
        );
        
        // Add quit menu item to app menu
        objc_msgSend_voidIntPtrIntPtr(appMenu, sel_registerName("addItem:"), quitMenuItem);
        
        // Set main menu
        objc_msgSend_voidIntPtrIntPtr(app, sel_registerName("setMainMenu:"), mainMenu);
    }

    private static IntPtr CreateNSString(string str)
    {
        var nsString = objc_getClass("NSString");
        var allocSelector = sel_registerName("alloc");
        var initSelector = sel_registerName("initWithUTF8String:");
        
        var ptr = objc_msgSend_retIntPtr(nsString, allocSelector);
        var utf8 = Marshal.StringToHGlobalAnsi(str);
        var result = objc_msgSend_retIntPtr(ptr, initSelector, utf8);
        Marshal.FreeHGlobal(utf8);
        
        return result;
    }

    #region P/Invoke declarations

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_retIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_retIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_retIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_voidIntPtrIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    #endregion
}
