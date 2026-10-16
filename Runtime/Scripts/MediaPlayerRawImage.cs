using UnityEngine;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// MediaPlayer の映像を uGUI の RawImage に出す。MediaPlayerPlayableTrack が制御中は Timeline の時刻のコマを出す
    /// </summary>
    /// <remarks>
    /// 同じ GameObject に AspectRatioFitter があれば、映像の縦横比を入れる。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class MediaPlayerRawImage : MonoBehaviour
    {
        [SerializeField] private MediaPlayer _mediaPlayer = null;
        [SerializeField, Tooltip("映像が無いときに出すテクスチャ")] private Texture _defaultTexture = null;

        private readonly MediaPlayerDisplayTexture _displayTexture = new MediaPlayerDisplayTexture();
        private RawImage _image;

        public MediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            set => _mediaPlayer = value;
        }

        private void OnEnable()
        {
            _image = GetComponent<RawImage>();
        }

        private void OnDisable()
        {
            if (_image != null) _image.texture = _defaultTexture;
            _displayTexture.Dispose();
        }

        // Timeline（DirectorUpdate）がコマを選んだ後に読む
        private void LateUpdate()
        {
            if (_image == null) return;

            var texture = _displayTexture.Get(_mediaPlayer);
            var next = texture != null ? texture : _defaultTexture;
            if (_image.texture != next) _image.texture = next;

            if (next != null && next.height > 0 && TryGetComponent(out AspectRatioFitter fitter))
            {
                var aspectRatio = (float)next.width / next.height;
                if (!Mathf.Approximately(fitter.aspectRatio, aspectRatio)) fitter.aspectRatio = aspectRatio;
            }
        }
    }
}
