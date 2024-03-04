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
        [SerializeField, HideInInspector] private Shader _shader = null;

        private Material _material = null;
        private RenderTexture _renderTexture = null;

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

        private void OnDestory()
        {
            if (_material != null) DestroyImmediate(_material);
            if (_renderTexture != null) DestroyImmediate(_renderTexture);
        }

        private void LateUpdate()
        {
            if (_mediaPlayer == null) return;
            if (_image == null) return;

            var textureSource = _mediaPlayer.TextureProducer;
            if (textureSource != null && textureSource.GetTexture() != null)
            {
                var texture = textureSource.GetTexture();

                if (_material == null)
                {
                    _material = new Material(_shader);
                }
                if (_renderTexture == null)
                {
                    _renderTexture = new RenderTexture(texture.width, texture.height, 0);
                }

                // テクスチャサイズが変わったらRenderTextureを再生成
                if (_renderTexture.width != texture.width || _renderTexture.height != texture.height)
                {
                    _renderTexture.Release();
                    _renderTexture.width = texture.width;
                    _renderTexture.height = texture.height;
                    _renderTexture.Create();
                }

                // AVProのテクスチャは上下反転しているので反転させる
                Graphics.Blit(texture, _renderTexture, _material);
                _image.texture = _renderTexture;
            }
            else
            {
                _image.texture = _defaultTexture;
            }
        }
    }
}