import React, { memo } from 'react';
import { View, Text, TouchableOpacity, Platform, ViewProps } from 'react-native';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from '../styles';
import { SongInfo, SpotifyStatus } from '../types';
import SpotifyStatusComponent from './SpotifyStatus';

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
    onToggle: (id: string) => void;
    showSpotifyStatus?: boolean;
}

function formatArtists(artists: string[]): string {
    if (!artists || artists.length === 0) {
        return '[No artists]';
    }

    return artists.join(', ');
}

// Using memo to prevent unnecessary re-renders
const SongItem = memo(({ item, isSelected, onToggle, showSpotifyStatus = false }: SongItemProps) => {
    // Defensive checks for item properties
    const safeItem = {
        Id: item?.Id || '',
        Name: item?.Name || '[No title]',
        Path: item?.Path || '',
        Artist: Array.isArray(item?.Artist) ? item.Artist : [], // Keep the original array structure
        Album: item?.Album || '[No album]',
        Rating: typeof item?.Rating === 'number' ? item.Rating : 0,
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

    return (
        <TooltipView
            style={styles.fileItem}
            tooltip={safeItem.Path}
        >
            <View style={styles.songItemContainer}>
                <TouchableOpacity
                    style={styles.leftCheckboxContainer}
                    onPress={handleToggle}>
                    <FontAwesomeIcon
                        name={isSelected ? 'check-square-o' : 'square-o'}
                        style={[styles.checkboxIcon, isSelected ? styles.checkboxIconChecked : null]} />
                </TouchableOpacity>

                <View style={[
                    styles.songContentContainer,
                    showSpotifyStatus && styles.withSpotifyStatus, // Make space for the status when it's shown
                ]}>
                    <TouchableOpacity onPress={handleToggle} style={styles.songContent}>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                <Text style={styles.labelText}>Artist: </Text>
                                {formatArtists(safeItem.Artist)}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                <Text style={styles.labelText}>Title: </Text>
                                {safeItem.Name}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                <Text style={styles.labelText}>Album: </Text>
                                {safeItem.Album}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <View style={styles.ratingRow}>
                                <Text style={styles.labelText}>Rating:</Text>
                                {renderStars(safeItem.Rating)}
                            </View>
                        </View>
                    </TouchableOpacity>
                </View>
                <SpotifyStatusComponent item={safeItem} showStatus={showSpotifyStatus} />
            </View>
        </TooltipView>
    );
});

export default SongItem;
