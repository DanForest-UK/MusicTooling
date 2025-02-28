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
        // Field to hold the React context
        ReactContext reactContext;

        // Static JsonSerializerSettings with a custom contract resolver
        static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new ForceCamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include
        };

        /// <summary>
        /// Default constructor for React Native code generation
        /// </summary>
        public StateModule()
        {}

        /// <summary>
        /// Initialize method called by React Native runtime
        /// </summary>
        [ReactInitializer]
        public void Initialize(ReactContext reactContext) =>
            this.reactContext = reactContext;
  
        /// <summary>
        /// Returns the current application state as JSON
        /// </summary>
        [ReactMethod("GetCurrentState")]
        public Task<string> GetCurrentState()
        {
            try
            {
                return Task.FromResult(JsonConvert.SerializeObject(ObservableState.Current, jsonSettings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting state: {ex.Message}");
                return Task.FromResult("{}");
            }
        }

        /// <summary>
        /// Updates the minimum rating filter
        /// </summary>
        [ReactMethod("SetMinimumRating")]
        public void SetMinimumRating(int rating) =>
            ObservableState.SetMinimumRating(rating);

        /// <summary>
        /// Cleanup resources when component is disposed
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Enhanced contract resolver that forces camelCase property names, 
    /// even for record types where the standard resolver often fails
    /// </summary>
    public class ForceCamelCasePropertyNamesContractResolver : DefaultContractResolver
    {
        readonly CamelCaseNamingStrategy namingStrategy = new CamelCaseNamingStrategy();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            // Force camelCase for all property names
            foreach (var prop in properties)
                prop.PropertyName = namingStrategy.GetPropertyName(prop.PropertyName, false);

            return properties;
        }
    }
}