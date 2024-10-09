using GazeFirst;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GazeFirst.functions
{
    /// <summary>
    /// Calibration class
    /// </summary>
    public class Calibration : ClientBased
    {
        private Task _calibrationTask;
        private CancellationTokenSource calibrationCts = new CancellationTokenSource();
        private IAsyncStreamReader<CalibrationStatus> _responseStream;
        private IClientStreamWriter<CalibrationControl> _requestStream;
        private int _calibrationTimeoutInMinutes;
        private event Action<(NormedPoint2d target, CalibrationPointState state, int seq)> calibPoint;
        private event Action<bool, List<int>, int, bool> calibResult;
        
        /// <summary>
        /// Create a new Calibration
        /// </summary>
        /// <param name="client"></param>
        /// <param name="timeoutInMinutes"></param>
        internal Calibration(Eyetracker.EyetrackerClient client, int timeoutInMinutes = 5) : base(client)
        {
            _calibrationTimeoutInMinutes = timeoutInMinutes;
        }

        /// <summary>
        /// Start calibration process
        /// </summary>
        /// <param name="calibPointUpdate"></param>
        /// <param name="calibFinished"></param>
        /// <param name="dimensions"></param>
        /// <param name="points"></param>
        /// <param name="fixationBased"></param>
        /// <param name="multipoint"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public async Task StartCalibrationAsync(EventHandler<CalibrationPointUpdateArgs> calibPointUpdate, EventHandler<CalibrationFinishedArgs> calibFinished, ScreenDimensions dimensions, CalibrationPoints points = CalibrationPoints.Nine, bool fixationBased = false, bool multipoint = false, bool record = false)
        {
            eyetuitive._logger?.LogDebug("Starting calibration");

            calibrationCts.CancelAfter(TimeSpan.FromMinutes(_calibrationTimeoutInMinutes));
            // Create a TaskCompletionSource to provide user input
            var userInputTcs = new TaskCompletionSource<CalibrationControl>();

            calibPoint += (point) =>
            {
                eyetuitive._logger?.LogDebug("Calibration point update: {0}", point.seq);
                calibPointUpdate?.Invoke(this, new CalibrationPointUpdateArgs
                {
                    sequenceNumber = point.seq,
                    target = point.target,
                    state = point.state
                });
            };

            calibResult += (success, perPoint, overall, canImprove) =>
            {
                eyetuitive._logger?.LogDebug("Calibration finished: {0}", success);
                calibFinished?.Invoke(this, new CalibrationFinishedArgs
                {
                    success = success,
                    percentageRatingPerPoint = perPoint.ToArray(),
                    percentageRatingOverall = overall,
                    canImprove = canImprove
                });
            };

            AsyncDuplexStreamingCall<CalibrationControl, CalibrationStatus> cal = _client.Calibrate(cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);
            _requestStream = cal.RequestStream;
            _responseStream = cal.ResponseStream;

            await WriteRequest(new CalibrationControl
            {
                Control = CalibrationControl.Types.Control.Start,
                CalibrationPoints = (CalibrationControl.Types.CalibrationPoints)(int)points,
                Size = new ScreenSize()
                {
                    WidthMm = dimensions.Width,
                    HeightMm = dimensions.Height
                },
                FixationBased = fixationBased,
                Multipoint = multipoint,            
            });

            // Start the calibration task and add it to task
            _calibrationTask = Task.Run(() => RunCalibration(calibrationCts.Token), calibrationCts.Token);
        }

        /// <summary>
        /// Stop calibration process (and cancel)
        /// </summary>
        public void StopCalibration()
        {
            _ = WriteRequest(new CalibrationControl
            {
                Control = CalibrationControl.Types.Control.Stop
            });
            calibrationCts.Cancel();
        }

        /// <summary>
        /// Improve calibration at specific points
        /// </summary>
        /// <param name="points"></param>
        public void Improve(int[] points)
        {
            if (points.Length == 0) return;
            _ = WriteRequest(new CalibrationControl
            {
                Control = CalibrationControl.Types.Control.Improve,
                PointsToImprove = { points }
            });
        }

        /// <summary>
        /// Run calibration response stream and invoke events
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async ValueTask RunCalibration(CancellationToken cancellationToken)
        {
            try
            {
                if (_responseStream == null) return;
                while (await _responseStream.MoveNext(cancellationToken))
                {
                    if (_responseStream.Current != null)
                    {
                        var calib = _responseStream.Current;
                        if (calib.Status == CalibrationStatus.Types.Status.Failed || calib.Status == CalibrationStatus.Types.Status.Succeeded)
                        {
                            if (calib.CalibrationResult != null)
                            {
                                List<int> perPoint = calib.CalibrationResult.PercentageRatings.ToList();
                                int total = calib.CalibrationResult.OverallPercentageRating;
                                calibResult?.Invoke(calib.Status == CalibrationStatus.Types.Status.Succeeded, perPoint, total, calib.CalibrationResult.CanImprove);
                            }
                        }
                        else
                        {
                            NormedPoint2d calibP = new NormedPoint2d(calib.CalibrationPoint.Position.X, calib.CalibrationPoint.Position.Y);
                            CalibrationPointState state = (CalibrationPointState)(int)calib.CalibrationPoint.State;
                            int seq = calib.CalibrationPoint.Sequence;
                            calibPoint?.Invoke((calibP, state, seq));
                        }
                    }
                }
            }
            catch (TaskCanceledException) { } //calibration cancelled
            catch (InvalidOperationException) { } //stream already finished / is closed...
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "Request stream in Calib failed");
            }
        }

        /// <summary>
        /// Write request to server
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task WriteRequest(CalibrationControl message)
        {
            try
            {
                if (_requestStream != null) await _requestStream.WriteAsync(message);
            }
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "WriteRequest in Calib failed");
            }
        }
    }
}
