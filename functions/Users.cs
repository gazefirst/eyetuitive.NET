using eyetuitive.NET.classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GazeFirst.functions
{
    /// <summary>
    /// Users class
    /// </summary>
    public class Users : ClientBased
    {
        private event EventHandler<UserArgs> userChanged;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<EventHandler<UserArgs>> _subscribers = new List<EventHandler<UserArgs>>();
        private bool taskRunning = false;

        /// <summary>
        /// Users constructor
        /// </summary>
        /// <param name="client"></param>
        internal Users(Eyetracker.EyetrackerClient client) : base(client) { }

        /// <summary>
        /// Start user stream
        /// </summary>
        /// <param name="userHandler"></param>
        public void StartUserChangedStream(EventHandler<UserArgs> userHandler)
        {
            userChanged += userHandler;
            _subscribers.Add(userHandler);

            if (_subscribers.Count > 0 && !taskRunning)
            {
                // Start the task only if this is the first subscriber
                Task.Run(() => StreamUsers(true, _cts.Token), _cts.Token);
            }
        }

        /// <summary>
        /// Stop User stream
        /// </summary>
        /// <param name="userHandler"></param>
        public void StopUserChangedStream(EventHandler<UserArgs> userHandler)
        {
            userChanged -= userHandler;
            _subscribers.Remove(userHandler);

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
                Task.Run(() => StreamUsers(true, _cts.Token), _cts.Token);
            }
        }

        /// <summary>
        /// Create user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public (bool success, UserArgs user) CreateUser(string username)
        {
            if (username == null || username.Length == 0) throw new System.ArgumentException("Username cannot be null or empty", nameof(username));
            try
            {
                var res = _client.ManageUserProfile(new UserProfileRequest()
                {
                    Username = username,
                    Operation = UserProfileRequest.Types.OperationType.Create
                });
                bool success = (res.Status == UserProfileResponse.Types.Status.Success);
                var user = CreateUserArgs(res.User);
                return (success, user);
            }
            catch (System.Exception)
            {
                return (false, new UserArgs());
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool UpdateUser(int ID, string username)
        {
            try
            {
                var res = _client.ManageUserProfile(new UserProfileRequest()
                {
                    UserID = ID,
                    Username = username,
                    Operation = UserProfileRequest.Types.OperationType.Update
                });
                return (res.Status == UserProfileResponse.Types.Status.Success);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public bool DeleteUser(int ID)
        {
            try
            {
                var res = _client.ManageUserProfile(new UserProfileRequest()
                {
                    UserID = ID,
                    Operation = UserProfileRequest.Types.OperationType.Delete
                });
                return (res.Status == UserProfileResponse.Types.Status.Success);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Activate user
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public bool ActivateUser(int ID)
        {
            try
            {
                var res = _client.ManageUserProfile(new UserProfileRequest()
                {
                    UserID = ID,
                    Operation = UserProfileRequest.Types.OperationType.Select
                });
                return (res.Status == UserProfileResponse.Types.Status.Success);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get all users (async)
        /// </summary>
        /// <returns></returns>
        public async Task<List<UserArgs>> GetAllUsersAsync()
        {
            var list = new List<UserArgs>();
            try
            {
                var users = _client.StreamUsers(new StreamUsersRequest() { StayOpen = false });
                while (await users.ResponseStream.MoveNext(default))
                {
                    var response = users.ResponseStream.Current;
                    if (response != null)
                    {
                        UserArgs args = CreateUserArgs(response.User, response.IsActive);
                        list.Add(args);
                    }
                }
            }
            catch (TaskCanceledException) { } //task cancelled
            catch (InvalidOperationException) { } //stream already finished / is closed...
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "User stream failed");
            }
            return list;
        }

        /// <summary>
        /// Stream users
        /// </summary>
        /// <param name="stayOpen"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task StreamUsers(bool stayOpen, CancellationToken token)
        {
            try
            {
                var users = _client.StreamUsers(new StreamUsersRequest() { StayOpen = stayOpen });
                while (await users.ResponseStream.MoveNext(token))
                {
                    var response = users.ResponseStream.Current;
                    if (response != null)
                    {
                        UserArgs args = CreateUserArgs(response.User, response.IsActive);
                        userChanged?.Invoke(this, args);
                    }
                }
            }
            catch (TaskCanceledException) { } //task cancelled
            catch (InvalidOperationException) { } //stream already finished / is closed...
            catch (Exception e)
            {
                eyetuitive._logger?.LogError(e, "User stream failed");
            }
        }

        /// <summary>
        /// Create user args
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static UserArgs CreateUserArgs(User response, bool active = false)
        {
            return new UserArgs()
            {
                UserName = response.Username,
                UserID = response.UserID,
                Active = active,
                UserGuid = Helper.FromByteString(response.Uid)
            };
        }
    }
}
