using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// VOICEゲージの表示およびキャリブレーション状態、しきい値ラインを制御するUIスクリプト。
    /// </summary>
    public class VoiceGaugeUI : MonoBehaviour
    {
        [Header("連携スクリプト")]
        [SerializeField] private MicInput micInput;
        [SerializeField] private MicCalibration micCalibration;

        [Header("UIコンポーネント")]
        [Tooltip("VOICEゲージ本体（Image Type は Filled に設定）")]
        [SerializeField] private Image gaugeImage;

        [Tooltip("しきい値を示すラインの RectTransform")]
        [SerializeField] private RectTransform thresholdLine;

        [Tooltip("キャリブレーション中の進捗を表示する Image（任意・Image Type は Filled）")]
        [SerializeField] private Image calibrationProgressImage;

        [Header("表示設定")]
        [Tooltip("キャリブレーション中にゲージの色を変更する場合の色")]
        [SerializeField] private Color calibratingColor = Color.yellow;

        [Tooltip("しきい値ラインの相対位置計算用最小dB（デフォルト: -80dB）")]
        [SerializeField] private float minDb = -80f;

        private Color originalGaugeColor;

        private void Awake()
        {
            if (gaugeImage != null)
            {
                originalGaugeColor = gaugeImage.color;
            }
        }

        private void OnEnable()
        {
            if (micCalibration != null)
            {
                micCalibration.Finished += OnCalibrationFinished;
            }
        }

        private void OnDisable()
        {
            if (micCalibration != null)
            {
                micCalibration.Finished -= OnCalibrationFinished;
            }
        }

        private void Update()
        {
            UpdateGauge();
            UpdateThresholdLine();
        }

        /// <summary>
        /// VOICEゲージの fillAmount および表示色の更新
        /// </summary>
        private void UpdateGauge()
        {
            if (micInput == null || gaugeImage == null) return;

            // キャリブレーション中の処理
            if (micCalibration != null && micCalibration.IsCalibrating)
            {
                if (calibrationProgressImage != null)
                {
                    calibrationProgressImage.enabled = true;
                    calibrationProgressImage.fillAmount = micCalibration.Progress;
                }
                else
                {
                    // 専用UIがない場合はメインゲージの色を変えて進捗を表示
                    gaugeImage.color = calibratingColor;
                    gaugeImage.fillAmount = micCalibration.Progress;
                }
            }
            else
            {
                // 通常時：MicInput の Level (0〜1) を反映
                if (calibrationProgressImage != null)
                {
                    calibrationProgressImage.enabled = false;
                }

                gaugeImage.color = originalGaugeColor;
                gaugeImage.fillAmount = micInput.Level; // 主な実装内容：Image.fillAmount = Level
            }
        }

        /// <summary>
        /// しきい値（NoiseFloorDb）ラインの位置更新
        /// </summary>
        private void UpdateThresholdLine()
        {
            if (thresholdLine == null || micInput == null) return;

            RectTransform parentRect = thresholdLine.parent as RectTransform;
            if (parentRect == null) return;

            // -80dB 〜 MaxDb の音量範囲の中で NoiseFloorDb がどの位置(0〜1)にあるかを算出
            float normalizedThreshold = Mathf.InverseLerp(minDb, micInput.MaxDb, micInput.NoiseFloorDb);

            // ゲージの高さ（Y軸方向）に合わせてラインの Y 座標を設定
            float parentHeight = parentRect.rect.height;
            Vector2 anchoredPos = thresholdLine.anchoredPosition;
            anchoredPos.y = parentHeight * normalizedThreshold;
            thresholdLine.anchoredPosition = anchoredPos;
        }

        /// <summary>
        /// キャリブレーション完了時のイベントハンドラ
        /// </summary>
        private void OnCalibrationFinished()
        {
            UpdateThresholdLine();
        }
    }
}