import React from 'react';
import { View, Animated, TouchableWithoutFeedback } from 'react-native';
import { styles } from '../styles';

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
        <View style={styles.modalContainer}>
            <Animated.View
                style={[
                    styles.modalBackdrop,
                    { opacity: fadeAnim },
                ]}
            >
                <TouchableWithoutFeedback onPress={onBackdropPress}>
                    <View style={styles.modalBackdropTouchable} />
                </TouchableWithoutFeedback>
            </Animated.View>

            <Animated.View
                style={[
                    styles.modalContentContainer,
                    {
                        opacity: fadeAnim,
                        transform: [{
                            scale: fadeAnim.interpolate({
                                inputRange: [0, 1],
                                outputRange: [0.8, 1],
                            }),
                        }],
                    },
                ]}
            >
                {children}
            </Animated.View>
        </View>
    );
};

export default CustomModal;
