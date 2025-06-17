using System;
using System.Management;
using System.Runtime.InteropServices;
using Google.Protobuf;

namespace eyetuitive.NET.classes
{
    /// <summary>
    /// General helper class for various utility functions
    /// </summary>
    internal static class Helper
    {
        /// <summary>
        /// Convert a ByteString to a Guid
        /// </summary>
        /// <param name="byteString"></param>
        /// <returns></returns>
        internal static Guid FromByteString(ByteString byteString)
        {
            if (byteString == null || byteString.IsEmpty) return Guid.Empty;
            try
            {
                return new Guid(byteString.ToByteArray());
            }
            catch (Exception) { }
            return Guid.Empty;
        }

        /// <summary>
        /// Check if the current platform is Windows
        /// </summary>
        /// <returns></returns>
        internal static bool IsWindowsPlatform()
        {
#if NET6_0_OR_GREATER
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
        // For .NET Framework, assume Windows
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif
        }

        /// <summary>
        /// Get the computer manufacturer and model information
        /// </summary>
        /// <returns></returns>
        public static (string Manufacturer, string Model) GetComputerVendorAndModel()
        {
            if(!IsWindowsPlatform()) return ("Unknown", "Unknown"); //Check if running on Windows platform, if not, return unknown values

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                        string model = obj["Model"]?.ToString() ?? "Unknown";
                        return (manufacturer, model);
                    }
                }
            }
            catch
            {
                // Optional: log exception
            }

            return ("Unknown", "Unknown");
        }
    }
}
