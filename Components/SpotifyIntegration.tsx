import React from 'react';
import { useSpotifyIntegration, SpotifyIntegrationProps } from './SpotifyIntegrationLogic';
import SpotifyIntegrationUI from './SpotifyIntegrationUI';

const SpotifyIntegration: React.FC<SpotifyIntegrationProps> = ({ appState, onClose, onSpotifyAction }) => {
    // Make sure appState is never undefined
    const safeAppState = appState || {
        songs: {},
        chosenSongs: [],
        minimumRating: 0,
    };

    const {
        isAuthenticated,
        isAuthenticating,
        isProcessing,
        progress,
        errors,
        operationRunning,
        handleAuthenticate,
        handleLikeSongs,
        handleFollowArtists,
        cancelOperation,
    } = useSpotifyIntegration(safeAppState, onSpotifyAction);

    return (
        <SpotifyIntegrationUI
            isAuthenticated={isAuthenticated}
            isAuthenticating={isAuthenticating}
            isProcessing={isProcessing}
            progress={progress}
            operationRunning={operationRunning}
            errors={errors}
            onAuthenticate={handleAuthenticate}
            onLikeSongs={handleLikeSongs}
            onFollowArtists={handleFollowArtists}
            onCancelOperation={cancelOperation}
            onClose={onClose}
        />
    );
};

export default SpotifyIntegration;
