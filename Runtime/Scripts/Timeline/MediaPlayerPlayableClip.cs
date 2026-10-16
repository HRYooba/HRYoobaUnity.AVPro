using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HRYooba.AVPro
{
    [Serializable]
    public class MediaPlayerPlayableClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private bool _autoRewind = true;
        [SerializeField] private bool _destroyClose = false;

        public bool AutoRewind => _autoRewind;
        public bool DestroyClose => _destroyClose;

        // 再生位置はクリップのローカル時刻から求めるので、Clip In はそのまま映像の開始位置になる。
        // Speed は Buffered の再生速度に反映しないため持たせない
        public ClipCaps clipCaps => ClipCaps.ClipIn;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MediaPlayerPlayableBehaviour>.Create(graph);
            playable.GetBehaviour().AutoRewind = _autoRewind;
            return playable;
        }
    }
}
