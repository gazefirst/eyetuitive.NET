using GazeFirst;
using Microsoft.Extensions.Logging;
using System;

namespace GazeFirst.functions
{
    /// <summary>
    /// Settings class
    /// </summary>
    public class Settings : ClientBased
    {
        private GazeFirst.Settings _settings;

        /// <summary>
        /// Settings constructor
        /// </summary>
        /// <param name="_client"></param>
        internal Settings(Eyetracker.EyetrackerClient _client) : base(_client) { }

        /// <summary>
        /// Get current user settings
        /// </summary>
        /// <returns></returns>
        public UserSettingsArgs GetCurrentUsersSettings()
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                return new UserSettingsArgs()
                {
                    Smoothing = _settings.UserSettings.Smoothing,
                    LeftEyeOnly = _settings.UserSettings.LeftEyeOnly,
                    RightEyeOnly = _settings.UserSettings.RightEyeOnly
                };
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to get current user settings");
                return null;
            }
        }

        /// <summary>
        /// Get current device settings
        /// </summary>
        /// <returns></returns>
        public DeviceSettingsArgs GetCurrentDeviceSettings()
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                return new DeviceSettingsArgs()
                {
                    pauseNative = _settings.DeviceSettings.PauseNative,
                    pauseAPIGaze = _settings.DeviceSettings.PauseAPIGaze,
                    enablePauseByGaze = _settings.DeviceSettings.EnablePauseByGaze,
                    Framerate = (FrameRate)((int)_settings.DeviceSettings.Framerate)
                };
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to get current device settings");
                return null;
            }
        }

        /// <summary>
        /// Return device info (serial number, firmware version, hardware config)
        /// Note: this is mostly for internal purposes
        /// </summary>
        /// <returns></returns>
        public (long serialNumber, string firmwareVersion, int hardwareConfig) GetDeviceInfo()
        {
            try
            {
                var result = _client.GetDeviceInfo(new Google.Protobuf.WellKnownTypes.Empty());
                return (result.Serial, result.Version, result.HwConfig);
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to get device info");
                return (0, "unknown", 0);
            }
        }

        /// <summary>
        /// Update screen size - units are in mm
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool updateScreenSize(double width, double height)
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                Configuration configuration = new Configuration()
                {
                    Settings = new GazeFirst.Settings()
                    {
                        Size = new ScreenSize()
                        {
                            WidthMm = width,
                            HeightMm = height
                        }
                    }
                };
                _settings = _client.Configure(configuration); //Update settings
                return true;
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to update screen size");
                return false;
            }

        }

        /// <summary>
        /// Update pause native (e.g. HID or USB)
        /// </summary>
        /// <param name="pause"></param>
        /// <returns></returns>
        public bool updatePauseNative(bool pause)
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                _settings.DeviceSettings.PauseNative = pause;
                _settings.DeviceSettings.Update = true;
                Configuration configuration = new Configuration()
                {
                    Settings = new GazeFirst.Settings()
                    {
                        DeviceSettings = _settings.DeviceSettings
                    }
                };
                _settings = _client.Configure(configuration); //Update settings
                return true;
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to update pause native");
                return false;
            }
        }

        /// <summary>
        /// Update pause API
        /// </summary>
        /// <param name="pause"></param>
        /// <returns></returns>
        public bool updatePauseAPI(bool pause)
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                _settings.DeviceSettings.PauseAPIGaze = pause;
                _settings.DeviceSettings.Update = true;
                Configuration configuration = new Configuration()
                {
                    Settings = new GazeFirst.Settings()
                    {
                        DeviceSettings = _settings.DeviceSettings
                    }
                };
                _settings = _client.Configure(configuration); //Update settings
                return true;
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to update pause API");
                return false;
            }
        }

        /// <summary>
        /// Set frame rate
        /// </summary>
        /// <param name="frameRate"></param>
        /// <returns></returns>
        public bool setFrameRate(FrameRate frameRate)
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                _settings.DeviceSettings.Framerate = (DeviceSettings.Types.Framerate)((int)frameRate);
                _settings.DeviceSettings.Update = true;
                Configuration configuration = new Configuration()
                {
                    Settings = new GazeFirst.Settings()
                    {
                        DeviceSettings = _settings.DeviceSettings
                    }
                };
                _settings = _client.Configure(configuration); //Update settings
                return true;
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to update frame rate");
                return false;
            }
        }

        /// <summary>
        /// Configure smoothing, call with no parameters to reset to default.
        /// Note: This affects the current user profile (or default if no user profile is selected)
        /// </summary>
        /// <param name="smoothing"></param>
        /// <returns></returns>
        public bool configureSmoothing(int smoothing = 7)
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                _settings.UserSettings.Smoothing = smoothing;
                _settings.UserSettings.Update = true;
                Configuration configuration = new Configuration()
                {
                    Settings = new GazeFirst.Settings()
                    {
                        UserSettings = _settings.UserSettings
                    }
                };
                _settings = _client.Configure(configuration); //Update settings
                return true;
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to update pause API");
                return false;
            }
        }

        /// <summary>
        /// Select eyes to track, call with no parameters to reset to default (both eyes).
        /// Note: This affects the current user profile (or default if no user profile is selected)
        /// </summary>
        /// <param name="leftEyeOnly"></param>
        /// <param name="rightEyeOnly"></param>
        /// <returns></returns>
        public bool selectEyesToTrack(bool leftEyeOnly = false, bool rightEyeOnly = false)
        {
            try
            {
                _settings = _client.Configure(new Configuration() { Empty = new Google.Protobuf.WellKnownTypes.Empty() }); //Get current settings
                _settings.UserSettings.LeftEyeOnly = leftEyeOnly;
                _settings.UserSettings.RightEyeOnly = rightEyeOnly;
                _settings.UserSettings.Update = true;
                Configuration configuration = new Configuration()
                {
                    Settings = new GazeFirst.Settings()
                    {
                        UserSettings = _settings.UserSettings
                    }
                };
                _settings = _client.Configure(configuration); //Update settings
                return true;
            }
            catch (Exception ex)
            {
                eyetuitive._logger?.LogError(ex, "Failed to update pause API");
                return false;
            }
        }
    }
}
