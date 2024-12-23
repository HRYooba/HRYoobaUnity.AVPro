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
        public bool IsAutoRewind { get; set; }
        public MediaPlayer MediaPlayer { get; set; }
        public PlayableDirector Director { get; set; }

        private FieldInfo _isMediaOpenedField;

        public override void OnGraphStart(Playable playable)
        {
        }

        public override void OnGraphStop(Playable playable)
        {
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (MediaPlayer == null || Director == null) return;

            if (MediaPlayer.AutoStart)
            {
                Debug.LogWarning($"MediaPlayerPlayableBehaviour: Target MediaPlayer.AutoStart[{MediaPlayer.gameObject.name}] is enabled. Please disable to avoid conflicts with Timeline.");
            }

            if (!MediaPlayer.gameObject.activeSelf) return;
            if (!MediaPlayer.MediaOpened)
            {
                MediaPlayer.OpenMedia(MediaPlayer.AutoStart);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (MediaPlayer == null || Director == null) return;
            
            if (IsAutoRewind) 
            {
                MediaPlayer.Rewind(true);
            }

            // if (!Application.isPlaying && MediaPlayer.MediaOpened)
            // {
            //     MediaPlayer.CloseMedia();
            // }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (MediaPlayer == null || Director == null) return;

            if (MediaPlayer.Control == null)
            {
                if (MediaPlayer.MediaOpened)
                {
                    if (_isMediaOpenedField == null) _isMediaOpenedField = typeof(MediaPlayer).GetField("_isMediaOpened", BindingFlags.NonPublic | BindingFlags.Instance);
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
            }
        }
    }
}