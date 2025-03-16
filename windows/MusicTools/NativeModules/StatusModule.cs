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

        // Message throttling parameters
        private const int THROTTLE_INTERVAL_MS = 500; // Minimum time between messages
        private static DateTime lastMessageTime = DateTime.MinValue;
        private static readonly object throttleLock = new object();

        // ReactContext instance
        ReactContext reactContext;

        // Initialize method called by React Native
        [ReactInitializer]
        public void Initialize(ReactContext reactContext) =>
            this.reactContext = reactContext;

        /// <summary>
        /// Adds a new status message to the queue with throttling
        /// </summary>
        public static Unit AddStatus(string message, StatusLevel level)
        {
            // Create status message with unique ID and timestamp
            var statusMessage = new StatusMessage(message, level, Guid.NewGuid(), DateTime.UtcNow);

            // Apply throttling for similar messages to prevent flooding UI
            bool shouldQueue = true;

            lock (throttleLock)
            {
                // Check if we should throttle based on time and content
                var now = DateTime.UtcNow;
                if ((now - lastMessageTime).TotalMilliseconds < THROTTLE_INTERVAL_MS)
                {
                    // If recent message is similar to this one and is the same level, skip it
                    if (lastMessage.Text.StartsWith(message.Substring(0, Math.Min(10, message.Length))) &&
                        lastMessage.Level == level)
                    {
                        // Skip this message since it's too similar to a recent one
                        shouldQueue = false;
                    }
                    else if (message.Contains("Searching for") && lastMessage.Text.Contains("Searching for"))
                    {
                        // Special case: throttle repeated searching messages
                        shouldQueue = false;
                    }
                }

                if (shouldQueue)
                {
                    lastMessageTime = now;
                    statusQueue.Enqueue(statusMessage);
                    lastMessage = statusMessage;
                }

                return unit;
            }
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
        /// Processes the queue and emits events for new status messages using the shared helper
        /// </summary>
        void EmitStatusUpdate(StatusMessage message)
        {
            if ((object)reactContext != null)
            {
                JsEmitterHelper.EmitEvent(reactContext, STATUS_UPDATE_EVENT, JsonConvert.SerializeObject(message));
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

                    // Throttle the processing of the queue
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