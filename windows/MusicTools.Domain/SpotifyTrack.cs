namespace MusicTools.Domain
{
    public record SpotifyTrack(
           SpotifySongId Id,
           string Name,
           SpotifyArtist[] Artists,
           string Uri);
}
