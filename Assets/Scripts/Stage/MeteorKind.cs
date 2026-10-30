/// <summary>
/// 隕石の種類（色）。当たったときの作用は GameManager.NotifyHit() が種類ごとに決める。
///   Normal … 灰：ふつうの減速
///   Red    … 赤：大きく減速し、加速できない時間も長い
///   Gold   … 金：当たると加速（ブースト）。取りに行く良い隕石
///   Purple … 紫：しばらく左右の操作が逆になる
/// </summary>
public enum MeteorKind
{
    Normal,
    Red,
    Gold,
    Purple,
}
