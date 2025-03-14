import React from 'react';
import { useSpotifyIntegration, SpotifyIntegrationProps } from './SpotifyIntegrationLogic';
import SpotifyIntegrationUI from './SpotifyIntegrationUI';

const SpotifyIntegration: React.FC<SpotifyIntegrationProps> = ({ appState, onClose, onSpotifyAction }) => {
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
        cancelOperation
    } = useSpotifyIntegration(appState, onSpotifyAction);

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