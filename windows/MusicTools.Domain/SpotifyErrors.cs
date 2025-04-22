using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicTools.Domain
{
    // Base record for all Spotify errors
    public record SpotifyError(string ErrorCode, string Message, string ResourceId);

    // Specific error types 
    public record AlreadyAuthenticated()
        : SpotifyError("ALREADY_AUTHENTICATED", "Already authenticated", "authentication");

    public record SongNotFound(SongId TrackId, SongName Title, Artist[] Artists, string ErrorMessage)
        : SpotifyError("SONG_NOT_FOUND", $"Could not find song: {Title} by {string.Join(", ", Artists.Select(a => a.Value))}", TrackId.ToString());


    public record ArtistNotFound(Artist ArtistName, string ErrorMessage)
        : SpotifyError("ARTIST_NOT_FOUND", $"Could not find artist: {ArtistName}", ArtistName.Value);

    public record AuthenticationError(string ErrorMessage)
        : SpotifyError("AUTH_ERROR", $"Authentication failed: {ErrorMessage}", "auth");

    public record RateLimitError(string Resource)
        : SpotifyError("RATE_LIMIT", $"Rate limit exceeded for {Resource}.", Resource);

    public record ApiError(string Resource, int StatusCode, string ErrorMessage)
        : SpotifyError("API_ERROR", $"API error ({StatusCode}): {ErrorMessage}", Resource);
}
