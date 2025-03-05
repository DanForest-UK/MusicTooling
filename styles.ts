import { StyleSheet } from 'react-native';

export const styles = StyleSheet.create({
    container: {
        flex: 1,
        padding: 20,
        paddingVertical: 10,
    },
    listContainer: {
        padding: 3,
        paddingRight: 10,
        paddingBottom: 80, // Add padding at bottom for Spotify toggle
    },
    fileItem: {
        marginVertical: 10,
        padding: 10,
        backgroundColor: '#374151',
        borderRadius: 5,
    },
    fileItemSelected: {
        borderWidth: 2,
        borderColor: '#60A5FA',
    },
    fileTextContainer: {
        padding: 5,
        marginVertical: 2,
        backgroundColor: '#4B5563',
        borderRadius: 3,
    },
    fileHeaderContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        padding: 5,
        marginBottom: 5,
        backgroundColor: '#2D3748',
        borderRadius: 3,
    },
    fileHeaderText: {
        color: 'white',
        fontWeight: 'bold',
        marginLeft: 10,
    },
    fileText: {
        color: 'white',
    },
    activityIndicator: {
        marginTop: 20,
        alignSelf: 'center',
    },
    emptyText: {
        marginTop: 20,
        fontSize: 16,
        color: 'gray',
        textAlign: 'center',
        fontStyle: 'italic',
    },
    loadingOverlay: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
    },
    loadingText: {
        marginTop: 10,
        fontSize: 18,
        color: '#ffffff',
    },
    controlsContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        marginBottom: 10,
    },
    buttonWrapper: {
        flex: 1,
        marginRight: 10,
    },
    starsContainer: {
        flexDirection: 'row',
        marginLeft: 5,
    },
    starIcon: {
        fontSize: 16,
        color: '#F4D03F',
        marginHorizontal: 2,
    },
    ratingRow: {
        flexDirection: 'row',
        alignItems: 'center',
    },
    pickerLabel: {
        fontSize: 16,
        color: 'white',
        marginRight: 10,
    },
    picker: {
        height: 40,
        width: 150,
        color: 'white',
        backgroundColor: '#4B5563',
        borderRadius: 5,
        paddingHorizontal: 10,
    },
    songSwitch: {
        transform: [{ scaleX: 0.8 }, { scaleY: 0.8 }],
    },
    statsContainer: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 10,
        padding: 8,
        backgroundColor: '#2D3748',
        borderRadius: 5,
    },
    statsText: {
        color: 'white',
        fontSize: 12,
    },
    // New styles for the redesigned SongItem
    songItemContainer: {
        flexDirection: 'row',
        alignItems: 'stretch',
    },
    leftCheckboxContainer: {
        width: 40,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#2D3748',
        borderTopLeftRadius: 3,
        borderBottomLeftRadius: 3,
    },
    songContentContainer: {
        flex: 1,
    },
    songContent: {
        flex: 1,
    },
    labelText: {
        fontWeight: 'bold',
        color: 'white',
    },
    checkboxContainer: {
        flexDirection: 'row',
        alignItems: 'center',
    },
    checkboxIcon: {
        fontSize: 20,
        color: '#9CA3AF',
    },
    checkboxIconChecked: {
        color: '#60A5FA',
    },

    // New style for Spotify toggle container
    spotifyToggleContainer: {
        position: 'absolute',
        bottom: 0,
        left: 0,
        right: 0,
        padding: 10,
        backgroundColor: '#2D3748',
        borderTopLeftRadius: 10,
        borderTopRightRadius: 10,
        alignItems: 'center',
        zIndex: 50,
    },

    // Spotify integration styles - adjusted for new toggle position
    spotifyContainer: {
        position: 'absolute',
        left: 0,
        right: 0,
        bottom: 0,
        padding: 15,
        backgroundColor: '#282828',
        borderTopLeftRadius: 15,
        borderTopRightRadius: 15,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: -3 },
        shadowOpacity: 0.27,
        shadowRadius: 4.65,
        elevation: 6,
        zIndex: 100,
    },
    spotifyHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 15,
    },
    spotifyTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        color: 'white',
    },
    spotifyCloseButton: {
        padding: 5,
    },
    spotifyCloseIcon: {
        fontSize: 24,
        color: '#9CA3AF',
    },
    spotifyButtonRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginTop: 10,
    },
    spotifyButton: {
        flex: 1,
        backgroundColor: '#1DB954',
        padding: 12,
        borderRadius: 6,
        alignItems: 'center',
        marginHorizontal: 5,
    },
    spotifyButtonText: {
        color: 'white',
        fontWeight: 'bold',
    },
    spotifyConnectText: {
        color: '#1DB954',
        fontWeight: 'bold',
        marginBottom: 10,
    },
    spotifyErrorContainer: {
        marginTop: 15,
        backgroundColor: '#3E3E3E',
        padding: 10,
        borderRadius: 6,
        maxHeight: 150,
    },
    spotifyErrorTitle: {
        color: '#FF6B6B',
        fontWeight: 'bold',
        marginBottom: 5,
    },
    spotifyErrorItem: {
        borderBottomWidth: 1,
        borderBottomColor: '#555',
        paddingVertical: 5,
    },
    spotifyErrorCode: {
        color: '#FF6B6B',
        fontWeight: 'bold',
    },
    spotifyErrorMessage: {
        color: 'white',
        fontSize: 12,
    },
    spotifyOptionsButton: {
        backgroundColor: '#1DB954',
        paddingVertical: 8,
        paddingHorizontal: 16,
        borderRadius: 4,
        width: '100%',
        alignItems: 'center',
    },
    spotifyOptionsButtonText: {
        color: 'white',
        fontSize: 14,
        fontWeight: 'bold',
    }
});