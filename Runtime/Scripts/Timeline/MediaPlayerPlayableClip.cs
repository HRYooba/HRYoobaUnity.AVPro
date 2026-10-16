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

        // 再生位置はクリップのローカル時刻から求めるので、Clip In と Speed はそのまま映像の位置と速さになる
        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MediaPlayerPlayableBehaviour>.Create(graph);
            playable.GetBehaviour().AutoRewind = _autoRewind;
            return playable;
        }
    }
}
