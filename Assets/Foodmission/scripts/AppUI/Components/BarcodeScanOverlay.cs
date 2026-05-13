using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            _overlay.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            _overlay.pickingMode = PickingMode.Position;

            _pendingCancelled = onCancelled;

            var closeBtn = new Button(() =>
            {
                Dismiss();
                _pendingCancelled?.Invoke();
                _pendingCancelled = null;
            });
            closeBtn.text = "\u2715";
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.top = 48;
            closeBtn.style.right = 16;
            closeBtn.style.width = 44;
            closeBtn.style.height = 44;
            closeBtn.style.fontSize = 22;
            closeBtn.style.backgroundColor = new Color(0, 0, 0, 0.3f);
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

            var guide = new Label("Align the barcode within the frame");
            guide.style.position = Position.Absolute;
            guide.style.bottom = 100;
            guide.style.left = 0;
            guide.style.right = 0;
            guide.style.unityTextAlign = TextAnchor.MiddleCenter;
            guide.style.color = Color.white;
            guide.style.fontSize = 16;
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
        private BarcodeReader<RGBLuminanceSource> _reader;
        private Action _onCancelled;
        private Action<string> _onDetected;
        private int _frameSkip = 4;
        private int _frameCount;
        private bool _isRunning;

        private int _rotationAngle;
        private bool _mirrored;
        private ScreenOrientation _lastOrientation;
        private Color32[] _rotatedBuffer;
        private byte[] _rgbBuffer;

        public void Initialize(
            VisualElement overlay,
            Action onCancelled,
            Action<string> onDetected)
        {
            _overlay = overlay;
            _onCancelled = onCancelled;
            _onDetected = onDetected;
            _isRunning = true;
        }

        private IEnumerator Start()
        {
            yield return RequestCameraPermission();
            if (!_isRunning) yield break;

            _webCamTexture = new WebCamTexture();
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

            _reader = new BarcodeReader<RGBLuminanceSource>(s => s)
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
                        BarcodeFormat.CODE_39,
                        BarcodeFormat.CODE_128,
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
                int decodeW = texW;
                int decodeH = texH;
                Color32[] decodeSrc = needsRotation ? _rotatedBuffer : pixels;
                int count = decodeW * decodeH;

                if (_rgbBuffer == null || _rgbBuffer.Length != count * 3)
                    _rgbBuffer = new byte[count * 3];

                for (int i = 0; i < count; i++)
                {
                    _rgbBuffer[i * 3] = decodeSrc[i].r;
                    _rgbBuffer[i * 3 + 1] = decodeSrc[i].g;
                    _rgbBuffer[i * 3 + 2] = decodeSrc[i].b;
                }

                var source = new RGBLuminanceSource(_rgbBuffer, decodeW, decodeH);
                var result = _reader.Decode(source);

                if (result != null && !string.IsNullOrEmpty(result.Text))
                {
                    _isRunning = false;
                    _webCamTexture.Stop();
                    _onDetected?.Invoke(result.Text);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BarcodeScanner] Decode error: {ex.Message}");
            }
        }

        private void RotatePixels(Color32[] src, int srcW, int srcH, Color32[] dst, int dstW, int dstH)
        {
            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    int srcIdx = y * srcW + x;
                    int dstIdx;

                    switch (_rotationAngle)
                    {
                        case 90:
                            dstIdx = x * srcH + (srcH - 1 - y);
                            break;
                        case 180:
                            dstIdx = (srcH - 1 - y) * srcW + (srcW - 1 - x);
                            break;
                        case 270:
                            dstIdx = (srcW - 1 - x) * srcH + y;
                            break;
                        default:
                            dstIdx = srcIdx;
                            break;
                    }

                    dst[dstIdx] = src[srcIdx];
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
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
            _rgbBuffer = null;
            _reader = null;
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
