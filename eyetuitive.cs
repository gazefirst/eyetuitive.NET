using eyetuitive.NET.classes;
using GazeFirst.functions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;
using static GazeFirst.Eyetracker;

namespace GazeFirst
{
    /// <summary>
    /// EyetrackingLib class
    /// </summary>
    public class eyetuitive : IDisposable
    {
        internal static ILogger _logger;

        private readonly string _host;
        private readonly int _port;
        private GrpcChannel _channel;
        private EyetrackerClient _client;
        private CancellationTokenSource _connectionCts = new CancellationTokenSource();
        private readonly object _connectionLock = new object();
        private bool _isConnecting = false, _isConnected = false, _monitoring = false;
        private Task<bool> _connectionTask;

        //Internal functions
        private Position position;
        private Calibration calibration;
        private Gaze gaze;
        private GazeFirst.functions.Settings settings;
        private Users users;
        private Video video;

        /// <summary>
        /// Client
        /// </summary>
        internal EyetrackerClient Client
        {
            get
            {
                if (_client == null)
                    throw new InvalidOperationException("Not initialized or connected to the eye tracker");
                return _client;
            }
        }

        /// <summary>
        /// Reconect to the eye tracker and restart all the tracking tasks
        /// </summary>
        private void Reconnect()
        {
            _logger?.LogInformation("Reconnecting to the eye tracker...");
            position?.UpdateClient(Client);
            calibration?.UpdateClient(Client);
            gaze?.UpdateClient(Client);
            settings?.UpdateClient(Client);
            users?.UpdateClient(Client);
            video?.UpdateClient(Client);
        }

        /// <summary>
        /// Position object, used for getting position data
        /// </summary>
        public Position Position
        {
            get
            {
                if (position == null) position = new Position(Client);
                return position;
            }
        }

        /// <summary>
        /// Calibration object, used for calibrating the eye tracker
        /// </summary>
        public Calibration Calibration
        {
            get
            {
                if (calibration == null) calibration = new Calibration(Client);
                return calibration;
            }
        }

        /// <summary>
        /// Gaze object, used for getting gaze data
        /// </summary>
        public Gaze Gaze
        {
            get
            {
                if (gaze == null) gaze = new Gaze(Client);
                return gaze;
            }
        }

        /// <summary>
        /// Settings object, used for updating screen size, mounting, user and device settings
        /// </summary>
        public GazeFirst.functions.Settings Settings
        {
            get
            {
                if (settings == null) settings = new GazeFirst.functions.Settings(Client);
                return settings;
            }
        }

        /// <summary>
        /// Users object, used for getting user data and managing users
        /// </summary>
        public Users Users
        {
            get
            {
                if (users == null) users = new Users(Client);
                return users;
            }
        }

        /// <summary>
        /// Video object, used for getting video data
        /// </summary>
        public Video Video
        {
            get
            {
                if (video == null) video = new Video(Client);
                return video;
            }
        }

        /// <summary>
        /// EyetrackingLib constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public eyetuitive(string host = "eyetracker.local", int port = 12340)
        {
            _host = host;
            _port = port;
        }

        /// <summary>
        /// Attach a logger
        /// </summary>
        /// <param name="loggerFactory"></param>
        public static void AttachLogger(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger(nameof(eyetuitive));
        }

        /// <summary>
        /// Connect to the eye tracker
        /// </summary>
        /// <param name="timeoutInSeconds"></param>
        /// <returns></returns>
        public Task<bool> ConnectAsync(double timeoutInSeconds = 5)
        {
            lock (_connectionLock)
            {
                if (_isConnecting)
                {
                    // Return the existing task if a connection attempt is already in progress
                    return _connectionTask ?? Task.FromResult(false);
                }

                _isConnecting = true;
                _connectionTask = ConnectInternalAsync(timeoutInSeconds);
                return _connectionTask;
            }
        }

        /// <summary>
        /// Check if an eye tracker is available (only on Windows)
        /// </summary>
        /// <returns></returns>
        public static bool IsAvailable()
        {
            return UsbDeviceMonitor.CheckConnect();
        }

        /// <summary>
        /// Internal connect method, handles retries
        /// </summary>
        /// <param name="timeoutInSeconds"></param>
        /// <returns></returns>
        private async Task<bool> ConnectInternalAsync(double timeoutInSeconds)
        {
            _connectionCts?.Dispose();
            _connectionCts = new CancellationTokenSource();

            _channel = GrpcChannel.ForAddress($"http://{_host}:{_port}");
            _client = new EyetrackerClient(_channel);

            var policy = Policy.Handle<Exception>().WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            try
            {
                return await policy.ExecuteAsync(async () =>
                {
                    try
                    {
#if NET6_0_OR_GREATER
                        var token = CancellationTokenSource.CreateLinkedTokenSource(_connectionCts.Token, new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds)).Token).Token;
                        await _channel.ConnectAsync(token);
                        MonitorConnection();
                        return true;
#else
                        var httpClientHandler = new System.Net.Http.WinHttpHandler()
                        {
                            ClientCertificateOption = System.Net.Http.ClientCertificateOption.Manual,
                            ServerCertificateValidationCallback = (a, b, c, d) => true
                        };
                        var httpClient = new System.Net.Http.HttpClient(httpClientHandler);
                        _channel = GrpcChannel.ForAddress($"https://{_host}:{_port + 1}", new GrpcChannelOptions
                        {
                            HttpClient = httpClient,
                        });
                        _client = new EyetrackerClient(_channel);
                        var info = await _client.GetDeviceInfoAsync(new Empty());
                        MonitorConnection();
                        return (info.Serial != 0);
#endif
                    }
                    catch (TaskCanceledException)
                    {
                        // Log connection timeout
                        return false;
                    }
                });
            }
            finally
            {
                lock (_connectionLock)
                {
                    _isConnecting = false;
                }
            }
        }

        /// <summary>
        /// Monitor the connection
        /// </summary>
        private void MonitorConnection()
        {
            if (_monitoring) return;
            UsbDeviceMonitor.ConnectedChanged += OnConnectedChanged;
            _monitoring = true;
        }

        /// <summary>
        /// Handle USB device connection changes
        /// </summary>
        /// <param name="isConnected"></param>
        private async void OnConnectedChanged(bool isConnected)
        {
            _logger?.LogInformation($"USB device connection status changed: {isConnected}");
            if (isConnected)
            {
                _client = new EyetrackerClient(_channel);
#if NET6_0_OR_GREATER
                await _channel.ConnectAsync();
                Reconnect();
#else
                var info = await _client.GetDeviceInfoAsync(new Empty());
                if(info.Serial != 0) Reconnect();
#endif
            }
        }

        /// <summary>
        /// Disconnect from the eye tracker
        /// </summary>
        public void Dispose()
        {
            _connectionCts?.Cancel();
            _connectionCts?.Dispose();
            _channel?.ShutdownAsync().Wait();
        }
    }
}