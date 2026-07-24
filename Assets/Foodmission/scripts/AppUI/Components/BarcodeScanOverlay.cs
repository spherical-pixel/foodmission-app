using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
using ZXing;

namespace eu.foodmission.platform.Components
{
    public static class BarcodeScanOverlay
    {
        private static BarcodeScanOverlayController _controller;
        private static VisualElement _overlay;
        private static Action _pendingCancelled;

        public static void Show(
            VisualElement anchor,
            Action<string> onBarcodeDetected,
            Action onCancelled = null)
        {
            Dismiss();

            var root = anchor.panel?.visualTree;
            if (root == null)
            {
                onCancelled?.Invoke();
                return;
            }

            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new Color(0, 0, 0, 1);
            _overlay.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            _overlay.pickingMode = PickingMode.Position;

            _pendingCancelled = onCancelled;

            Unity.AppUI.UI.Button torchBtn = null;
            torchBtn = new Unity.AppUI.UI.Button(() =>
            {
                if (_controller != null)
                {
                    bool isOn = _controller.ToggleTorch();
                    if (isOn)
                    {
                        torchBtn.style.backgroundColor = new Color(1.0f, 0.8f, 0.2f, 1.0f);
                        torchBtn.style.color = Color.black;
                    }
                    else
                    {
                        torchBtn.style.backgroundColor = new Color(0, 0, 0, 1.0f);
                        torchBtn.style.color = Color.white;
                    }
                }
            });
            torchBtn.title = "🔦";
            torchBtn.style.position = Position.Absolute;
            torchBtn.style.top = 160;
            torchBtn.style.left = 80;
            torchBtn.style.width = 160;
            torchBtn.style.height = 160;
            torchBtn.style.fontSize = 44;
            torchBtn.style.backgroundColor = new Color(0, 0, 0, 1.0f);
            torchBtn.style.color = Color.white;
            torchBtn.style.borderLeftColor = torchBtn.style.borderRightColor = torchBtn.style.borderTopColor = torchBtn.style.borderBottomColor = new Color(1, 1, 1, 0.5f);
            torchBtn.style.borderLeftWidth = torchBtn.style.borderRightWidth = torchBtn.style.borderTopWidth = torchBtn.style.borderBottomWidth = 1;
            torchBtn.style.borderTopLeftRadius = torchBtn.style.borderTopRightRadius = torchBtn.style.borderBottomLeftRadius = torchBtn.style.borderBottomRightRadius = 22;
            _overlay.Add(torchBtn);

            var closeBtn = new Unity.AppUI.UI.Button(() =>
            {
                Dismiss();
                _pendingCancelled?.Invoke();
                _pendingCancelled = null;
            });
            closeBtn.title = "X";
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.top = 160;
            closeBtn.style.right = 80;
            closeBtn.style.width = 160;
            closeBtn.style.height = 160;
            closeBtn.style.fontSize = 44;
            closeBtn.style.backgroundColor = new Color(0, 0, 0, 1.0f);
            closeBtn.style.color = Color.white;
            closeBtn.style.borderLeftColor = closeBtn.style.borderRightColor = closeBtn.style.borderTopColor = closeBtn.style.borderBottomColor = new Color(1, 1, 1, 0.5f);
            closeBtn.style.borderLeftWidth = closeBtn.style.borderRightWidth = closeBtn.style.borderTopWidth = closeBtn.style.borderBottomWidth = 1;
            closeBtn.style.borderTopLeftRadius = closeBtn.style.borderTopRightRadius = closeBtn.style.borderBottomLeftRadius = closeBtn.style.borderBottomRightRadius = 22;
            _overlay.Add(closeBtn);

            var scanFrame = new VisualElement();
            scanFrame.style.position = Position.Absolute;
            scanFrame.style.left = Length.Percent(10);
            scanFrame.style.right = Length.Percent(10);
            scanFrame.style.top = Length.Percent(25);
            scanFrame.style.bottom = Length.Percent(35);
            scanFrame.style.borderLeftColor = scanFrame.style.borderRightColor = scanFrame.style.borderTopColor = scanFrame.style.borderBottomColor = new Color(1, 1, 1, 0.5f);
            scanFrame.style.borderLeftWidth = scanFrame.style.borderRightWidth = scanFrame.style.borderTopWidth = scanFrame.style.borderBottomWidth = 2;
            scanFrame.style.borderTopLeftRadius = scanFrame.style.borderTopRightRadius = scanFrame.style.borderBottomLeftRadius = scanFrame.style.borderBottomRightRadius = 8;
            _overlay.Add(scanFrame);

            var guide = new Unity.AppUI.UI.LocalizedTextElement();
            guide.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SCAN_HELP");
            guide.style.position = Position.Absolute;
            guide.style.bottom = 100;
            guide.style.left = 0;
            guide.style.right = 0;
            guide.style.unityTextAlign = TextAnchor.MiddleCenter;
            guide.style.color = Color.white;
            guide.style.fontSize = 48;
            guide.style.whiteSpace = WhiteSpace.Normal;
            _overlay.Add(guide);

            root.Add(_overlay);

            var go = new GameObject("BarcodeScanner");
            _controller = go.AddComponent<BarcodeScanOverlayController>();
            _controller.Initialize(_overlay, () =>
            {
                Dismiss();
                onCancelled?.Invoke();
            }, barcode =>
            {
                Dismiss();
                onBarcodeDetected?.Invoke(barcode);
            });
        }

        public static void Dismiss()
        {
            if (_controller != null)
            {
                _controller.Stop();
                UnityEngine.Object.Destroy(_controller.gameObject);
                _controller = null;
            }

            if (_overlay != null && _overlay.parent != null)
            {
                _overlay.RemoveFromHierarchy();
            }
            _overlay = null;
            _pendingCancelled = null;
        }
    }

    internal class BarcodeScanOverlayController : MonoBehaviour
    {
        private VisualElement _overlay;
        private WebCamTexture _webCamTexture;
        private Texture2D _previewTexture;
        private BarcodeReader<PlanarYUVLuminanceSource> _reader;
        private Action _onCancelled;
        private Action<string> _onDetected;
        private int _frameSkip = 2;
        private int _frameCount;
        private bool _isRunning;
        private bool _isTorchEnabled;

        private string _lastCandidate;
        private int _candidateCount;

        private int _rotationAngle;
        private bool _mirrored;
        private ScreenOrientation _lastOrientation;
        private Color32[] _rotatedBuffer;
        private byte[] _luminanceBuffer;

        public void Initialize(
            VisualElement overlay,
            Action onCancelled,
            Action<string> onDetected)
        {
            _overlay = overlay;
            _onCancelled = onCancelled;
            _onDetected = onDetected;
            _isRunning = true;
            _lastCandidate = null;
            _candidateCount = 0;
        }

        private IEnumerator Start()
        {
            yield return RequestCameraPermission();
            if (!_isRunning) yield break;

            _webCamTexture = new WebCamTexture(1280, 720, 30);
            _webCamTexture.Play();

            for (int i = 0; i < 30; i++)
            {
                if (_webCamTexture.width > 100) break;
                yield return null;
            }

            if (!_webCamTexture.isPlaying || _webCamTexture.width < 100)
            {
                _isRunning = false;
                _onCancelled?.Invoke();
                yield break;
            }

            _rotationAngle = _webCamTexture.videoRotationAngle;
            _mirrored = _webCamTexture.videoVerticallyMirrored;
            _lastOrientation = Screen.orientation;

            _rotationAngle = (360 - _rotationAngle) % 360;

            _reader = new BarcodeReader<PlanarYUVLuminanceSource>(s => s)
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<BarcodeFormat>
                    {
                        BarcodeFormat.EAN_13,
                        BarcodeFormat.EAN_8,
                        BarcodeFormat.UPC_A,
                        BarcodeFormat.UPC_E,
                        BarcodeFormat.CODE_128,
                        BarcodeFormat.CODE_39
                    }
                }
            };
        }

        private IEnumerator RequestCameraPermission()
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                yield return new WaitForSeconds(0.3f);
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    _isRunning = false;
                    _onCancelled?.Invoke();
                }
            }
#endif
            yield break;
        }

        public bool ToggleTorch()
        {
            _isTorchEnabled = !_isTorchEnabled;
            SetTorchState(_isTorchEnabled);
            return _isTorchEnabled;
        }

        private void SetTorchState(bool enabled)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var pluginClass = new AndroidJavaClass("eu.foodmission.platform.AndroidTorchPlugin"))
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    bool success = pluginClass.CallStatic<bool>("setTorchEnabled", activity, enabled);
                    Debug.Log($"[BarcodeScanner] AndroidTorchPlugin setTorchEnabled({enabled}) result: {success}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BarcodeScanner] Android torch error: {ex.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                _SetIOSTorchEnabled(enabled);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BarcodeScanner] iOS torch set error: {ex.Message}");
            }
#else
            Debug.Log($"[BarcodeScanner] Torch set to {enabled} (Editor fallback)");
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _SetIOSTorchEnabled(bool enabled);
#endif

        private void Update()
        {
            if (!_isRunning || _webCamTexture == null) return;
            if (!_webCamTexture.isPlaying || !_webCamTexture.didUpdateThisFrame) return;

            if (Screen.orientation != _lastOrientation)
            {
                _lastOrientation = Screen.orientation;
                _rotationAngle = _webCamTexture.videoRotationAngle;
                _mirrored = _webCamTexture.videoVerticallyMirrored;
                _rotationAngle = (360 - _rotationAngle) % 360;
            }

            int camW = _webCamTexture.width;
            int camH = _webCamTexture.height;
            if (camW < 100 || camH < 100) return;

            bool needsRotation = _rotationAngle != 0;
            bool swapDimensions = _rotationAngle == 90 || _rotationAngle == 270;
            int texW = swapDimensions ? camH : camW;
            int texH = swapDimensions ? camW : camH;

            var pixels = _webCamTexture.GetPixels32();

            if (_previewTexture == null || _previewTexture.width != texW || _previewTexture.height != texH)
            {
                if (_previewTexture != null) Destroy(_previewTexture);
                _previewTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
                _previewTexture.hideFlags = HideFlags.HideAndDontSave;
                _overlay.style.backgroundImage = new StyleBackground(_previewTexture);
            }

            if (needsRotation)
            {
                int count = texW * texH;
                if (_rotatedBuffer == null || _rotatedBuffer.Length != count)
                    _rotatedBuffer = new Color32[count];

                RotatePixels(pixels, camW, camH, _rotatedBuffer, texW, texH);
                _previewTexture.SetPixels32(_rotatedBuffer);
            }
            else
            {
                _previewTexture.SetPixels32(pixels);
            }
            _previewTexture.Apply(false);

            _frameCount++;
            if (_frameCount % _frameSkip != 0) return;

            try
            {
                Color32[] decodeSrc = needsRotation ? _rotatedBuffer : pixels;

                // Crop Region of Interest (ROI) matching visual frame (center 70% width, 45% height)
                int cropW = (int)(texW * 0.70f);
                int cropH = (int)(texH * 0.45f);
                int cropLeft = (texW - cropW) / 2;
                int cropTop = (texH - cropH) / 2;
                int cropCount = cropW * cropH;

                if (_luminanceBuffer == null || _luminanceBuffer.Length != cropCount)
                    _luminanceBuffer = new byte[cropCount];

                for (int y = 0; y < cropH; y++)
                {
                    int srcRowOffset = (cropTop + y) * texW + cropLeft;
                    int dstRowOffset = y * cropW;
                    for (int x = 0; x < cropW; x++)
                    {
                        Color32 c = decodeSrc[srcRowOffset + x];
                        _luminanceBuffer[dstRowOffset + x] = (byte)((c.r * 19595 + c.g * 38470 + c.b * 7471) >> 16);
                    }
                }

                var source = new PlanarYUVLuminanceSource(_luminanceBuffer, cropW, cropH, 0, 0, cropW, cropH, false);
                var result = _reader.Decode(source);

                if (result != null && !string.IsNullOrEmpty(result.Text) && result.Text.Length >= 6)
                {
                    if (_lastCandidate == result.Text)
                    {
                        _candidateCount++;
                        if (_candidateCount >= 2)
                        {
                            _isRunning = false;
                            if (_isTorchEnabled)
                            {
                                SetTorchState(false);
                                _isTorchEnabled = false;
                            }
                            _webCamTexture.Stop();
                            _onDetected?.Invoke(result.Text);
                        }
                    }
                    else
                    {
                        _lastCandidate = result.Text;
                        _candidateCount = 1;
                    }
                }
                else
                {
                    _lastCandidate = null;
                    _candidateCount = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BarcodeScanner] Decode error: {ex.Message}");
            }
        }

        private void RotatePixels(Color32[] src, int srcW, int srcH, Color32[] dst, int dstW, int dstH)
        {
            switch (_rotationAngle)
            {
                case 90:
                    for (int y = 0; y < srcH; y++)
                    {
                        int yOffset = y * srcW;
                        int dstX = srcH - 1 - y;
                        for (int x = 0; x < srcW; x++)
                        {
                            dst[x * srcH + dstX] = src[yOffset + x];
                        }
                    }
                    break;
                case 180:
                    for (int y = 0; y < srcH; y++)
                    {
                        int yOffset = y * srcW;
                        int dstYRow = (srcH - 1 - y) * srcW;
                        for (int x = 0; x < srcW; x++)
                        {
                            dst[dstYRow + (srcW - 1 - x)] = src[yOffset + x];
                        }
                    }
                    break;
                case 270:
                    for (int y = 0; y < srcH; y++)
                    {
                        int yOffset = y * srcW;
                        for (int x = 0; x < srcW; x++)
                        {
                            dst[(srcW - 1 - x) * srcH + y] = src[yOffset + x];
                        }
                    }
                    break;
                default:
                    Array.Copy(src, dst, src.Length);
                    break;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_isTorchEnabled)
            {
                SetTorchState(false);
                _isTorchEnabled = false;
            }
            if (_webCamTexture != null)
            {
                if (_webCamTexture.isPlaying) _webCamTexture.Stop();
                Destroy(_webCamTexture);
                _webCamTexture = null;
            }
            if (_previewTexture != null)
            {
                Destroy(_previewTexture);
                _previewTexture = null;
            }
            _rotatedBuffer = null;
            _luminanceBuffer = null;
            _reader = null;
            _lastCandidate = null;
            _candidateCount = 0;
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
