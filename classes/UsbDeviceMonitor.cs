using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace eyetuitive.NET.classes
{
    internal static class UsbDeviceMonitor
    {
        private static readonly ILogger _logger = GazeFirst.eyetuitive._logger;
        private const string _targetVendorId = "36F8"; //GazeFirst USB Vendor ID
        private static string[] _targetProductIds = { "0002", "0003", "0004" }; //GazeFirst USB Product IDs
        private static bool _isConnected = false;
        private static Task checkerTask;

        /// <summary>
        /// ConnectedChanged event (true if connected, false if disconnected)
        /// </summary>
        internal static event Action<bool> ConnectedChanged;

        /// <summary>
        /// Check if the target USB device is connected
        /// </summary>
        /// <returns></returns>
        internal static bool CheckConnect()
        {
            bool connected = isAvailable();
            if (connected != _isConnected)
            {
                _isConnected = connected;
                ConnectedChanged?.Invoke(_isConnected);
            }
            return connected;
        }

        static UsbDeviceMonitor()
        {
            // Start monitoring the USB devices for changes only if running on Windows platform
            if (IsWindowsPlatform()) StartWatcher();
        }

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
        /// Start monitoring the USB devices for changes
        /// </summary>
        private static void StartWatcher()
        {
            try
            {
                var query = new WqlEventQuery()
                {
                    EventClassName = "__InstanceOperationEvent",
                    WithinInterval = new TimeSpan(0, 0, 3),
                    Condition = @"TargetInstance ISA 'Win32_USBHub'"
                };

                var scope = new ManagementScope("root\\CIMV2");
                using (var moWatcher = new ManagementEventWatcher(scope, query))
                {
                    moWatcher.Options.Timeout = ManagementOptions.InfiniteTimeout;
                    moWatcher.EventArrived += new EventArrivedEventHandler(DeviceChangedEvent);
                    moWatcher.Start();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting USB device watcher");
            }
        }

        /// <summary>
        /// DeviceChangedEvent handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DeviceChangedEvent(object sender, EventArrivedEventArgs e)
        {
            var targetInstance = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            var eventType = e.NewEvent.ClassPath.ClassName;

            if (targetInstance != null)
            {
                var deviceId = targetInstance["DeviceID"]?.ToString();

                if (!string.IsNullOrEmpty(deviceId) && deviceId.Contains(_targetVendorId))
                {
                    foreach (var productId in _targetProductIds)
                    {
                        if (deviceId.Contains(productId))
                        {
                            if (eventType == "__InstanceCreationEvent")
                            {
                                _logger?.LogInformation("eyetuitive connected");
                                _isConnected = true;
                                ConnectedChanged?.Invoke(true);
                            }
                            else if (eventType == "__InstanceDeletionEvent")
                            {
                                _logger?.LogInformation("eyetuitive disconnected");
                                _isConnected = false;
                                ConnectedChanged?.Invoke(false);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if the target USB device is connected
        /// </summary>
        private static bool isAvailable()
        {
            if (!IsWindowsPlatform()) 
                throw new PlatformNotSupportedException("USB device monitoring is only supported on Windows platform.");
            try
            {
                bool res = false;
                if (checkerTask == null || checkerTask.IsCompleted)
                {
                    checkerTask = Task.Run(() =>
                    {
                        res = queryUSBdevices();
                    });
                }
                Task.WaitAll(checkerTask);
                return res;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the target USB device is available
        /// </summary>
        /// <returns></returns>
        private static bool queryUSBdevices()
        {
            bool res = false;
            var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub");
            foreach (ManagementObject device in searcher.Get().Cast<ManagementObject>())
            {
                var deviceId = Convert.ToString(device["DeviceID"]);
                if (!string.IsNullOrEmpty(deviceId) && deviceId.Contains(_targetVendorId))
                {
                    foreach (var productId in _targetProductIds)
                    {
                        if (deviceId.Contains(productId))
                        {
                            res = true;
                            break;
                        }
                    }
                }
            }
            return res;
        }
    }
}
