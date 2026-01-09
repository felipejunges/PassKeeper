using System.Runtime.InteropServices;
using System.Security;

namespace PassKeeper.Gtk.Extensions;

public static class SecureStringExtensions
{
    // Create a SecureString from a regular string
    public static SecureString CreateFromString(this string source)
    {
        var ss = new SecureString();
        foreach (char c in source)
            ss.AppendChar(c);
        ss.MakeReadOnly();
        return ss;
    }

    // Convert SecureString back to plain string (use only when absolutely necessary)
    public static string? SecureStringToString(this SecureString? secure)
    {
        if (secure == null) return null;
        
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(secure);
            return Marshal.PtrToStringUni(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }
}