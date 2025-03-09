import React from 'react';
import { useSpotifyIntegration, SpotifyIntegrationProps } from './SpotifyIntegrationLogic';
import SpotifyIntegrationUI from './SpotifyIntegrationUI';

const SpotifyIntegration: React.FC<SpotifyIntegrationProps> = ({ appState, onClose, onSpotifyAction }) => {
    const {
        isAuthenticated,
        isAuthenticating,
        isProcessing,
        errors,
        handleAuthenticate,
        handleLikeSongs,
        handleFollowArtists
    } = useSpotifyIntegration(appState, onSpotifyAction);

    return (
        <SpotifyIntegrationUI
            isAuthenticated={isAuthenticated}
            isAuthenticating={isAuthenticating}
            isProcessing={isProcessing}
            errors={errors}
            selectedSongsCount={appState.chosenSongs.length}
            onAuthenticate={handleAuthenticate}
            onLikeSongs={handleLikeSongs}
            onFollowArtists={handleFollowArtists}
            onClose={onClose}
        />
    );
};

export default SpotifyIntegration;