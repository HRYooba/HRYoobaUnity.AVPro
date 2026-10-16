using System;
using UnityEngine.Playables;

namespace HRYooba.AVPro
{
    /// <summary>
    /// クリップごとの設定を MediaPlayerPlayableMixerBehaviour へ渡す入れ物。MediaPlayer の操作はミキサーだけが行う
    /// </summary>
    [Serializable]
    public class MediaPlayerPlayableBehaviour : PlayableBehaviour
    {
        /// <summary>クリップ区間を抜けたら先頭へ戻して止めるか</summary>
        public bool AutoRewind { get; set; }
    }
}
