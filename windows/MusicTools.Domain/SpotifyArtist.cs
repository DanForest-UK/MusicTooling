using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    public record SpotifyArtist(
            SpotifyArtistId Id,
            string Name,
            string Uri);
}
