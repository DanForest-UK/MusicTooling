import React from 'react';
import { View, StyleSheet, Animated, TouchableWithoutFeedback } from 'react-native';

interface CustomModalProps {
    visible: boolean;
    onBackdropPress?: () => void;
    children: React.ReactNode;
}

const CustomModal: React.FC<CustomModalProps> = ({ visible, onBackdropPress, children }) => {
    // Animation value for fade in/out
    const [fadeAnim] = React.useState(new Animated.Value(0));

    // Manage visibility with a local state to handle animation completion
    const [isVisible, setIsVisible] = React.useState(false);

    React.useEffect(() => {
        if (visible) {
            setIsVisible(true);
            Animated.timing(fadeAnim, {
                toValue: 1,
                duration: 300,
                useNativeDriver: true,
            }).start();
        } else {
            Animated.timing(fadeAnim, {
                toValue: 0,
                duration: 300,
                useNativeDriver: true,
            }).start(() => {
                setIsVisible(false);
            });
        }
    }, [visible, fadeAnim]);

    if (!isVisible && !visible) {
        return null;
    }

    return (
        <View style={styles.container}>
            <Animated.View
                style={[
                    styles.backdrop,
                    { opacity: fadeAnim }
                ]}
            >
                <TouchableWithoutFeedback onPress={onBackdropPress}>
                    <View style={styles.backdropTouchable} />
                </TouchableWithoutFeedback>
            </Animated.View>

            <Animated.View
                style={[
                    styles.contentContainer,
                    {
                        opacity: fadeAnim,
                        transform: [{
                            scale: fadeAnim.interpolate({
                                inputRange: [0, 1],
                                outputRange: [0.8, 1],
                            })
                        }]
                    }
                ]}
            >
                {children}
            </Animated.View>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        justifyContent: 'center',
        alignItems: 'center',
        zIndex: 1000,
    },
    backdrop: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
    },
    backdropTouchable: {
        flex: 1,
    },
    contentContainer: {
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
    }
});

export default CustomModal;