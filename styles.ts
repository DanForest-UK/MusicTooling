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
    fileTextContainer: {
        padding: 5,
        marginVertical: 2,
        backgroundColor: '#4B5563',
        borderRadius: 3,
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
    pickerLabel: {
        fontSize: 16,
        color: 'white',
        marginRight: 10,
    },
    picker: {
        height: 40,
        width: 150,
        color: 'white',
        backgroundColor: '#4B5563', // Same as fileTextContainer for consistency
        borderRadius: 5,
        paddingHorizontal: 10,
    },

});

