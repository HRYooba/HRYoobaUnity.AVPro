using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    [ExecuteAlways]
    [RequireComponent(typeof(MediaPlayer))]
    public class MediaPlayerTimelineHandler : MonoBehaviour, ITimeControl, IPropertyPreview
    {
        private MediaPlayer _mediaPlayer;
        private double _time = 0.0;
        private bool _isControlling = false;

        private FieldInfo _isMediaOpenedField;

        private void Awake()
        {
            _mediaPlayer = GetComponent<MediaPlayer>();

            if (_mediaPlayer.AutoStart)
            {
                Debug.LogWarning($"MediaPlayerTimelineHandler: Target MediaPlayer.AutoStart is enabled. Please disable to avoid conflicts with Timeline.");
            }
        }

        private void LateUpdate()
        {
            if (!_isControlling) return;
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.Control == null)
            {
                if (_mediaPlayer.MediaOpened)
                {
                    if (_isMediaOpenedField == null) _isMediaOpenedField = typeof(MediaPlayer).GetField("_isMediaOpened", BindingFlags.NonPublic | BindingFlags.Instance);
                    _isMediaOpenedField.SetValue(_mediaPlayer, false);
                }
                else
                {
                    _mediaPlayer.OpenMedia(_mediaPlayer.AutoStart);
                }
            }
            else
            {
                if (_mediaPlayer.MediaOpened)
                {

                    _mediaPlayer.Control.Seek(_time);

#if UNITY_EDITOR
                    if (!Application.isPlaying) _mediaPlayer.EditorUpdate();
#endif
                }
            }
        }

        public void OnControlTimeStart()
        {
            _isControlling = true;

            if (!gameObject.activeSelf) return;
            if (!_mediaPlayer.MediaOpened)
            {
                _mediaPlayer.OpenMedia(_mediaPlayer.AutoStart);
            }
        }

        public void OnControlTimeStop()
        {
            _isControlling = false;

            // if (!Application.isPlaying && _mediaPlayer.MediaOpened)
            // {
            //     _mediaPlayer.CloseMedia();
            // }
        }

        public void SetTime(double time)
        {
            _time = time;
        }

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            // driver.AddFromName<MediaPlayerTimelineHandler>(gameObject, "_time");
        }
    }
}