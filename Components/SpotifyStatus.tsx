import React from 'react';
import { View, Text } from 'react-native';
import { styles } from '../styles';
import { SongInfo, SpotifyStatus } from '../types';

interface SpotifyStatusProps {
    item: SongInfo;
    showStatus: boolean;
}

const SpotifyStatusComponent: React.FC<SpotifyStatusProps> = ({ item, showStatus }) => {
    if (!showStatus) {
        return null;
    }


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
                isSuccessStatus(item.songStatus) ? styles.spotifyStatusSuccess :
                    isErrorStatus(item.songStatus) ? styles.spotifyStatusError : null
            ]}>
                {getSongStatusText(item.songStatus)}
            </Text>

            <Text style={[
                styles.spotifyStatusText,
                styles.spotifyStatusArtistText,
                isSuccessStatus(item.artistStatus) ? styles.spotifyStatusSuccess :
                    isErrorStatus(item.artistStatus) ? styles.spotifyStatusError : null
            ]}>
                {getArtistStatusText(item.artistStatus)}
            </Text>
        </View>
    );
};

export default SpotifyStatusComponent;