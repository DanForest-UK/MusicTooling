import React, { memo } from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from '../styles';
import { SongInfo } from '../types';

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
        <View style={styles.fileItem}>
            <View style={styles.fileHeaderContainer}>
                <TouchableOpacity
                    style={styles.checkboxContainer}
                    onPress={handleToggle}
                >
                    <FontAwesomeIcon
                        name={isSelected ? 'check-square-o' : 'square-o'}
                        style={[styles.checkboxIcon, isSelected ? styles.checkboxIconChecked : null]}
                    />
                    <Text style={styles.fileHeaderText}>
                        {isSelected ? 'Selected' : 'Not Selected'}
                    </Text>
                </TouchableOpacity>
            </View>
            <TouchableOpacity onPress={handleToggle}>
                <View style={styles.fileTextContainer}>
                    <Text style={styles.fileText}>
                        Artist: {item.artist?.length ? item.artist.join(', ') : '[No artist]'}
                    </Text>
                </View>
                <View style={styles.fileTextContainer}>
                    <Text style={styles.fileText}>Title: {item.name}</Text>
                </View>
                <View style={styles.fileTextContainer}>
                    <Text style={styles.fileText}>Album: {item.album}</Text>
                </View>
                <View style={styles.fileTextContainer}>
                    <View style={styles.ratingRow}>
                        <Text style={styles.fileText}>Rating:</Text>
                        {renderStars(item.rating)}
                    </View>
                </View>
                <View style={styles.fileTextContainer}>
                    <Text style={styles.fileText}>Path: {item.path}</Text>
                </View>
            </TouchableOpacity>
        </View>
    );
});

export default SongItem;
