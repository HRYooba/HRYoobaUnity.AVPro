using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    [Serializable]
    public class MediaPlayerPlayableClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private bool _autoRewind = true;
        [SerializeField] private bool _destroyClose = false;

        public MediaPlayer MediaPlayer { get; set; }
        public PlayableDirector Director { get; set; }
        public TimelineClip Clip { get; set; }

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MediaPlayerPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.AutoRewind = _autoRewind;
            behaviour.DestroyClose = _destroyClose;
            behaviour.MediaPlayer = MediaPlayer;
            behaviour.Director = Director;

            return playable;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(MediaPlayerPlayableClip))]
    public class MediaPlayerPlayableClipEditor : UnityEditor.Editor
    {
        private MediaPlayerPlayableClip _target;

        private void OnEnable()
        {
            _target = target as MediaPlayerPlayableClip;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (_target.MediaPlayer == null) return;
            if (_target.MediaPlayer.Info == null) return;

            UnityEditor.EditorGUILayout.Space(10);
            
            var duration = _target.MediaPlayer.Info.GetDuration();
            var durationFrames = _target.MediaPlayer.Info.GetDurationFrames();
            var frameRate = _target.MediaPlayer.Info.GetVideoFrameRate();

            UnityEditor.EditorGUILayout.LabelField("Duration", $"{duration} sec ({durationFrames} frames@{frameRate}fps)");

            // clipのdurationを変更するボタン
            if (GUILayout.Button("Set Duration"))
            {
                _target.Clip.duration = duration;
            }
        }
    }
#endif
}