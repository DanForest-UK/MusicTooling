// SongItem.tsx (updated)
import React, { memo } from 'react';
import { View, Text, TouchableOpacity, Platform, ViewProps } from 'react-native';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from '../styles';
import {
    SongInfo,
    SpotifyStatus,
  
} from '../types';

// Define the props type for TooltipView
interface TooltipViewProps extends ViewProps {
    tooltip?: string;
    children?: React.ReactNode;
}

// Windows-specific tooltip support
const TooltipView = (props: TooltipViewProps) => {
    const viewProps = { ...props };

    // For Windows platform, add the tooltip property
    if (Platform.OS === 'windows') {
        viewProps.tooltip = props.tooltip;
    }

    return <View {...viewProps}>{props.children}</View>;
};

interface SongItemProps {
    item: SongInfo;
    isSelected: boolean;
    onToggle: (id: number) => void;
    showSpotifyStatus?: boolean;
}

// Using memo to prevent unnecessary re-renders
const SongItem = memo(({ item, isSelected, onToggle, showSpotifyStatus = false }: SongItemProps) => {
    // Defensive checks for item properties
    const safeItem = {
        Id: item?.Id?.Value,
        Name: item?.Name?.Value,
        Path: item?.Path?.Value,
        Artist: Array.isArray(item?.Artist) ? item.Artist.map(a => a.Value) : [],
        Album: item?.Album?.Value,
        Rating: item?.Rating?.Value,
        SongStatus: typeof item?.SongStatus === 'number' ? item.SongStatus : SpotifyStatus.NotSearched,
        ArtistStatus: typeof item?.ArtistStatus === 'number' ? item.ArtistStatus : SpotifyStatus.NotSearched,
    };

    const renderStars = (rating: number) => (
        <View style={styles.starsContainer}>
            {[...Array(rating)].map((_, index) => (
                <FontAwesomeIcon key={index} name="star" style={styles.starIcon} />
            ))}
        </View>
    );

    const handleToggle = () => {
        if (safeItem.Id) {
            onToggle(safeItem.Id);
        }
    };

    const getSongStatusText = (status: number) => {
        switch (status) {
            case SpotifyStatus.NotSearched:
                return 'Not searched';
            case SpotifyStatus.Found:
                return 'Found';
            case SpotifyStatus.NotFound:
                return 'Not found';
            case SpotifyStatus.Liked:
                return 'Liked';
            default:
                return 'Unknown';
        }
    };

    const getArtistStatusText = (status: number) => {
        switch (status) {
            case SpotifyStatus.NotSearched:
                return 'Not searched';
            case SpotifyStatus.Found:
                return 'Found';
            case SpotifyStatus.NotFound:
                return 'Not found';
            case SpotifyStatus.Liked:
                return 'Followed';
            default:
                return 'Unknown';
        }
    };

    const isSuccessStatus = (status: number) => {
        return status === SpotifyStatus.Found || status === SpotifyStatus.Liked;
    };

    const isErrorStatus = (status: number) => {
        return status === SpotifyStatus.NotFound;
    };

    return (
        <TooltipView
            style={styles.tableRow}
            tooltip={safeItem.Path}
        >
            {/* Checkbox Column */}
            <TouchableOpacity
                style={styles.tableCheckboxCell}
                onPress={handleToggle}>
                <FontAwesomeIcon
                    name={isSelected ? 'check-square-o' : 'square-o'}
                    style={[styles.checkboxIcon, isSelected ? styles.checkboxIconChecked : null]} />
            </TouchableOpacity>

            {/* Artist Column */}
            <TouchableOpacity
                style={styles.tableArtistCell}
                onPress={handleToggle}>
                <Text style={styles.tableCellText} numberOfLines={1} ellipsizeMode="tail">
                    {safeItem.Artist.join(' ,')}
                </Text>
            </TouchableOpacity>

            {/* Album Column */}
            <TouchableOpacity
                style={styles.tableAlbumCell}
                onPress={handleToggle}>
                <Text style={styles.tableCellText} numberOfLines={1} ellipsizeMode="tail">
                    {safeItem.Album}
                </Text>
            </TouchableOpacity>

            {/* Title Column */}
            <TouchableOpacity
                style={styles.tableTitleCell}
                onPress={handleToggle}>
                <Text style={styles.tableCellText} numberOfLines={1} ellipsizeMode="tail">
                    {safeItem.Name}
                </Text>
            </TouchableOpacity>

            {/* Rating Column */}
            <TouchableOpacity
                style={styles.tableRatingCell}
                onPress={handleToggle}>
                {renderStars(safeItem.Rating)}
            </TouchableOpacity>

            {/* Spotify Status Columns - Only visible when showSpotifyStatus is true */}
            {showSpotifyStatus && (
                <>
                    {/* Song Status Column */}
                    <View style={styles.tableStatusCell}>
                        <Text style={[
                            styles.tableCellText,
                            isSuccessStatus(safeItem.SongStatus) ? styles.spotifyStatusSuccess :
                                isErrorStatus(safeItem.SongStatus) ? styles.spotifyStatusError : null,
                        ]}>
                            {getSongStatusText(safeItem.SongStatus)}
                        </Text>
                    </View>

                    {/* Artist Status Column */}
                    <View style={styles.tableStatusCell}>
                        <Text style={[
                            styles.tableCellText,
                            isSuccessStatus(safeItem.ArtistStatus) ? styles.spotifyStatusSuccess :
                                isErrorStatus(safeItem.ArtistStatus) ? styles.spotifyStatusError : null,
                        ]}>
                            {getArtistStatusText(safeItem.ArtistStatus)}
                        </Text>
                    </View>
                </>
            )}
        </TooltipView>
    );
});

export default SongItem;