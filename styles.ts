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
});
