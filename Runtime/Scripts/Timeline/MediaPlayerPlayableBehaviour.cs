using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    [Serializable]
    public class MediaPlayerPlayableBehaviour : PlayableBehaviour
    {
        public bool AutoRewind { get; set; }
        public bool DestroyClose { get; set; }
        public MediaPlayer MediaPlayer { get; set; }
        public PlayableDirector Director { get; set; }

        private FieldInfo _isMediaOpenedField;

        public override void OnPlayableCreate(Playable playable)
        {
            if (_isMediaOpenedField == null)
            {
                _isMediaOpenedField = typeof(MediaPlayer).GetField("_isMediaOpened", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (MediaPlayer == null || Director == null) return;

            if (DestroyClose)
            {
                MediaPlayer.CloseMedia();
            }
        }

        // public override void OnGraphStart(Playable playable)
        // {
        // }

        // public override void OnGraphStop(Playable playable)
        // {
        // }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (MediaPlayer == null || Director == null) return;

            if (MediaPlayer.AutoStart)
            {
                Debug.LogWarning($"MediaPlayerPlayableBehaviour: Target MediaPlayer.AutoStart[{MediaPlayer.gameObject.name}] is enabled. Please disable to avoid conflicts with Timeline.");
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (MediaPlayer == null || Director == null) return;

            if (AutoRewind)
            {
                MediaPlayer.Rewind(true);
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (MediaPlayer == null || Director == null) return;
            if (!MediaPlayer.gameObject.activeInHierarchy) return;

            if (MediaPlayer.Control == null)
            {
                if (MediaPlayer.MediaOpened)
                {
                    _isMediaOpenedField.SetValue(MediaPlayer, false);
                }
                else
                {
                    MediaPlayer.OpenMedia(MediaPlayer.AutoStart);
                }
            }
            else
            {
                if (MediaPlayer.MediaOpened)
                {
                    var time = playable.GetTime();
                    MediaPlayer.Control.Seek(time);

#if UNITY_EDITOR
                    if (!Application.isPlaying) MediaPlayer.EditorUpdate();
#endif
                }
                else
                {
                    MediaPlayer.OpenMedia(MediaPlayer.AutoStart);
                }
            }
        }
    }
}