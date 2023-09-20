using UnityEngine;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.Library
{
    [ExecuteAlways]
    public class ApplyToRawImage : MonoBehaviour
    {
        [SerializeField] private Texture _defaultTexture = null;
        [SerializeField] private RawImage _image = null;
        [SerializeField] private MediaPlayer _mediaPlayer = null;

        private void OnEnable()
        {
            if (_image == null) return;
            
            _image.texture = _defaultTexture;
        }

        private void OnDisable()
        {
            if (_image == null) return;
            
            _image.texture = _defaultTexture;
        }

        private void Update()
        {
            if (_mediaPlayer == null) return;
            if (_image == null) return;

            var textureSource = _mediaPlayer.TextureProducer;
            if (textureSource != null && textureSource.GetTexture() != null)
            {
                _image.texture = textureSource.GetTexture();
            }
            else
            {
                _image.texture = _defaultTexture;
            }
        }
    }
}