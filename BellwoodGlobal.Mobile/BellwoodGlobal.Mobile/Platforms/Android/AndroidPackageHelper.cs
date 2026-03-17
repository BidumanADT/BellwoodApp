using Android.Content.PM;
using Android.OS;
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

            byte[]? signatureBytes = null;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                // API 28+: PackageInfoFlags.Signatures is deprecated and returns nothing.
                // Use SigningCertificates instead, which returns the current signer.
#pragma warning disable CA1416
                var packageInfo = packageManager.GetPackageInfo(
                    packageName,
                    PackageInfoFlags.SigningCertificates);
#pragma warning restore CA1416

                var signingInfo = packageInfo?.SigningInfo;
                if (signingInfo == null)
                    throw new InvalidOperationException("SigningInfo is null");

                // HasMultipleSigners means the app has a signing history with rotation.
                // GetApkContentsSigners() gives the current active signers (index 0 = current).
                // GetSigningCertificateHistory() gives all certs including rotated-away ones.
                var signers = signingInfo.HasMultipleSigners
                    ? signingInfo.GetApkContentsSigners()
                    : signingInfo.GetSigningCertificateHistory();

                if (signers == null || signers.Length == 0)
                    throw new InvalidOperationException("No signing certificates found");

                signatureBytes = signers[0].ToByteArray();
            }
            else
            {
                // API < 28: use the legacy flag
#pragma warning disable CA1422 // Validate platform compatibility
#pragma warning disable CS0618  // PackageInfoFlags.Signatures is obsolete
                var packageInfo = packageManager.GetPackageInfo(
                    packageName,
                    PackageInfoFlags.Signatures);
#pragma warning restore CS0618
#pragma warning restore CA1422

                if (packageInfo?.Signatures == null || packageInfo.Signatures.Count == 0)
                    throw new InvalidOperationException("No signatures found");

                signatureBytes = packageInfo.Signatures[0]?.ToByteArray();
            }

            if (signatureBytes == null || signatureBytes.Length == 0)
                throw new InvalidOperationException("Signature bytes are empty");
            
            // Compute SHA-1 hash
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(signatureBytes);
            
            // Convert to uppercase hex string (no separators — required by X-Android-Cert header)
            return BitConverter.ToString(hash).Replace("-", "");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get certificate fingerprint: {ex.Message}", ex);
        }
    }
}
