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
        private static bool _initialCheckDone = false;
        private static readonly object _initLock = new object();

        /// <summary>
        /// ConnectedChanged event (true if connected, false if disconnected)
        /// </summary>
        internal static event Action<bool> ConnectedChanged;

        /// <summary>
        /// Check if the target USB device is connected.
        /// Only queries USB devices on the first call; subsequent calls return cached state
        /// which is kept up-to-date by the WMI event watcher.
        /// </summary>
        /// <returns></returns>
        internal static bool CheckConnect()
        {
            EnsureInitialCheck();
            return _isConnected;
        }

        /// <summary>
        /// Performs the initial USB device query if not already done.
        /// </summary>
        private static void EnsureInitialCheck()
        {
            if (_initialCheckDone) return;

            lock (_initLock)
            {
                if (_initialCheckDone) return;

                if (Helper.IsWindowsPlatform())
                {
                    try
                    {
                        _isConnected = queryUSBdevices();
                    }
                    catch (Exception)
                    {
                        _isConnected = false;
                    }
                }
                _initialCheckDone = true;
            }
        }

        static UsbDeviceMonitor()
        {
            // Start monitoring the USB devices for changes only if running on Windows platform
            if (Helper.IsWindowsPlatform()) StartWatcher();
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
