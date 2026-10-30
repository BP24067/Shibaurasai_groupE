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
///   D              次のマイクに切り替える（切り替えたあと、キャリブレーションをやり直す）
///   I（押している間）「いー」と言う → 右側の音色として記録
///   O（押している間）「おー」と言う → 左側の音色として記録
///                  両方そろうと、音色の中心（TimbreCenterDb）と幅（TimbreRangeDb）を自動で合わせる
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

    // 音色の記録（キーを押している間の平均）
    float capturedHighDb = float.NaN, capturedLowDb = float.NaN;
    double captureSum;
    int captureCount;

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
        if (keyboard.dKey.wasPressedThisFrame && deviceSelector != null)
        {
            deviceSelector.SelectNext();
            if (calibration != null) calibration.StartCalibration();   // マイクが変わると周囲の音量も変わるので測り直す
        }
        UpdateTimbreCapture(keyboard.iKey.isPressed, keyboard.iKey.wasReleasedThisFrame, true);
        UpdateTimbreCapture(keyboard.oKey.isPressed, keyboard.oKey.wasReleasedThisFrame, false);

        if (infoText != null) infoText.text = BuildText();
    }

    // キーを押している間の音色を平均し、離したときに記録する。両方そろったら中心と幅を合わせる
    void UpdateTimbreCapture(bool held, bool released, bool high)
    {
        if (held && mic.IsVoiced && !float.IsNaN(mic.TimbreDb))
        {
            captureSum += mic.TimbreDb;
            captureCount++;
        }
        if (!released) return;

        if (captureCount > 0)
        {
            float average = (float)(captureSum / captureCount);
            if (high) capturedHighDb = average; else capturedLowDb = average;
            if (!float.IsNaN(capturedHighDb) && !float.IsNaN(capturedLowDb) && capturedHighDb > capturedLowDb)
            {
                mic.TimbreCenterDb = (capturedHighDb + capturedLowDb) / 2f;
                mic.TimbreRangeDb = (capturedHighDb - capturedLowDb) / 2f;
            }
        }
        captureSum = 0;
        captureCount = 0;
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
                sb.AppendLine($"Calib  : ambient {calibration.LastAmbientDb:F1} dB  spread {calibration.LastSpreadDb:F1} dB  tries {calibration.LastAttempts}");
            else
                sb.AppendLine("Calib  : not yet");
        }

        // 音色（左右）
        string raw = float.IsNaN(mic.TimbreDb) ? "  --  " : $"{mic.TimbreDb,6:F1}";
        int half = barLength / 2;
        int pos = Mathf.Clamp(Mathf.RoundToInt((mic.Timbre + 1f) * 0.5f * barLength), 0, barLength);
        var bar = new char[barLength + 1];
        for (int i = 0; i <= barLength; i++) bar[i] = i == pos ? '#' : i == half ? '|' : '.';
        sb.AppendLine($"Timbre : {raw} dB -> {mic.Timbre:+0.00;-0.00} [{new string(bar)}]");
        string fmt(float v) => float.IsNaN(v) ? "--" : v.ToString("F1");
        sb.AppendLine($"         center {mic.TimbreCenterDb:F1}  range {mic.TimbreRangeDb:F1}   oo:{fmt(capturedLowDb)}  ii:{fmt(capturedHighDb)}");

        sb.AppendLine("[C] Recalibrate   [D] Next device");
        sb.Append("[I] hold + say \"ii-\" (right)   [O] hold + say \"oo-\" (left)");
        return sb.ToString();
    }
}
