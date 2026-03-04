using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AppSwitcher;

internal class AutoStart(ILogger<AutoStart> logger)
{
    public bool IsEnabled()
    {
        var linkPath = GetLinkPath();
        return linkPath is not null && File.Exists(linkPath);
    }

    public bool RemoveShortcut()
    {
        var linkPath = GetLinkPath();
        if (linkPath is null)
        {
            logger.LogError("Cannot determine shortcut path.");
            return false;
        }

        try
        {
            if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
            }

            logger.LogInformation("Shortcut removed: {LinkPath}", linkPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove shortcut: {LinkPath}", linkPath);
            return false;
        }
    }

    public bool CreateShortcut()
    {
        var executablePath = Environment.ProcessPath;
        if (executablePath is null)
        {
            logger.LogError("Cannot get current process executable path.");
            return false;
        }
        var originalFileDirectory = Path.GetDirectoryName(executablePath);

        if (string.IsNullOrEmpty(originalFileDirectory))
        {
            logger.LogError("Cannot determine working directory from executable path.");
            return false;
        }

        var linkPath = GetLinkPath();
        if (linkPath is null)
        {
            logger.LogError("Cannot get current process executable path.");
            return false;
        }

        try
        {
            // Create ShellLink instance
            // ReSharper disable once SuspiciousTypeConversion.Global
            var shellLink = (IShellLinkW)new ShellLink();

            // Set target path and working directory
            shellLink.SetPath(executablePath);
            shellLink.SetWorkingDirectory(originalFileDirectory);

            // Save the shortcut
            // ReSharper disable once SuspiciousTypeConversion.Global
            var persistFile = (IPersistFile)shellLink;
            persistFile.Save(linkPath, true);

            // Release COM objects
            Marshal.ReleaseComObject(persistFile);
            Marshal.ReleaseComObject(shellLink);

            logger.LogInformation("Shortcut created successfully at: {LinkPath}", linkPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create shortcut at: {LinkPath}", linkPath);
            return false;
        }
    }

    private static string? GetLinkPath()
    {
        var executablePath = Environment.ProcessPath;
        if (executablePath is null)
        {
            return null;
        }

        var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var fileName = Path.GetFileNameWithoutExtension(executablePath);
        return Path.Combine(startupPath, fileName + ".lnk");
    }
}


// Native P/Invoke interfaces for creating Windows shortcuts without COM
[ComImport]
[Guid("000214F9-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
file interface IShellLinkW
{
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
    void GetIDList(out IntPtr ppidl);
    void SetIDList(IntPtr pidl);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    void GetHotkey(out short pwHotkey);
    void SetHotkey(short wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
    void Resolve(IntPtr hwnd, uint fFlags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[ComImport]
[Guid("0000010b-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
file interface IPersistFile
{
    void GetClassID(out Guid pClassID);
    [PreserveSig]
    int IsDirty();
    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
    void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}

[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
[ClassInterface(ClassInterfaceType.None)]
file class ShellLink
{
}