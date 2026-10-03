using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 会場でマイクを調整するためのデバッグ画面。F1 で表示／非表示。
/// このスクリプトは常に有効な親オブジェクト（Canvas/Debug）に付け、root には非表示にする子（DebugRoot）を指定する。
///
/// 操作（表示中のみ）:
///   ↑ / ↓          しきい値（NoiseFloorDb）を上げる／下げる
///   Shift + ↑ / ↓  最大音量（MaxDb）を上げる／下げる
///   C              キャリブレーションをやり直す
///   D              次のマイクに切り替える
///
/// ※ TextMeshPro の標準フォントは日本語を表示できないため、画面の文字は英語にしている。
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [SerializeField] MicInput mic;
    [SerializeField] MicCalibration calibration;
    [SerializeField] MicDeviceSelector deviceSelector;
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text infoText;
    [SerializeField] float stepDb = 1f;
    [SerializeField] int barLength = 30;

    readonly StringBuilder sb = new StringBuilder();

    void Start()
    {
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || root == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) root.SetActive(!root.activeSelf);
        if (!root.activeSelf) return;

        bool shift = keyboard.shiftKey.isPressed;
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            if (shift) mic.MaxDb += stepDb; else mic.NoiseFloorDb += stepDb;
        }
        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            if (shift) mic.MaxDb -= stepDb; else mic.NoiseFloorDb -= stepDb;
        }
        if (keyboard.cKey.wasPressedThisFrame && calibration != null) calibration.StartCalibration();
        if (keyboard.dKey.wasPressedThisFrame && deviceSelector != null) deviceSelector.SelectNext();

        if (infoText != null) infoText.text = BuildText();
    }

    string BuildText()
    {
        sb.Clear();
        sb.AppendLine("<b>MIC DEBUG</b>  (F1: close)");
        sb.AppendLine($"Device : {(mic.IsRecording ? mic.DeviceName : "none  (Space key fallback)")}");
        sb.AppendLine($"dB     : {mic.CurrentDb,6:F1}");
        sb.AppendLine($"Floor  : {mic.NoiseFloorDb,6:F1}   [Up / Down]");
        sb.AppendLine($"Max    : {mic.MaxDb,6:F1}   [Shift + Up / Down]");

        int filled = Mathf.RoundToInt(mic.Level * barLength);
        sb.Append($"Level  : {mic.Level:F2}  [");
        sb.Append('|', filled).Append('.', barLength - filled).AppendLine("]");

        if (calibration != null)
        {
            if (calibration.IsCalibrating)
                sb.AppendLine($"Calib  : measuring... {calibration.Progress * 100f:F0}%  (stay quiet)");
            else if (!float.IsNaN(calibration.LastAmbientDb))
                sb.AppendLine($"Calib  : ambient {calibration.LastAmbientDb:F1} dB");
            else
                sb.AppendLine("Calib  : not yet");
        }

        sb.Append("[C] Recalibrate   [D] Next device");
        return sb.ToString();
    }
}
