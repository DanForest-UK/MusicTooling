import React, { useEffect, useState, useCallback } from 'react';
import { Text, TouchableOpacity, Animated } from 'react-native';
import { useStatus, StatusLevel } from '../StatusContext';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from '../styles';

const StatusBar: React.FC = () => {
    const { status, clearStatus } = useStatus();
    const [visible, setVisible] = useState(false);
    const opacity = useState(new Animated.Value(0))[0];

    // Define handleHide as a useCallback to avoid dependency issues
    const handleHide = useCallback(() => {
        Animated.timing(opacity, {
            toValue: 0,
            duration: 300,
            useNativeDriver: true,
        }).start(() => {
            setVisible(false);
            clearStatus();
        });
    }, [opacity, clearStatus]);

    // Show animation when status updates
    useEffect(() => {
        if (status.Text) {
            setVisible(true);
            Animated.timing(opacity, {
                toValue: 1,
                duration: 300,
                useNativeDriver: true,
            }).start();

            // Auto-hide success messages after 5 seconds
            if (status.Level === StatusLevel.Success) {
                const timer = setTimeout(() => {
                    handleHide();
                }, 5000);

                return () => clearTimeout(timer);
            }
        }
    }, [status, handleHide, opacity]);

    if (!visible || !status.Text) {
        return null;
    }

    // Get the appropriate style based on status level
    const getStatusStyle = () => {
        switch (status.Level) {
            case StatusLevel.Success:
                return styles.statusBarSuccess;
            case StatusLevel.Warning:
                return styles.statusBarWarning;
            case StatusLevel.Error:
                return styles.statusBarError;
            default:
                return styles.statusBarInfo;
        }
    };

    // Get the appropriate icon based on status level
    const getStatusIcon = () => {
        switch (status.Level) {
            case StatusLevel.Success:
                return 'check-circle';
            case StatusLevel.Warning:
                return 'exclamation-triangle';
            case StatusLevel.Error:
                return 'times-circle';
            default:
                return 'info-circle';
        }
    };

    return (
        <Animated.View
            style={[
                styles.statusBar,
                getStatusStyle(),
                { opacity: opacity },
            ]}
        >
            <FontAwesomeIcon
                name={getStatusIcon()}
                style={styles.statusIcon}
            />
            <Text style={styles.statusText} numberOfLines={1}>
                {status.Text}
            </Text>
            <TouchableOpacity
                style={styles.clearButton}
                onPress={handleHide}
            >
                <FontAwesomeIcon
                    name="times"
                    style={styles.clearIcon}
                />
            </TouchableOpacity>
        </Animated.View>
    );
};

export default StatusBar;