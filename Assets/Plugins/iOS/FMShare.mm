#import <UIKit/UIKit.h>

extern UIViewController* UnityGetGLViewController();

static NSString* FMShareString(const char* value)
{
    return (value != NULL && value[0] != '\0') ? [NSString stringWithUTF8String:value] : nil;
}

extern "C" {
    bool _FMShare(const char* text, const char* subject, const char* imagePath)
    {
        NSString* nsText = FMShareString(text);
        NSString* nsSubject = FMShareString(subject);
        NSString* nsImagePath = FMShareString(imagePath);

        NSMutableArray* items = [NSMutableArray array];
        if (nsText != nil) {
            [items addObject:nsText];
        }
        if (nsImagePath != nil && [[NSFileManager defaultManager] fileExistsAtPath:nsImagePath]) {
            [items addObject:[NSURL fileURLWithPath:nsImagePath]];
        }
        if (items.count == 0) {
            return false;
        }

        UIViewController* root = UnityGetGLViewController();
        if (root == nil) {
            NSLog(@"[FMShare] No root view controller");
            return false;
        }

        dispatch_async(dispatch_get_main_queue(), ^{
            UIActivityViewController* controller = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
            if (nsSubject != nil) {
                [controller setValue:nsSubject forKey:@"subject"];
            }
            // iPad presents the sheet as a popover; without an anchor UIKit throws.
            UIPopoverPresentationController* popover = controller.popoverPresentationController;
            if (popover != nil) {
                popover.sourceView = root.view;
                popover.sourceRect = CGRectMake(CGRectGetMidX(root.view.bounds), CGRectGetMidY(root.view.bounds), 0, 0);
                popover.permittedArrowDirections = 0;
            }
            [root presentViewController:controller animated:YES completion:nil];
        });
        return true;
    }
}
