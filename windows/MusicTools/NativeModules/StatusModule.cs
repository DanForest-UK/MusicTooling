using Microsoft.ReactNative.Managed;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static MusicTools.Core.Types;
using MusicTools.Logic;
using LanguageExt;
using static LanguageExt.Prelude;
using System.ComponentModel.Design;

namespace MusicTools.NativeModules
{
    /// <summary>
    /// Module that provides status updates to the React Native UI
    /// </summary>
    [ReactModule("StatusModule")]
    public sealed class StatusModule : IDisposable
    {
        // Event for status updates
        const string STATUS_UPDATE_EVENT = "statusUpdate";

        // Queue to hold status messages
        static readonly ConcurrentQueue<StatusMessage> statusQueue = new ConcurrentQueue<StatusMessage>();

        // Last message sent, for new subscribers
        static StatusMessage lastMessage = StatusMessage.Create("", StatusLevel.Info);

        // ReactContext instance
        ReactContext reactContext;

        // Initialize method called by React Native
        [ReactInitializer]
        public void Initialize(ReactContext reactContext) =>
            this.reactContext = reactContext;
      
        /// <summary>
        /// Adds a new status message to the queue
        /// </summary>
        public static Unit AddStatus(string message, StatusLevel level = StatusLevel.Info)
        {
            var statusMessage = new StatusMessage(message, level, Guid.NewGuid(), DateTime.UtcNow);
            statusQueue.Enqueue(statusMessage);
            lastMessage = statusMessage;
            return unit;
        }

        /// <summary>
        /// Returns the most recent status message - implementing IReactPromise
        /// </summary>
        [ReactMethod("GetCurrentStatus")]
        public void GetCurrentStatus(IReactPromise<string> promise)
        {
            try
            {
                var statusJson = JsonConvert.SerializeObject(lastMessage);
                promise.Resolve(statusJson);
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error getting current status", ex);
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Registers a status change listener - implementing IReactPromise
        /// </summary>
        [ReactMethod("AddListener")]
        public void AddListener(string listenerId, IReactPromise<string> promise)
        {
            try
            {
                // Send the last message to the new subscriber
                EmitStatusUpdate(lastMessage);
                promise.Resolve("Listener added successfully");
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error adding listener", ex);
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Processes the queue and emits events for new status messages
        /// </summary>
        void EmitStatusUpdate(StatusMessage message)
        {
            if ((object)reactContext != null)
            {
                try
                {
                    reactContext.EmitJSEvent(
                        "RCTDeviceEventEmitter",
                        STATUS_UPDATE_EVENT,
                        JsonConvert.SerializeObject(message));
                }
                catch (Exception ex)
                {
                    Runtime.Error($"Error emitting status update", ex);
                }
            }
        }

        /// <summary>
        /// Background task to process the status queue
        /// </summary>
        Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (statusQueue.TryDequeue(out var message))
                    {
                        EmitStatusUpdate(message);
                    }

                    await Task.Delay(100, cancellationToken);
                }
            }, cancellationToken);
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        Task queueProcessingTask;

        /// <summary>
        /// Starts processing the status queue - implementing IReactPromise
        /// </summary>
        [ReactMethod("StartStatusUpdates")]
        public void StartStatusUpdates(IReactPromise<string> promise)
        {
            try
            {
                // Only start if not already running
                if (queueProcessingTask == null || queueProcessingTask.IsCompleted)
                {
                    queueProcessingTask = ProcessQueueAsync(cts.Token);
                    promise.Resolve("Status updates started");
                }
                else
                {
                    promise.Resolve("Status updates already running");
                }
            }
            catch (Exception ex)
            {
                Runtime.Error($"Error starting status updates", ex);
                promise.Reject(new ReactError { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}