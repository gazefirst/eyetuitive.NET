using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
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
        private event Action<CalibrationPointUpdateArgs> calibPoint;
        private event Action<CalibrationFinishedArgs> calibResult;

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
        public async Task StartCalibrationAsync(EventHandler<CalibrationPointUpdateArgs> calibPointUpdate, EventHandler<CalibrationFinishedArgs> calibFinished, ScreenDimensions dimensions, CalibrationPoints points = CalibrationPoints.Nine, bool fixationBased = false, bool multipoint = false, bool record = false, bool manualCalibration = false)
        {
            eyetuitive._logger?.LogDebug("Starting calibration");

            calibrationCts.CancelAfter(TimeSpan.FromMinutes(_calibrationTimeoutInMinutes));
            // Create a TaskCompletionSource to provide user input
            var userInputTcs = new TaskCompletionSource<CalibrationControl>();

            calibPoint += (point) =>
            {
                eyetuitive._logger?.LogDebug("Calibration point update: {0}", point.sequenceNumber);
                calibPointUpdate?.Invoke(this, point);
            };

            calibResult += (args) =>
            {
                eyetuitive._logger?.LogDebug("Calibration finished: {0}", args.success);
                calibFinished?.Invoke(this, args);
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
                ManualCalibration = manualCalibration,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
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
        /// Confirm calibration point
        /// </summary>
        public void ConfirmPoint()
        {
            _ = WriteRequest(new CalibrationControl
            {
                Control = CalibrationControl.Types.Control.Confirm
            });
        }

        /// <summary>
        /// Get the last calibration result
        /// </summary>
        /// <returns></returns>
        public CalibrationFinishedArgs GetLastCalibrationFinishedArgs()
        {
            try
            {
                var res = _client.GetLastCalibrationResult(new Google.Protobuf.WellKnownTypes.Empty());
                if (res == null) return null;
                return CreateCalibrationFinishedArgs(res, true);
            }
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "GetLastCalibrationResult failed");
            }
            return new CalibrationFinishedArgs();
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
                                bool isSuccess = calib.Status == CalibrationStatus.Types.Status.Succeeded;
                                CalibrationFinishedArgs res = CreateCalibrationFinishedArgs(calib.CalibrationResult, isSuccess);
                                calibResult?.Invoke(res);
                            }
                        }
                        else
                        {
                            var p = calib.CalibrationPoint.Position;
                            calibPoint?.Invoke(new CalibrationPointUpdateArgs
                            {
                                sequenceNumber = calib.CalibrationPoint.Sequence,
                                target = new NormedPoint2d(p.X, p.Y),
                                state = (CalibrationPointState)(int)calib.CalibrationPoint.State
                            });
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
        /// Helper function to create CalibrationFinishedArgs
        /// </summary>
        /// <param name="result"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        private static CalibrationFinishedArgs CreateCalibrationFinishedArgs(CalibrationResult result, bool success)
        {
            var res = new CalibrationFinishedArgs
            {
                success = success,
                percentageRatingPerPoint = result.PercentageRatings.ToArray(),
                percentageRatingPerPointLeft = result.PercentageRatingsLeft.ToArray(),
                percentageRatingPerPointRight = result.PercentageRatingsRight.ToArray(),
                percentageRatingOverall = result.OverallPercentageRating,
                canImprove = result.CanImprove,
                calibrationId = new Guid(result.Uid.ToArray()),
                timestamp = result.Timestamp,
            };
            return res;
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
