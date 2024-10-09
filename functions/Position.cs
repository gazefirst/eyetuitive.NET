using GazeFirst;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazeFirst.functions
{
    /// <summary>
    /// Position
    /// </summary>
    public class Position : ClientBased
    {
        private event EventHandler<PositionEventArgs> PositionChanged;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<EventHandler<PositionEventArgs>> _subscribers = new List<EventHandler<PositionEventArgs>>();
        private bool taskRunning = false;

        /// <summary>
        /// Position constructor
        /// </summary>
        /// <param name="_client"></param>
        internal Position(Eyetracker.EyetrackerClient _client) : base(_client) { }

        /// <summary>
        /// Start position tracking
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="positionChangedHandler"></param>
        public void StartPositionTracking(EventHandler<PositionEventArgs> positionChangedHandler)
        {
            PositionChanged += positionChangedHandler;
            _subscribers.Add(positionChangedHandler);

            if (_subscribers.Count > 0 && !taskRunning)
            {
                // Start the task only if this is the first subscriber
                Task.Run(() => GetPositionAsync(_cts.Token), _cts.Token);                
            }
        }

        /// <summary>
        /// Stop position tracking
        /// </summary>
        /// <param name="positionChangedHandler"></param>
        public void StopPositionTracking(EventHandler<PositionEventArgs> positionChangedHandler)
        {
            PositionChanged -= positionChangedHandler;
            _subscribers.Remove(positionChangedHandler);

            if (_subscribers.Count == 0)
            {
                // Stop the task if there are no more subscribers
                _cts.Cancel();
                _cts = new CancellationTokenSource(); // Reset the CancellationTokenSource for future use
            }
        }

        /// <summary>
        /// Reconnect to the eye tracker and restart all the tracking tasks
        /// </summary>
        /// <param name="client"></param>
        internal override void Reconnect()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource(); // Reset the CancellationTokenSource for future use
            if (_subscribers.Count > 0)
            {
                Task.Run(() => GetPositionAsync(_cts.Token), _cts.Token);
            }
        }

        /// <summary>
        /// Get position data from the eye tracker
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task GetPositionAsync(CancellationToken cancellationToken)
        {
            taskRunning = true;
            try
            {
                var posstream = _client.SubscribePositioning(new Google.Protobuf.WellKnownTypes.Empty());
                while (await posstream.ResponseStream.MoveNext(cancellationToken))
                {
                    if (posstream.ResponseStream.Current != null)
                    {
                        var pos = posstream.ResponseStream.Current;
                        NormedPoint2d left = new NormedPoint2d(pos.LeftEyePos.X, pos.LeftEyePos.Y);
                        NormedPoint2d right = new NormedPoint2d(pos.RightEyePos.X, pos.RightEyePos.Y);
                        var positionData = new PositionEventArgs()
                        {
                            depthInMM = pos.DepthInMM,
                            leftEyePos = left,
                            rightEyePos = right,
                            isLeftEyeOpen = !pos.LeftEyeClosed,
                            isRightEyeOpen = !pos.RightEyeClosed,
                            gazeIsPaused = pos.GazeIsPaused
                        };
                        OnPositionChanged(positionData);
                    }
                }
            }
            catch (TaskCanceledException) { } //task cancelled
            catch (InvalidOperationException) { } //stream already finished / is closed...
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "Stream in position failed");
            }
            finally
            {
                taskRunning = false;
            }
        }

        /// <summary>
        /// OnPositionChanged
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPositionChanged(PositionEventArgs e)
        {
            PositionChanged?.Invoke(this, e);
        }
    }
}
