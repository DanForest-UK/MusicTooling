import React from 'react';
import { View, Text } from 'react-native';
import { styles } from '../styles';
import { SongInfo, SpotifyStatus } from '../types';

interface SpotifyStatusProps {
    item: SongInfo;
    showStatus: boolean;
}

// This component is no longer needed since the status is now displayed in the table
// But kept for backwards compatibility or future use
const SpotifyStatusComponent: React.FC<SpotifyStatusProps> = ({ item, showStatus }) => {
    if (!showStatus) {
        return null;
    }

    // Defensive checks for item properties
    const songStatus = typeof item?.SongStatus === 'number' ? item.SongStatus : SpotifyStatus.NotSearched;
    const artistStatus = typeof item?.ArtistStatus === 'number' ? item.ArtistStatus : SpotifyStatus.NotSearched;

    const getSongStatusText = (status: number) => {
        switch (status) {
            case SpotifyStatus.NotSearched:
                return 'Song not searched';
            case SpotifyStatus.Found:
                return 'Song found';
            case SpotifyStatus.NotFound:
                return 'Song not found';
            case SpotifyStatus.Liked:
                return 'Song liked';
            default:
                return 'Unknown status';
        }
    };

    const getArtistStatusText = (status: number) => {
        switch (status) {
            case SpotifyStatus.NotSearched:
                return 'Artist not searched';
            case SpotifyStatus.Found:
                return 'Artist found';
            case SpotifyStatus.NotFound:
                return 'Artist not found';
            case SpotifyStatus.Liked:
                return 'Artist followed';
            default:
                return 'Unknown status';
        }
    };

    const isSuccessStatus = (status: number) => {
        return status === SpotifyStatus.Found || status === SpotifyStatus.Liked;
    };

    const isErrorStatus = (status: number) => {
        return status === SpotifyStatus.NotFound;
    };

    return (
        <View style={styles.spotifyStatusContainer}>
            <Text style={[
                styles.spotifyStatusText,
                isSuccessStatus(songStatus) ? styles.spotifyStatusSuccess :
                    isErrorStatus(songStatus) ? styles.spotifyStatusError : null
            ]}>
                {getSongStatusText(songStatus)}
            </Text>

            <Text style={[
                styles.spotifyStatusText,
                styles.spotifyStatusArtistText,
                isSuccessStatus(artistStatus) ? styles.spotifyStatusSuccess :
                    isErrorStatus(artistStatus) ? styles.spotifyStatusError : null
            ]}>
                {getArtistStatusText(artistStatus)}
            </Text>
        </View>
    );
};

export default SpotifyStatusComponent;