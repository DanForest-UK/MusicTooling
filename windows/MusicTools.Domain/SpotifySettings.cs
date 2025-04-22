using System;
using System.Collections.Generic;
using System.Text;

namespace MusicTools.Domain
{
    public record SpotifySettings(string ClientId, string ClientSecret, int ApiWait);

}
