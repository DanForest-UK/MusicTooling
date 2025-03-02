using System;

namespace MusicTools.Core
{
    /// <summary>
    /// Configuration class for Spotify API credentials
    /// </summary>
    public class SpotifyConfig
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string RedirectUri { get; }
        public int Port { get; }

        public SpotifyConfig(string clientId, string clientSecret, string redirectUri, int port)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            if (string.IsNullOrEmpty(clientSecret))
                throw new ArgumentNullException(nameof(clientSecret));

            if (string.IsNullOrEmpty(redirectUri))
                throw new ArgumentNullException(nameof(redirectUri));

            ClientId = clientId;
            ClientSecret = clientSecret;
            RedirectUri = redirectUri;
            Port = port;
        }

        /// <summary>
        /// Creates a default configuration using environment variables if available
        /// </summary>
        public static SpotifyConfig CreateDefault()
        {
            // Try to get from environment variables first
            string clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID") ?? "a53ac9883ecd4a4da3f3b40c7588585c";
            string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET") ?? "9aac6c7555934655b601e4598f4b715b";
            string redirectUri = Environment.GetEnvironmentVariable("SPOTIFY_REDIRECT_URI") ?? "https://127.0.0.1:8000/callback";
            int port = int.TryParse(Environment.GetEnvironmentVariable("SPOTIFY_PORT"), out int p) ? p : 8000;

            return new SpotifyConfig(clientId, clientSecret, redirectUri, port);
        }
    }
}