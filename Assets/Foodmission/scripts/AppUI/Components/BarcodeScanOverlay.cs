using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using ZXing;
using Unity.AppUI.MVVM;
using Unity.AppUI.Core;

namespace eu.foodmission.platform.Components
{
    public static class BarcodeScanOverlay
    {
        private static BarcodeScanOverlayController _controller;
        private static VisualElement _overlay;
        private static Modal _modal;
        private static Action _pendingCancelled;

        public static void Show(
            VisualElement anchor,
            Action<string> onBarcodeDetected,
            Action onCancelled = null)
        {
            Dismiss();

            if (anchor == null)
            {
                onCancelled?.Invoke();
                return;
            }

            var panelRoot = anchor.panel?.visualTree;
            if (panelRoot == null)
            {
                onCancelled?.Invoke();
                return;
            }

            _pendingCancelled = onCancelled;

            // ── Root container for the full-screen modal ──────────────────────
            _overlay = new VisualElement
            {
                name = "barcode-scan-overlay"
            };
            _overlay.style.flexGrow = 1;
            _overlay.style.width = Length.Percent(100);
            _overlay.style.height = Length.Percent(100);
            _overlay.style.backgroundColor = Color.black;
            _overlay.style.overflow = Overflow.Hidden;
            _overlay.pickingMode = PickingMode.Position;
            _overlay.AddToClassList("appui--light");

            // ── Camera preview layer ──────────────────────────────────────────
            var cameraViewport = new VisualElement
            {
                name = "camera-viewport"
            };
            cameraViewport.style.position = Position.Absolute;
            cameraViewport.style.left = 0;
            cameraViewport.style.right = 0;
            cameraViewport.style.top = 0;
            cameraViewport.style.bottom = 0;
            cameraViewport.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            cameraViewport.pickingMode = PickingMode.Ignore;
            _overlay.Add(cameraViewport);

            // ── Viewfinder target frame (Center) ──────────────────────────────
            var maskOverlay = new VisualElement
            {
                name = "viewfinder-container"
            };
            maskOverlay.style.position = Position.Absolute;
            maskOverlay.style.left = 0;
            maskOverlay.style.right = 0;
            maskOverlay.style.top = 0;
            maskOverlay.style.bottom = 0;
            maskOverlay.style.justifyContent = Justify.Center;
            maskOverlay.style.alignItems = Align.Center;
            maskOverlay.pickingMode = PickingMode.Ignore;

            var scanFrame = new VisualElement
            {
                name = "scan-frame"
            };
            scanFrame.style.width = Length.Percent(78);
            // scanFrame.style.maxWidth = 360;
            scanFrame.style.height = 520;
            scanFrame.style.borderLeftColor = scanFrame.style.borderRightColor = scanFrame.style.borderTopColor = scanFrame.style.borderBottomColor = new Color(1f, 1f, 1f, 0.9f);
            scanFrame.style.borderLeftWidth = scanFrame.style.borderRightWidth = scanFrame.style.borderTopWidth = scanFrame.style.borderBottomWidth = 2.5f;
            scanFrame.style.borderTopLeftRadius = scanFrame.style.borderTopRightRadius = scanFrame.style.borderBottomLeftRadius = scanFrame.style.borderBottomRightRadius = 16;
            scanFrame.style.backgroundColor = new Color(0, 0, 0, 0.15f);
            scanFrame.style.justifyContent = Justify.Center;
            scanFrame.style.alignItems = Align.Center;
            scanFrame.pickingMode = PickingMode.Ignore;

            // Loading label shown while camera is warming up
            var loadingLabel = new Text
            {
                text = "...",
                name = "camera-loading-label"
            };
            loadingLabel.style.color = new Color(1f, 1f, 1f, 0.6f);
            loadingLabel.style.fontSize = 14;
            loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            scanFrame.Add(loadingLabel);

            // Subtle scan laser guideline
            var laserLine = new VisualElement();
            laserLine.style.position = Position.Absolute;
            laserLine.style.left = 12;
            laserLine.style.right = 12;
            laserLine.style.top = Length.Percent(50);
            laserLine.style.height = 4;
            laserLine.style.backgroundColor = new Color(0.2f, 0.85f, 0.45f, 0.8f);
            laserLine.pickingMode = PickingMode.Ignore;
            scanFrame.Add(laserLine);

            maskOverlay.Add(scanFrame);

            _overlay.Add(maskOverlay);

            // ── Top App Bar (Safe Area Top) ───────────────────────────────────
            var themeService = App.current?.services?.GetService<IThemeService>();

            var header = new VisualElement
            {
                name = "scan-header"
            };
            header.style.position = Position.Absolute;
            header.style.top = 0;
            header.style.left = 0;
            header.style.right = 0;
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 35;
            header.style.paddingRight = 35;
            header.style.paddingTop = 35;
            header.style.paddingBottom = 35;

            themeService?.ApplySafeAreaPadding(header, true, false, false, false);

            var closeBtn = new IconButton
            {
                icon = "fm-arrow-left",
                size = Size.L,
                quiet = false
            };
            // closeBtn.variant = IconVariant.Light;
            closeBtn.AddToClassList("fm-button");
            closeBtn.style.backgroundColor = new Color(0, 0, 0, 0.55f);
            closeBtn.style.color = Color.white;
            //closeBtn.style.borderTopLeftRadius = closeBtn.style.borderTopRightRadius = closeBtn.style.borderBottomLeftRadius = closeBtn.style.borderBottomRightRadius = 24;
            closeBtn.clicked += () =>
            {
                Dismiss();
                _pendingCancelled?.Invoke();
                _pendingCancelled = null;
            };
            header.Add(closeBtn);

            var torchBtn = new IconButton
            {
                icon = "flashlight",
                size = Size.L,
                quiet = false
            };

            // torchBtn.variant = IconVariant.Regular;

            torchBtn.AddToClassList("fm-button");
            torchBtn.style.backgroundColor = new Color(0, 0, 0, 0.55f);
            torchBtn.style.color = Color.white;

            //torchBtn.style.borderTopLeftRadius = torchBtn.style.borderTopRightRadius = torchBtn.style.borderBottomLeftRadius = torchBtn.style.borderBottomRightRadius = 24;
            torchBtn.clicked += () =>
            {
                if (_controller != null)
                {
                    bool isOn = _controller.ToggleTorch();
                    if (isOn)
                    {
                        torchBtn.style.backgroundColor = new Color(1.0f, 0.85f, 0.2f, 0.95f);
                        torchBtn.style.color = Color.black;
                    }
                    else
                    {
                        torchBtn.style.backgroundColor = new Color(0, 0, 0, 0.55f);
                        torchBtn.style.color = Color.white;
                    }
                }
            };
            header.Add(torchBtn);

            _overlay.Add(header);

            // ── Bottom Help Banner (Safe Area Bottom) ─────────────────────────
            var footer = new VisualElement
            {
                name = "scan-footer"
            };
            footer.style.position = Position.Absolute;
            footer.style.bottom = 0;
            footer.style.left = 0;
            footer.style.right = 0;
            footer.style.alignItems = Align.Center;
            footer.style.paddingLeft = 24;
            footer.style.paddingRight = 24;
            footer.style.paddingBottom = 32;
            footer.style.paddingTop = 16;
            footer.pickingMode = PickingMode.Ignore;

            themeService?.ApplySafeAreaPadding(footer, false, true, false, false);

            var helpPill = new VisualElement();
            helpPill.style.backgroundColor = new Color(0, 0, 0, 0.65f);
            helpPill.style.paddingLeft = 30;
            helpPill.style.paddingRight = 30;
            helpPill.style.paddingTop = 20;
            helpPill.style.paddingBottom = 20;
            helpPill.style.borderTopLeftRadius = helpPill.style.borderTopRightRadius = helpPill.style.borderBottomLeftRadius = helpPill.style.borderBottomRightRadius = 20;
            helpPill.pickingMode = PickingMode.Ignore;

            var guide = new LocalizedTextElement();
            guide.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SCAN_HELP") ?? "Coloca el código de barras en el marco";
            //guide.style.color = Color.white;
            //guide.style.fontSize = 15;
            guide.style.unityTextAlign = TextAnchor.MiddleCenter;
            guide.style.whiteSpace = WhiteSpace.Normal;
            helpPill.Add(guide);

            footer.Add(helpPill);
            _overlay.Add(footer);

            // ── Build and show Modal ──────────────────────────────────────────
            _modal = Modal.Build(anchor, _overlay);
            _modal.SetFullScreenMode(ModalFullScreenMode.FullScreenTakeOver);

            if (themeService != null)
            {
                void ApplySafeArea()
                {
                    themeService.ApplySafeAreaPadding(header, true, false, false, false);
                    themeService.ApplySafeAreaPadding(footer, false, true, false, false);
                }
                themeService.SafeAreaChanged += ApplySafeArea;
                _modal.dismissed += (_, _) =>
                {
                    themeService.SafeAreaChanged -= ApplySafeArea;
                };
            }

            _modal.dismissed += (_, dismissType) =>
            {
                if (_controller != null)
                {
                    _controller.Stop();
                    UnityEngine.Object.Destroy(_controller.gameObject);
                    _controller = null;
                }

                if (dismissType == DismissType.Manual)
                {
                    _pendingCancelled?.Invoke();
                }

                _overlay = null;
                _modal = null;
                _pendingCancelled = null;
            };

            _modal.Show();

            // ── Camera Controller ─────────────────────────────────────────────
            var go = new GameObject("BarcodeScanner");
            _controller = go.AddComponent<BarcodeScanOverlayController>();
            _controller.Initialize(cameraViewport, loadingLabel, () =>
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

            if (_modal != null)
            {
                var m = _modal;
                _modal = null;
                m.Dismiss(DismissType.Action);
            }

            _overlay = null;
            _pendingCancelled = null;
        }
    }

    internal class BarcodeScanOverlayController : MonoBehaviour
    {
        private VisualElement _overlay;
        private VisualElement _loadingLabel;
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
            VisualElement loadingLabel,
            Action onCancelled,
            Action<string> onDetected)
        {
            _overlay = overlay;
            _loadingLabel = loadingLabel;
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

            if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("[BarcodeScanner] No camera devices found on this system.");
                _isRunning = false;
                _onCancelled?.Invoke();
                yield break;
            }

            _webCamTexture = new WebCamTexture(1280, 720, 30);
            _webCamTexture.Play();

            // Wait up to 10 seconds for camera to negotiate stream and deliver valid dimensions
            float timeout = 10.0f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                if (!_isRunning) yield break;
                if (_webCamTexture != null && _webCamTexture.isPlaying && _webCamTexture.width > 100)
                {
                    break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_webCamTexture == null || !_webCamTexture.isPlaying || _webCamTexture.width < 100)
            {
                Debug.LogWarning("[BarcodeScanner] Camera initialization timed out.");
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

                if (_loadingLabel != null)
                {
                    _loadingLabel.style.display = DisplayStyle.None;
                }
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
                        int dstYRow = (srcH - 1 - y) * srcW;
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
