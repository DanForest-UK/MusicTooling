using Microsoft.ReactNative.Managed;
using MusicTools.Core;
using static MusicTools.Core.Types;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.ReactNative;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace MusicTools.NativeModules
{
    [ReactModule("StateModule")]
    public sealed class StateModule : IDisposable
    {
        private ReactContext _reactContext;

        // Static JsonSerializerSettings with a custom contract resolver
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new ForceCamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include
        };

        // Default parameterless constructor for React Native code gen
        public StateModule()
        {
            ObservableState.StateChanged += OnStateChanged;
            Console.WriteLine("StateModule: Default constructor called");
        }

        // Initialize will be called by the React Native runtime
        [ReactInitializer]
        public void Initialize(ReactContext reactContext)
        {
            _reactContext = reactContext;
            Console.WriteLine("StateModule: Initialize called with ReactContext");
        }

        [ReactMethod("addListener")]
        public void AddListener(string eventName)
        {
            // No-op implementation - just needs to exist for React Native
            Console.WriteLine($"StateModule: addListener called for {eventName}");
        }

        [ReactMethod("removeListeners")]
        public void RemoveListeners(int count)
        {
            // No-op implementation - just needs to exist for React Native
            Console.WriteLine($"StateModule: removeListeners called with count {count}");
        }

        // todo do we need this
        private void OnStateChanged(object sender, AppModel state)
        {
            try
            {
                if ((object)_reactContext != null)
                {
                    Console.WriteLine("StateModule: State changed, will update on next GetCurrentState call");
                }
                else
                {
                    Console.WriteLine("StateModule: Cannot handle state change - ReactContext is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnStateChanged: {ex.Message}");
            }
        }

        [ReactMethod("GetCurrentState")]
        public async Task<string> GetCurrentState()
        {
            var state = ObservableState.Current;

            Console.WriteLine($"GetCurrentState: Songs count: {state.Songs?.Length ?? 0}");

            // Serialize with our custom contract resolver
            string jsonResult = JsonConvert.SerializeObject(state, _jsonSettings);

            // Print serialized JSON to verify property names
            Console.WriteLine($"GetCurrentState: Serialized JSON: {jsonResult.Substring(0, Math.Min(200, jsonResult.Length))}");

            return jsonResult;
        }

        [ReactMethod("SetMinimumRating")]
        public void SetMinimumRating(int rating)
        {
            ObservableState.SetMinimumRating(rating);
        }

        public void Dispose()
        {
            ObservableState.StateChanged -= OnStateChanged;
        }
    }

    /// <summary>
    /// Enhanced contract resolver that forces camelCase property names, 
    /// even when standard CamelCasePropertyNamesContractResolver isn't working
    /// with certain types like records.
    /// </summary>
    public class ForceCamelCasePropertyNamesContractResolver : DefaultContractResolver
    {
        private readonly CamelCaseNamingStrategy _namingStrategy = new CamelCaseNamingStrategy();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            // Get properties from base implementation
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            // Force camelCase for all property names
            foreach (var prop in properties)
            {
                prop.PropertyName = _namingStrategy.GetPropertyName(prop.PropertyName, false);
            }

            return properties;
        }
    }
}