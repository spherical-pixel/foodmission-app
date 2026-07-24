package eu.foodmission.platform;

import android.app.Activity;
import android.content.Context;
import android.hardware.camera2.CameraCharacteristics;
import android.hardware.camera2.CameraManager;
import android.hardware.camera2.CameraCaptureSession;
import android.hardware.camera2.CaptureRequest;
import android.util.Log;
import java.lang.reflect.Field;
import java.util.HashSet;
import java.util.Set;

public class AndroidTorchPlugin {
    private static final String TAG = "AndroidTorchPlugin";

    public static boolean setTorchEnabled(Activity activity, boolean enabled) {
        if (activity == null) return false;

        Log.d(TAG, "setTorchEnabled called with enabled=" + enabled);

        // Method 1: CameraManager setTorchMode (Camera2 API)
        try {
            CameraManager cameraManager = (CameraManager) activity.getSystemService(Context.CAMERA_SERVICE);
            if (cameraManager != null) {
                String[] cameraIds = cameraManager.getCameraIdList();
                for (String id : cameraIds) {
                    CameraCharacteristics characteristics = cameraManager.getCameraCharacteristics(id);
                    Integer facing = characteristics.get(CameraCharacteristics.LENS_FACING);
                    Boolean flashAvailable = characteristics.get(CameraCharacteristics.FLASH_INFO_AVAILABLE);

                    if (Boolean.TRUE.equals(flashAvailable) && (facing == null || facing == CameraCharacteristics.LENS_FACING_BACK)) {
                        try {
                            cameraManager.setTorchMode(id, enabled);
                            Log.d(TAG, "Successfully set torch mode via CameraManager for camera: " + id);
                            return true;
                        } catch (Exception e) {
                            Log.w(TAG, "CameraManager setTorchMode failed for cam " + id + ": " + e.getMessage());
                        }
                    }
                }
            }
        } catch (Exception e) {
            Log.w(TAG, "CameraManager search failed: " + e.getMessage());
        }

        // Method 2: Deep Reflection scan on Activity & UnityPlayer for active Camera1 / Camera2 objects & session
        try {
            Set<Object> visited = new HashSet<>();
            if (scanObject(activity, enabled, 0, visited)) {
                return true;
            }

            // Also inspect static fields of UnityPlayer
            try {
                Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
                Field[] staticFields = unityPlayerClass.getDeclaredFields();
                for (Field sf : staticFields) {
                    try {
                        sf.setAccessible(true);
                        Object staticVal = sf.get(null);
                        if (staticVal != null && scanObject(staticVal, enabled, 0, visited)) {
                            return true;
                        }
                    } catch (Throwable ignored) {}
                }
            } catch (Throwable t) {
                Log.w(TAG, "UnityPlayer static field scan error: " + t.getMessage());
            }
        } catch (Exception e) {
            Log.w(TAG, "Deep reflection scan failed: " + e.getMessage());
        }

        Log.w(TAG, "Failed to enable torch via all methods.");
        return false;
    }

    private static boolean scanObject(Object obj, boolean enabled, int depth, Set<Object> visited) {
        if (obj == null || depth > 5 || visited.contains(obj)) return false;
        visited.add(obj);

        Class<?> clazz = obj.getClass();
        String clazzName = clazz.getName();

        // Skip non-Unity Android framework UI objects
        if (clazzName.startsWith("java.") || clazzName.startsWith("android.view.") ||
            clazzName.startsWith("android.widget.") || clazzName.startsWith("android.os.") ||
            clazzName.startsWith("android.content.") || clazzName.startsWith("android.graphics.")) {
            return false;
        }

        Log.d(TAG, "Scanning depth " + depth + ": " + clazzName);

        // Check if object is legacy Camera1
        if (clazzName.equals("android.hardware.Camera")) {
            return applyLegacyCameraParameters(obj, enabled);
        }

        // Check if object container holds both CaptureRequest.Builder and CameraCaptureSession
        if (tryApplyCamera2Session(obj, enabled)) {
            return true;
        }

        // Traverse fields up the class hierarchy
        while (clazz != null && !clazz.getName().equals("java.lang.Object")) {
            Field[] fields;
            try {
                fields = clazz.getDeclaredFields();
            } catch (Throwable t) {
                break;
            }

            for (Field field : fields) {
                try {
                    field.setAccessible(true);
                    Object val = field.get(obj);
                    if (val == null) continue;

                    String valType = val.getClass().getName();

                    if (valType.equals("android.hardware.Camera")) {
                        if (applyLegacyCameraParameters(val, enabled)) return true;
                    }

                    // Recurse into Unity objects or custom containers
                    if (depth < 4 && (valType.startsWith("com.unity3d.") || valType.startsWith("eu.foodmission.") || valType.contains("Camera"))) {
                        if (scanObject(val, enabled, depth + 1, visited)) return true;
                    }
                } catch (Throwable ignored) {
                }
            }
            clazz = clazz.getSuperclass();
        }

        return false;
    }

    private static boolean tryApplyCamera2Session(Object container, boolean enabled) {
        if (container == null) return false;

        CaptureRequest.Builder builder = null;
        CameraCaptureSession session = null;

        Class<?> clazz = container.getClass();
        while (clazz != null && !clazz.getName().equals("java.lang.Object")) {
            Field[] fields;
            try {
                fields = clazz.getDeclaredFields();
            } catch (Throwable t) {
                break;
            }

            for (Field field : fields) {
                try {
                    field.setAccessible(true);
                    Object val = field.get(container);
                    if (val == null) continue;

                    if (val instanceof CaptureRequest.Builder) {
                        builder = (CaptureRequest.Builder) val;
                    } else if (val instanceof CameraCaptureSession) {
                        session = (CameraCaptureSession) val;
                    }
                } catch (Throwable ignored) {}
            }
            clazz = clazz.getSuperclass();
        }

        Log.d(TAG, "tryApplyCamera2Session on " + container.getClass().getName() +
                   " -> builder=" + (builder != null) + ", session=" + (session != null));

        if (builder != null) {
            try {
                int mode = enabled ? CaptureRequest.FLASH_MODE_TORCH : CaptureRequest.FLASH_MODE_OFF;
                builder.set(CaptureRequest.FLASH_MODE, mode);

                if (session != null) {
                    CaptureRequest request = builder.build();
                    session.setRepeatingRequest(request, null, null);
                    Log.d(TAG, "SUCCESS! Updated repeating request on CameraCaptureSession with FLASH_MODE=" + mode);
                    return true;
                } else {
                    Log.w(TAG, "Found Builder but no CameraCaptureSession on " + container.getClass().getName());
                }
            } catch (Exception e) {
                Log.w(TAG, "Failed to apply Camera2 session update: " + e.getMessage());
            }
        }

        return false;
    }

    private static boolean applyLegacyCameraParameters(Object cameraObj, boolean enabled) {
        try {
            android.hardware.Camera camera = (android.hardware.Camera) cameraObj;
            android.hardware.Camera.Parameters params = camera.getParameters();
            if (params != null) {
                String targetMode = enabled ?
                    android.hardware.Camera.Parameters.FLASH_MODE_TORCH :
                    android.hardware.Camera.Parameters.FLASH_MODE_OFF;
                params.setFlashMode(targetMode);
                camera.setParameters(params);
                Log.d(TAG, "Successfully applied legacy Camera flash mode: " + targetMode);
                return true;
            }
        } catch (Exception e) {
            Log.w(TAG, "applyLegacyCameraParameters failed: " + e.getMessage());
        }
        return false;
    }
}
