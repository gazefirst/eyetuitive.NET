using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GazeFirst.functions
{
    public class Video : ClientBased
    {
        private event EventHandler<FrameArgs> videoDataAvailable;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<EventHandler<FrameArgs>> _subscribers = new List<EventHandler<FrameArgs>>();
        private bool taskRunning = false;

        internal Video(Eyetracker.EyetrackerClient client) : base(client) { }

        /// <summary>
        /// Start video stream
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="videoDataHandler"></param>
        public void StartVideoStream(EventHandler<FrameArgs> videoDataHandler)
        {
            videoDataAvailable += videoDataHandler;
            _subscribers.Add(videoDataHandler);

            if (_subscribers.Count > 0 && !taskRunning)
            {
                // Start the task only if this is the first subscriber
                Task.Run(() => StreamAsync(_cts.Token), _cts.Token);
            }
        }

        /// <summary>
        /// Stop video stream
        /// </summary>
        /// <param name="videoDataHandler"></param>
        public void StopVideoStream(EventHandler<FrameArgs> videoDataHandler)
        {
            videoDataAvailable -= videoDataHandler;
            _subscribers.Remove(videoDataHandler);

            if (_subscribers.Count == 0)
            {
                // Stop the task if there are no more subscribers
                _cts.Cancel();
                _cts = new CancellationTokenSource(); // Reset the CancellationTokenSource for future use
            }
        }

        /// <summary>
        /// Reconnect to the eye tracker and restart all the stream tasks
        /// </summary>
        /// <param name="client"></param>
        internal override void Reconnect()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource(); // Reset the CancellationTokenSource for future use
            if (_subscribers.Count > 0)
            {
                Task.Run(() => StreamAsync(_cts.Token), _cts.Token);
            }
        }

        /// <summary>
        /// Stream video data
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StreamAsync(CancellationToken cancellationToken)
        {
            taskRunning = true;
            TimeSpan backoff = TimeSpan.FromMilliseconds(250);
            const int backoffMaxMs = 5000;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var videoStream = _client.SubscribeRawVideo(new Google.Protobuf.WellKnownTypes.Empty());
                        while (await videoStream.ResponseStream.MoveNext(cancellationToken))
                        {
                            if (videoStream.ResponseStream.Current != null)
                            {
                                var Frame = videoStream.ResponseStream.Current;

                                byte[] frameBytes = Frame.Data.ToByteArray();
                                var frameArgs = new FrameArgs()
                                {
                                    channels = Frame.Channels,
                                    height = Frame.Height,
                                    width = Frame.Width,
                                    data = frameBytes,
                                    timestamp = Frame.Timestamp,
                                };

                                videoDataAvailable?.Invoke(this, frameArgs);
                            }
                        }
                        // Stream finished normally: short delay and resubscribe
                        await Task.Delay(backoff, cancellationToken);
                    }
                    catch (TaskCanceledException) { break; } // cancelled
                    catch (OperationCanceledException) { break; } // cancelled
                    catch (InvalidOperationException)
                    {
                        await Task.Delay(backoff, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        if (e is Grpc.Core.RpcException rpcEx)
                        {
                            if (rpcEx.StatusCode == Grpc.Core.StatusCode.Cancelled)
                            {
                                eyetuitive._logger?.LogDebug("Video stream cancelled");
                                break;
                            }
                            else if (rpcEx.StatusCode == Grpc.Core.StatusCode.Unavailable ||
                                     rpcEx.StatusCode == Grpc.Core.StatusCode.Internal ||
                                     rpcEx.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                            {
                                eyetuitive._logger?.LogWarning("Video stream unavailable, retrying...");
                            }
                            else
                                eyetuitive._logger?.LogError(e, "Video stream error");
                        }
                        else
                            eyetuitive._logger?.LogError(e, "StreamAsync failed, retrying...");

                        backoff = TimeSpan.FromMilliseconds(Math.Min(backoff.TotalMilliseconds * 2, backoffMaxMs));
                        try { await Task.Delay(backoff, cancellationToken); } catch { break; }
                    }
                }
            }
            finally
            {
                taskRunning = false;
            }
        }
    }
}
