import React, { memo } from 'react';
import { View, Text, TouchableOpacity, Platform } from 'react-native';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from '../styles';
import { SongInfo } from '../types';

// Windows-specific tooltip support
const TooltipView = (props) => {
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
}

// Using memo to prevent unnecessary re-renders
const SongItem = memo(({ item, isSelected, onToggle }: SongItemProps) => {
    const renderStars = (rating: number) => (
        <View style={styles.starsContainer}>
            {[...Array(rating)].map((_, index) => (
                <FontAwesomeIcon key={index} name="star" style={styles.starIcon} />
            ))}
        </View>
    );

    const handleToggle = () => {
        onToggle(item.id);
    };

    return (
        <TooltipView
            style={styles.fileItem}
            tooltip={item.path}
        >
            <View style={styles.songItemContainer}>
                {/* Checkbox on the left */}
                <TouchableOpacity
                    style={styles.leftCheckboxContainer}
                    onPress={handleToggle}
                >
                    <FontAwesomeIcon
                        name={isSelected ? 'check-square-o' : 'square-o'}
                        style={[styles.checkboxIcon, isSelected ? styles.checkboxIconChecked : null]}
                    />
                </TouchableOpacity>

                {/* Content area */}
                <View style={styles.songContentContainer}>
                    <TouchableOpacity onPress={handleToggle} style={styles.songContent}>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                <Text style={styles.labelText}>Artist: </Text>
                                {item.artist?.length ? item.artist.join(', ') : '[No artist]'}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                <Text style={styles.labelText}>Title: </Text>
                                {item.name}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                <Text style={styles.labelText}>Album: </Text>
                                {item.album}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <View style={styles.ratingRow}>
                                <Text style={styles.labelText}>Rating:</Text>
                                {renderStars(item.rating)}
                            </View>
                        </View>
                    </TouchableOpacity>
                </View>
            </View>
        </TooltipView>
    );
});

export default SongItem;