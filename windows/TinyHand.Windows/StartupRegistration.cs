using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace TinyHand.Windows;

internal sealed class StartupRegistration
{
    private const string ValueName = "TinyHand.Windows";
    private readonly string keyPath;
    private readonly string executablePath;

    internal StartupRegistration(string executablePath,
        string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run")
    {
        this.executablePath = Path.GetFullPath(executablePath);
        this.keyPath = keyPath;
    }

    internal bool Enabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                return key?.GetValue(ValueName) is string command && !string.IsNullOrWhiteSpace(command);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
            {
                return false;
            }
        }
    }

    internal bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, true);
            if (enabled)
                key.SetValue(ValueName, $"\"{executablePath}\"", RegistryValueKind.String);
            else
                key.DeleteValue(ValueName, false);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            return false;
        }
    }
}
