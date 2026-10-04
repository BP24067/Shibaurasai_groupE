/// <summary>
/// ゲームの状態。現在の状態は GameManager.State で取得し、変化は GameManager.OnStateChanged で受け取る。
/// </summary>
public enum GameState
{
    /// <summary>開始待ち。キャリブレーション後、声を出すとスタートする。</summary>
    Ready,

    /// <summary>プレイ中。声の大きさで加速し、ゴールを目指す。</summary>
    Playing,

    /// <summary>ゴール到達。ワープ演出と結果表示を行う。</summary>
    Goal,
}
