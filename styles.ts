import { StyleSheet } from 'react-native';

export const styles = StyleSheet.create({
    container: {
        flex: 1,
        padding: 20,
        paddingVertical: 10,
    },
    // Modal styles for session dialog
    modalOverlay: {
        flex: 1,
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        justifyContent: 'center',
        alignItems: 'center',
    },
    modalContent: {
        backgroundColor: '#2D3748',
        borderRadius: 8,
        padding: 20,
        width: '80%',
        maxWidth: 500,
        alignItems: 'center',
    },
    modalTitle: {
        fontSize: 20,
        fontWeight: 'bold',
        color: 'white',
        marginBottom: 15,
    },
    modalText: {
        fontSize: 16,
        color: 'white',
        textAlign: 'center',
        marginBottom: 20,
    },
    modalButtonRow: {
        flexDirection: 'row',
        justifyContent: 'space-around',
        width: '100%',
        marginTop: 10,
    },
    modalButton: {
        paddingVertical: 10,
        paddingHorizontal: 20,
        borderRadius: 5,
        minWidth: 120,
        alignItems: 'center',
    },
    modalButtonPrimary: {
        backgroundColor: '#1DB954', // Spotify green for continuation
    },
    modalButtonSecondary: {
        backgroundColor: '#B71C1C', // Red for delete/start fresh
    },
    modalButtonText: {
        color: 'white',
        fontSize: 16,
        fontWeight: 'bold',
    },
    listContainer: {
        padding: 3,
        paddingRight: 10,
        paddingBottom: 80, // Add padding at bottom for Spotify toggle
    },
    // Table header styles
    tableHeader: {
        flexDirection: 'row',
        backgroundColor: '#2D3748',
        borderBottomWidth: 2,
        borderBottomColor: '#4A5568',
        marginBottom: 5,
        paddingVertical: 8,
        paddingHorizontal: 10,
    },
    tableHeaderText: {
        fontWeight: 'bold',
        color: 'white',
        fontSize: 14,
    },
    tableHeaderCheckbox: {
        width: 40,
        justifyContent: 'center',
        alignItems: 'center',
    },
    // Table row styles
    tableRow: {
        flexDirection: 'row',
        alignItems: 'center',
        backgroundColor: '#374151',
        marginVertical: 2,
        borderRadius: 3,
        paddingVertical: 6,
        paddingHorizontal: 10,
    },
    // Table cell styles
    tableCheckboxCell: {
        width: 40,
        justifyContent: 'center',
        alignItems: 'center',
    },
    tableArtistCell: {
        flex: 3,
        paddingHorizontal: 5,
    },
    tableAlbumCell: {
        flex: 2,
        paddingHorizontal: 5,
    },
    tableTitleCell: {
        flex: 3,
        paddingHorizontal: 5,
    },
    tableRatingCell: {
        width: 100,
        flexDirection: 'row',
        alignItems: 'center',
        paddingHorizontal: 5,
        paddingRight: 15,
    },
    tableStatusCell: {
        flex: 1,
        paddingHorizontal: 5,
        minWidth: 80,
        justifyContent: 'center',
    },
    tableCellText: {
        color: 'white',
        fontSize: 13,
    },
    // Keeping existing styles
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
        zIndex: 200,
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
    // Legacy styles for the SongItem
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
    withSpotifyStatus: {
        paddingRight: 120,
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
    spotifyButtonDisabled: {
        opacity: 0.6,
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
    // Error display styles
    spotifyErrorContainer: {
        marginTop: 15,
        backgroundColor: '#3E3E3E',
        padding: 10,
        borderRadius: 6,
        maxHeight: 150,
    },
    spotifyErrorScroll: {
        maxHeight: 120,
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
    },
    // Spotify status styles
    spotifyStatusContainer: {
        position: 'absolute',
        right: 0,
        top: 0,
        bottom: 0,
        width: 120,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#2D3748',
        borderTopRightRadius: 3,
        borderBottomRightRadius: 3,
        paddingHorizontal: 8,
    },
    spotifyStatusText: {
        fontSize: 12,
        fontWeight: 'bold',
        textAlign: 'center',
    },
    spotifyStatusArtistText: {
        marginTop: 8,
    },
    spotifyStatusSuccess: {
        color: '#1DB954', // Spotify green
    },
    spotifyStatusError: {
        color: '#FF6B6B', // Red for errors
    },
    // Scan cancellation button styles
    cancelScanButton: {
        backgroundColor: '#B71C1C',
        paddingVertical: 8,
        paddingHorizontal: 16,
        borderRadius: 5,
        marginTop: 20,
    },
    cancelScanButtonText: {
        color: 'white',
        fontWeight: 'bold',
    },

    // Status bar styles
    statusBar: {
        position: 'absolute',
        bottom: 0,
        left: 0,
        right: 0,
        padding: 10,
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        borderTopWidth: 1,
        borderTopColor: '#3D4852',
        zIndex: 150, // Increased z-index to appear above other elements
    },
    statusBarInfo: {
        backgroundColor: '#2D3748',
    },
    statusBarSuccess: {
        backgroundColor: '#1C6937',
    },
    statusBarWarning: {
        backgroundColor: '#974C10',
    },
    statusBarError: {
        backgroundColor: '#A12121',
    },
    statusText: {
        color: 'white',
        flex: 1,
        marginRight: 10,
    },
    statusIcon: {
        marginRight: 10,
        color: 'white',
        fontSize: 16,
    },
    clearButton: {
        padding: 5,
    },
    clearIcon: {
        color: 'white',
        fontSize: 16,
    },
    // Authentication and processing styles
    authLoadingContainer: {
        alignItems: 'center',
        marginVertical: 15,
    },
    authLoadingText: {
        color: 'white',
        marginTop: 10,
    },
    processingContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        marginTop: 10,
    },
    processingText: {
        color: 'white',
        marginLeft: 10,
    },
    selectedSongsContainer: {
        backgroundColor: '#3E3E3E',
        padding: 10,
        borderRadius: 4,
        marginBottom: 10,
    },
    selectedSongsText: {
        color: 'white',
        fontSize: 12,
    },
    // Simplified progress display styles with compact cancel button
    spotifyProgressContainer: {
        marginVertical: 10,
        backgroundColor: '#3E3E3E',
        padding: 12,
        borderRadius: 6,
    },
    spotifyProgressHeader: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'flex-start',
    },
    spotifyProgressTitle: {
        color: 'white',
        fontWeight: 'bold',
        fontSize: 14,
        flex: 1,
    },
    // Updated compact cancel button style
    spotifyCancelButtonCompact: {
        backgroundColor: '#B71C1C',
        paddingVertical: 6,
        paddingHorizontal: 12,
        borderRadius: 4,
        alignSelf: 'flex-start',
        marginLeft: 10,
    },
    spotifyCancelButtonText: {
        color: 'white',
        fontWeight: 'bold',
        fontSize: 12,
    },

    // Dialog component styles (moved from Dialog.tsx)
    dialogTitle: {
        fontSize: 20,
        fontWeight: 'bold',
        color: 'white',
        marginBottom: 15,
    },
    dialogMessage: {
        fontSize: 16,
        color: 'white',
        textAlign: 'center',
        marginBottom: 20,
    },
    dialogButtonRow: {
        flexDirection: 'row',
        justifyContent: 'space-around',
        width: '100%',
        marginTop: 10,
    },
    dialogButton: {
        paddingVertical: 10,
        paddingHorizontal: 20,
        borderRadius: 5,
        minWidth: 120,
        alignItems: 'center',
        marginHorizontal: 10,
    },
    dialogPrimaryButton: {
        backgroundColor: '#1DB954', // Spotify green for continuation
    },
    dialogSecondaryButton: {
        backgroundColor: '#B71C1C', // Red for delete/start fresh
    },
    dialogButtonText: {
        color: 'white',
        fontSize: 16,
        fontWeight: 'bold',
    },

    // CustomModal component styles (moved from CustomModal.tsx)
    modalContainer: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        justifyContent: 'center',
        alignItems: 'center',
        zIndex: 1000,
    },
    modalBackdrop: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
    },
    modalBackdropTouchable: {
        flex: 1,
    },
    modalContentContainer: {
        backgroundColor: '#2D3748',
        borderRadius: 8,
        padding: 20,
        width: '80%',
        maxWidth: 500,
        alignItems: 'center',
        elevation: 5,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.25,
        shadowRadius: 3.84,
    },
    // Add these new styles to the styles.ts file

    // Folder path input container
    folderPathContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        marginBottom: 10,
    },
    folderPathInput: {
        flex: 1,
        height: 40,
        backgroundColor: '#4B5563',
        color: 'white',
        borderRadius: 5,
        paddingHorizontal: 10,
        marginRight: 10,
    },
    browseButton: {
        backgroundColor: '#4B5563',
        paddingVertical: 8,
        paddingHorizontal: 12,
        borderRadius: 5,
    },
    browseButtonText: {
        color: 'white',
        fontWeight: 'bold',
    },
});
