using eyetuitive.NET.classes;
using GazeFirst.functions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http;
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
        private volatile bool _disposed = false;
        private Task<bool> _connectionTask;
        private static readonly TimeSpan ReconnectTimeout = TimeSpan.FromSeconds(5);

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
                if (_disposed)
                    throw new ObjectDisposedException(nameof(eyetuitive));
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
        /// ConnectedChanged event (true if connected, false if disconnected)
        /// </summary>
        public event Action<bool> ConnectedChanged
        {
            add
            {
                UsbDeviceMonitor.ConnectedChanged += value;
            }
            remove
            {
                UsbDeviceMonitor.ConnectedChanged -= value;
            }
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

            // Built once, before the retry policy. Creating the channel inside the policy meant a
            // new HttpClient/handler/channel per attempt, each overwriting the last without disposing
            // it, and - because a fresh channel resolves the host again - the default mDNS name was
            // re-resolved on every attempt. On Windows that resolution measures ~2.7s and is not
            // cached, so with the 2s/4s/8s backoff a connect took ~15s to succeed.
            _channel?.ShutdownAsync().Wait(TimeSpan.FromSeconds(1));
#if NET6_0_OR_GREATER
            _channel = CreateChannel(_host, _port);
#else
            var httpClientHandler = new WinHttpHandler
            {
                ClientCertificateOption = ClientCertificateOption.Manual,
                ServerCertificateValidationCallback = (a, b, c, d) => true
            };
            var httpClient = new HttpClient(httpClientHandler);
            _channel = GrpcChannel.ForAddress($"https://{_host}:{_port + 1}", new GrpcChannelOptions
            {
                HttpClient = httpClient,
            });
#endif
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
                        Reconnect();
                        MonitorConnection();
                        return true;
#else
                        // Retry only the call; the channel above is reused across attempts.
                        // Honour timeoutInSeconds here too: without a deadline this call is bounded
                        // only by the HttpClient defaults, so the parameter had no effect on this
                        // target and callers could not cap how long a connect attempt takes.
                        var info = await _client.GetDeviceInfoAsync(new Empty(),
                            deadline: DateTime.UtcNow.AddSeconds(timeoutInSeconds));
                        if (info.Serial == 0) return false;
                        Reconnect();
                        MonitorConnection();
                        return true;
#endif
                    }
                    catch (TaskCanceledException)
                    {
                        // Log connection timeout
                        return false;
                    }
                    catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                    {
                        // A deadline is the non-NET6 equivalent of the cancellation above, and gRPC
                        // reports it as RpcException rather than TaskCanceledException. Treated the
                        // same way so a timeout does not turn into a retry on one target only.
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
        /// For NET6_0_OR_GREATER, create a gRPC channel with HTTP/2 keepalive pings enabled.
        /// </summary>
#if NET6_0_OR_GREATER
        private static GrpcChannel CreateChannel(string host, int port)
        {
            var handler = new SocketsHttpHandler
            {
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(20),
#if NET8_0_OR_GREATER
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
#endif
                EnableMultipleHttp2Connections = true,
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            };
            return GrpcChannel.ForAddress($"http://{host}:{port}", new GrpcChannelOptions { HttpHandler = handler });
        }
#endif

        /// <summary>
        /// Monitor the connection
        /// </summary>
        private void MonitorConnection()
        {
            if (_monitoring || _disposed) return;
            Settings?.updateHostSettings();
            lock (_connectionLock)
            {
                if (_monitoring || _disposed) return;
                UsbDeviceMonitor.ConnectedChanged += OnConnectedChanged;
                _monitoring = true;
            }
        }

        /// <summary>
        /// Handle USB device connection changes
        /// </summary>
        /// <param name="isConnected"></param>
        private async void OnConnectedChanged(bool isConnected)
        {
            // async void on the WMI event thread: anything that escapes ends the process.
            try
            {
                if (_disposed) return;
                _logger?.LogInformation($"USB device connection status changed: {isConnected}");
                if (!isConnected)
                {
                    // Device disconnected - nothing to do, the event already notified subscribers
                    return;
                }

                _client = new EyetrackerClient(_channel);
#if NET6_0_OR_GREATER
                using (var cts = new CancellationTokenSource(ReconnectTimeout))
                {
                    await _channel.ConnectAsync(cts.Token);
                }
                if (_disposed) return;
                Reconnect();
                Settings?.updateHostSettings();
#else
                var info = await _client.GetDeviceInfoAsync(new Empty(),
                    deadline: DateTime.UtcNow.Add(ReconnectTimeout));
                if (_disposed) return;
                if(info.Serial != 0)
                {
                    Reconnect();
                    Settings?.updateHostSettings();
                }
#endif
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to reconnect to eye tracker after connection change");
            }
        }

        /// <summary>
        /// Disconnect from the eye tracker
        /// </summary>
        public void Dispose()
        {
            lock (_connectionLock)
            {
                if (_disposed) return;
                _disposed = true;
                if (_monitoring)
                {
                    UsbDeviceMonitor.ConnectedChanged -= OnConnectedChanged;
                    _monitoring = false;
                }
            }
            _connectionCts?.Cancel();
            _connectionCts?.Dispose();
            // Bounded so a stuck shutdown cannot hang the caller, which is often a UI thread.
            _channel?.ShutdownAsync().Wait(TimeSpan.FromSeconds(1));
        }
    }
}