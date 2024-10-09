using GazeFirst;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GazeFirst.functions
{
    /// <summary>
    /// Gaze class
    /// </summary>
    public class Gaze : ClientBased
    {
        private ConcurrentDictionary<(bool filter, EventHandler<GazeEventArgs> handler), TrackingTaskInfo> _trackingTasks = new ConcurrentDictionary<(bool, EventHandler<GazeEventArgs>), TrackingTaskInfo>();
        /// <summary>
        /// Gaze constructor
        /// </summary>
        /// <param name="_client"></param>
        internal Gaze(Eyetracker.EyetrackerClient _client) : base(_client) { }

        /// <summary>
        /// Start gaze tracking with the given handler
        /// </summary>
        /// <param name="filtered"></param>
        /// <param name="GazeChangedHandler"></param>
        public void StartGazeTracking(EventHandler<GazeEventArgs> GazeChangedHandler, bool filtered = true)
        {
            var key = (filtered, GazeChangedHandler);
            if (_trackingTasks.ContainsKey(key))
            {
                // Task with these parameters already exists
                return;
            }

            var cts = new CancellationTokenSource();
            var task = Task.Run(() => GetGazeAsync(filtered, GazeChangedHandler, cts.Token), cts.Token);

            _trackingTasks.TryAdd(key, new TrackingTaskInfo { CancellationTokenSource = cts, Task = task });
        }

        /// <summary>
        /// Stop gaze tracking with the given handler
        /// </summary>
        /// <param name="filtered"></param>
        /// <param name="GazeChangedHandler"></param>
        public void StopGazeTracking(EventHandler<GazeEventArgs> GazeChangedHandler, bool filtered = true)
        {
            var key = (filtered, GazeChangedHandler);
            if (_trackingTasks.TryRemove(key, out var trackingTaskInfo))
            {
                trackingTaskInfo?.CancellationTokenSource?.Cancel();
                // Optionally await the task if necessary: await trackingTaskInfo.Task;
            }
        }

        /// <summary>
        /// Reconnect to the eye tracker and restart all the tracking tasks
        /// </summary>
        /// <param name="_client"></param>
        internal override void Reconnect()
        {
            var trackingTasks = new ConcurrentDictionary<(bool, EventHandler<GazeEventArgs>), TrackingTaskInfo>();
            foreach (var key in _trackingTasks.Keys)
            {
                var cts = new CancellationTokenSource();
                var task = Task.Run(() => GetGazeAsync(key.filter, key.handler, cts.Token), cts.Token);

                trackingTasks.TryAdd(key, new TrackingTaskInfo { CancellationTokenSource = cts, Task = task });
            }
            //Cancel all the previous tasks
            foreach (var key in _trackingTasks.Keys)
            {
                _trackingTasks[key]?.CancellationTokenSource?.Cancel();
            }
            _trackingTasks = trackingTasks;
        }

        /// <summary>
        /// Get gaze data
        /// </summary>
        /// <param name="filtered"></param>
        /// <param name="GazeChangedHandler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task GetGazeAsync(bool filtered, EventHandler<GazeEventArgs> GazeChangedHandler, CancellationToken cancellationToken)
        {
            try
            {
                var gazeStream = _client.SubscribeGaze(new GazeSubscription() { Unfiltered = !filtered });
                while (await gazeStream.ResponseStream.MoveNext(cancellationToken))
                {
                    if (gazeStream.ResponseStream.Current != null)
                    {
                        var gaze = gazeStream.ResponseStream.Current;
                        var GazeData = new GazeEventArgs()
                        {
                            gazePoint = new NormedPoint2d(gaze.GazePoint.X, gaze.GazePoint.Y),
                            leftEye = new NormedPoint2d(gaze.LeftEye.X, gaze.LeftEye.Y),
                            rightEye = new NormedPoint2d(gaze.RightEye.X, gaze.RightEye.Y),
                            leftEyeOpen = gaze.LeftEyeOpen,
                            rightEyeOpen = gaze.RightEyeOpen,
                            timestamp = gaze.Timestamp,
                            userPresent = gaze.UserPresent,
                            fixation = gaze.Fixation,
                        };
                        GazeChangedHandler?.Invoke(this, GazeData);
                    }
                }

            }
            catch (TaskCanceledException) { } //task cancelled
            catch (InvalidOperationException) { } //stream already finished / is closed...
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "Stream in gaze failed");
            }
        }

        /// <summary>
        /// Note: This is a helper class to keep track of the tasks and cancellation tokens
        /// </summary>
        class TrackingTaskInfo
        {
            internal CancellationTokenSource CancellationTokenSource { get; set; }
            internal Task Task { get; set; }
        }
    }
}
