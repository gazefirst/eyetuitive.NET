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
            }
            catch (TaskCanceledException) { } //task cancelled
            catch (InvalidOperationException) { } //stream already finished / is closed...
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "StreamAsync failed");
            }
            finally
            {
                taskRunning = false;
            }
        }
    }
}
