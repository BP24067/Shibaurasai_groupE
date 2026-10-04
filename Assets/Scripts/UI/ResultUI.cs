using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// ゴール時の結果画面を表示し、一定時間後または発声でReady画面に戻すスクリプト
/// </summary>

public class ResultUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager  gameManager;
    [SerializeField] MicInput mic;  //発生検知用
    [SerializeField] GameObject resultPanel;//結果画面全体の親オブジェクト
    [SerializeField] TextMeshProUGUI clearTimeText;//クリアタイム表示用テキスト

    [Header("設定")]
    [Tooltip("ゴール後に自動でReadyに戻るまでの待ち時間(秒)")]
    [SerializeField] float autoResetDelay = 10f;

    [Tooltip("ゴール後誤反応を防ぐrために発生検知を受け付けない時間(秒)")]
    [SerializeField] float inputBlockTime = 1.5f;

    [Tooltip("Readyに戻すための発生しきい値(MicInputのLevel)")]
    [SerializeField, Range(0f, 1f)] float voiceThreshold = 0.3f;

    bool isShowingResult = false;
    float resultTimer= 0f;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //最初は非表示
        if(resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        //GameMangerのイベントに登録
        if(gameManager != null)
        {
            gameManager.OnStateChanged += OnGameStateChanged;
        }
        else
        {
            Debug.LogError("[ResultUI] GameManagerが設定されていません。");
        }
    }

    void OnDestroy()
    {
        //イベント登録の解除、エラー防止
        if(gameManager != null)
        {
            gameManager.OnStateChanged -= OnGameStateChanged;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!isShowingResult) return;

        resultTimer += Time.deltaTime;

        //一定時間経過でReadyに戻す
        if(resultTimer >= autoResetDelay)
        {
            ReturnToReady();
            return;
        }

        //誤反応防止時間(inputBlockTime)を過ぎた後、一定の発生でReadyに戻る
        if(resultTimer >= inputBlockTime && mic != null)
        {
            if(mic.Level >= voiceThreshold)
            {
                ReturnToReady();
            }
        }
    }

    /// <summary>
    /// GameMangerの状態が変化した時に呼ばれる
    /// </summary>
    void OnGameStateChanged(GameState state)
    {
        if(state == GameState.Goal)
        {
            ShowResult();
        }
        else if(state == GameState.Ready)
        {
            HideResult();
        }
    }

    /// <summary>
    /// 結果画面の表示処理
    /// </summary>
    void ShowResult()
    {
        isShowingResult = true;
        resultTimer = 0f;

        if(resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        //クリアタイムの表示更新
        if(clearTimeText != null && gameManager != null)
        {
            float time = gameManager.ElapsedTime;
            int minutes = Mathf.FloorToInt(time /60f);
            float seconds = time % 60f;
            clearTimeText.text = $"TIME: {minutes:00}:{seconds:00.00}";
        }
    }

    /// <summary>
    /// 結果画面の非表示処理
    /// </summary>
    void HideResult()
    {
        isShowingResult = false;
        if(resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Readyに戻す処理
    /// </summary>
    void ReturnToReady()
    {
        if(!isShowingResult) return;

        HideResult();

        //GameManagerをReadyに戻す
        if(gameManager != null)
        {
            gameManager.ResetToReady();
        }
    }
}
