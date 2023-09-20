using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HRYooba.Library
{
    [Serializable]
    public class MediaPlayerPlayableClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MediaPlayerPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            var director = owner.GetComponent<PlayableDirector>();
            behaviour.SetPlayableDirector(director);

            return playable;
        }
    }
}