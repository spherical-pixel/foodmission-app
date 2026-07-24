#import <AVFoundation/AVFoundation.h>

extern "C" {
    void _SetIOSTorchEnabled(bool enabled) {
        AVCaptureDevice *device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
        if (device != nil && [device hasTorch] && [device isTorchAvailable]) {
            NSError *error = nil;
            if ([device lockForConfiguration:&error]) {
                if (enabled) {
                    if ([device isTorchModeSupported:AVCaptureTorchModeOn]) {
                        [device setTorchMode:AVCaptureTorchModeOn];
                    }
                } else {
                    if ([device isTorchModeSupported:AVCaptureTorchModeOff]) {
                        [device setTorchMode:AVCaptureTorchModeOff];
                    }
                }
                [device unlockForConfiguration];
            } else {
                NSLog(@"[IOSTorchPlugin] Error locking configuration: %@", error);
            }
        }
    }
}
