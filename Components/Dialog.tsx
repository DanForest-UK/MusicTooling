import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import CustomModal from './CustomModal';

interface DialogProps {
    visible: boolean;
    onLoadSession: () => void;
    onDeleteSession: () => void;
}

const SessionDialog: React.FC<DialogProps> = ({ 
    visible, 
    onLoadSession, 
    onDeleteSession 
}) => {
    return (
        <CustomModal visible={visible}>
            <Text style={styles.title}>Previous Session Found</Text>
            <Text style={styles.message}>
                Would you like to continue your previous session?
            </Text>
            <View style={styles.buttonRow}>
                <TouchableOpacity
                    style={[styles.button, styles.primaryButton]}
                    onPress={onLoadSession}
                >
                    <Text style={styles.buttonText}>Yes, Continue</Text>
                </TouchableOpacity>
                <TouchableOpacity
                    style={[styles.button, styles.secondaryButton]}
                    onPress={onDeleteSession}
                >
                    <Text style={styles.buttonText}>No, Start Fresh</Text>
                </TouchableOpacity>
            </View>
        </CustomModal>
    );
};

const styles = StyleSheet.create({
    title: {
        fontSize: 20,
        fontWeight: 'bold',
        color: 'white',
        marginBottom: 15,
    },
    message: {
        fontSize: 16,
        color: 'white',
        textAlign: 'center',
        marginBottom: 20,
    },
    buttonRow: {
        flexDirection: 'row',
        justifyContent: 'space-around',
        width: '100%',
        marginTop: 10,
    },
    button: {
        paddingVertical: 10,
        paddingHorizontal: 20,
        borderRadius: 5,
        minWidth: 120,
        alignItems: 'center',
        marginHorizontal: 10,
    },
    primaryButton: {
        backgroundColor: '#1DB954', // Spotify green for continuation
    },
    secondaryButton: {
        backgroundColor: '#B71C1C', // Red for delete/start fresh
    },
    buttonText: {
        color: 'white',
        fontSize: 16,
        fontWeight: 'bold',
    },
});

export default SessionDialog;