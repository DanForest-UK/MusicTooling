using Microsoft.ReactNative;
using Microsoft.ReactNative.Managed;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace MusicTools.NativeModules
{
    /// <summary>
    /// Helper class for emitting events to JavaScript
    /// </summary>
    public static class JsEmitterHelper
    {
        /// <summary>
        /// Emits an event to the React Native JavaScript side
        /// </summary>
        /// <param name="reactContext">The React context to use for emitting events</param>
        /// <param name="eventName">Name of the event to emit</param>
        /// <param name="data">Data to send with the event (will be serialized to JSON)</param>
        public static void EmitEvent(ReactContext reactContext, string eventName, object data)
        {
            try
            {
                // Skip if context is null (can happen during initialization or cleanup)
                if ((object)reactContext == null)
                {
                    Debug.WriteLine($"Cannot emit event {eventName}: reactContext is null");
                    return;
                }

                // Serialize data to JSON with proper casing preserved
                string jsonData;
                try
                {
                    // Use JsonSerializerSettings to preserve original property casing
                    jsonData = JsonConvert.SerializeObject(data);

                    Debug.WriteLine($"Serialized event data for {eventName}: {jsonData}");

                }
                catch (Exception serEx)
                {
                    Debug.WriteLine($"Error serializing event data for {eventName}: {serEx.Message}");

                    // Use a simplified fallback object if serialization fails
                    jsonData = JsonConvert.SerializeObject(new
                    {
                        error = $"Error serializing event data: {serEx.Message}",
                        simplifiedData = true
                    });
                }

                // Emit the event to JavaScript
                reactContext.EmitJSEvent(
                    "RCTDeviceEventEmitter",
                    eventName,
                    jsonData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error emitting event {eventName}: {ex.Message}");
            }
        }
    }
}