import React from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import { styles } from '../styles';
import CustomModal from './CustomModal';

interface DialogProps {
    visible: boolean;
    onLoadSession: () => void;
    onDeleteSession: () => void;
}

const SessionDialog: React.FC<DialogProps> = ({
    visible,
    onLoadSession,
    onDeleteSession,
}) => {
    return (
        <CustomModal visible={visible}>
            <Text style={styles.dialogTitle}>Previous Session Found</Text>
            <Text style={styles.dialogMessage}>
                Would you like to continue your previous session?
            </Text>
            <View style={styles.dialogButtonRow}>
                <TouchableOpacity
                    style={[styles.dialogButton, styles.dialogPrimaryButton]}
                    onPress={onLoadSession}
                >
                    <Text style={styles.dialogButtonText}>Yes, Continue</Text>
                </TouchableOpacity>
                <TouchableOpacity
                    style={[styles.dialogButton, styles.dialogSecondaryButton]}
                    onPress={onDeleteSession}
                >
                    <Text style={styles.dialogButtonText}>No, Start Fresh</Text>
                </TouchableOpacity>
            </View>
        </CustomModal>
    );
};

export default SessionDialog;
