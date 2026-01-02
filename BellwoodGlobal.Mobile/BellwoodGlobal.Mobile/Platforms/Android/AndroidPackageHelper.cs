using Android.Content.PM;
using System.Security.Cryptography;

namespace BellwoodGlobal.Mobile.Platforms.Android;

/// <summary>
/// Helper to get Android package name and SHA-1 certificate fingerprint.
/// Required for Google Places API key restrictions.
/// </summary>
public static class AndroidPackageHelper
{
    /// <summary>
    /// Gets the app's package name (e.g., "com.bellwoodglobal.mobile").
    /// </summary>
    public static string GetPackageName()
    {
        var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.ApplicationContext
            ?? throw new InvalidOperationException("Android context not available");
        
        return context.PackageName 
            ?? throw new InvalidOperationException("Package name is null");
    }
    
    /// <summary>
    /// Gets the SHA-1 fingerprint of the app's signing certificate.
    /// Format: Uppercase hex with no separators (e.g., "AABBCCDD...").
    /// </summary>
    public static string GetCertificateFingerprint()
    {
        var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.ApplicationContext
            ?? throw new InvalidOperationException("Android context not available");
        
        var packageName = context.PackageName
            ?? throw new InvalidOperationException("Package name is null");
        
        try
        {
            var packageManager = context.PackageManager
                ?? throw new InvalidOperationException("PackageManager is null");
            
            // Get package info with signatures
#pragma warning disable CA1416 // Validate platform compatibility
            var packageInfo = packageManager.GetPackageInfo(
                packageName, 
                PackageInfoFlags.Signatures);
#pragma warning restore CA1416
            
            if (packageInfo?.Signatures == null || packageInfo.Signatures.Count == 0)
                throw new InvalidOperationException("No signatures found");
            
            var signature = packageInfo.Signatures[0];
            if (signature == null)
                throw new InvalidOperationException("Signature is null");
            
            var signatureBytes = signature.ToByteArray();
            if (signatureBytes == null || signatureBytes.Length == 0)
                throw new InvalidOperationException("Signature bytes are empty");
            
            // Compute SHA-1 hash
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(signatureBytes);
            
            // Convert to uppercase hex string (no separators for header)
            var fingerprint = BitConverter.ToString(hash).Replace("-", "");
            
            return fingerprint;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get certificate fingerprint: {ex.Message}", ex);
        }
    }
}
