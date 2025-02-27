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
using MusicTools.Logic;

namespace MusicTools.NativeModules
{
    [ReactModule("StateModule")]
    public sealed class StateModule : IDisposable
    {
        private ReactContext reactContext;

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
        }

        // Initialize will be called by the React Native runtime
        [ReactInitializer]
        public void Initialize(ReactContext reactContext)
        {
            this.reactContext = reactContext;
        }

        [ReactMethod("addListener")]
        public void AddListener(string eventName)
        {
            // No-op implementation - just needs to exist for React Native
        }

        [ReactMethod("removeListeners")]
        public void RemoveListeners(int count)
        {
            // No-op implementation - just needs to exist for React Native
        }

        // todo do we need this
        private void OnStateChanged(object sender, AppModel state)
        {}

        [ReactMethod("GetCurrentState")]
        public async Task<string> GetCurrentState() =>
            JsonConvert.SerializeObject(ObservableState.Current, _jsonSettings);


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