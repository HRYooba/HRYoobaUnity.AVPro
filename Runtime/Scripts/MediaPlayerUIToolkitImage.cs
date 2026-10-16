using UnityEngine;
using UnityEngine.UIElements;
using RenderHeads.Media.AVProVideo;

namespace HRYooba.AVPro
{
    /// <summary>
    /// MediaPlayer の映像を UI Toolkit の要素に出す。MediaPlayerPlayableTrack が制御中は Timeline の時刻のコマを出す
    /// </summary>
    /// <remarks>
    /// 要素が <see cref="Image"/> なら image に、それ以外なら背景画像に設定する。
    /// </remarks>
    [ExecuteAlways]
    public class MediaPlayerUIToolkitImage : MonoBehaviour
    {
        [SerializeField] private MediaPlayer _mediaPlayer = null;
        [SerializeField] private UIDocument _document = null;
        [SerializeField, Tooltip("映像を出す要素の name")] private string _elementName = string.Empty;
        [SerializeField, Tooltip("要素が Image のときの拡大縮小")] private ScaleMode _scaleMode = ScaleMode.ScaleToFit;
        [SerializeField, Tooltip("映像が無いときに出すテクスチャ")] private Texture _defaultTexture = null;

        private readonly MediaPlayerDisplayTexture _displayTexture = new MediaPlayerDisplayTexture();
        private VisualElement _element;
        private Texture _assigned;

        public MediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            set => _mediaPlayer = value;
        }

        private void OnDisable()
        {
            Assign(null);
            _element = null;
            _displayTexture.Dispose();
        }

        // Timeline（DirectorUpdate）がコマを選んだ後に読む
        private void LateUpdate()
        {
            var element = FindElement();
            if (element != _element)
            {
                Assign(null);
                _element = element;
            }
            if (_element == null) return;

            var texture = _displayTexture.Get(_mediaPlayer);
            Assign(texture != null ? texture : _defaultTexture);

            // RenderTexture の中身が変わっても UI Toolkit は再描画しないので、毎フレーム描き直させる
            _element.MarkDirtyRepaint();
        }

        private VisualElement FindElement()
        {
            // UIDocument は有効化のたびに要素を作り直すので、毎フレーム引き直す
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null) return null;
            return string.IsNullOrEmpty(_elementName) ? root : root.Q(_elementName);
        }

        private void Assign(Texture texture)
        {
            if (_element == null || texture == _assigned) return;
            _assigned = texture;

            if (_element is Image image)
            {
                image.image = texture;
                image.scaleMode = _scaleMode;
            }
            else
            {
                _element.style.backgroundImage = texture switch
                {
                    RenderTexture renderTexture => new StyleBackground(Background.FromRenderTexture(renderTexture)),
                    Texture2D texture2D => new StyleBackground(texture2D),
                    _ => new StyleBackground(StyleKeyword.None),
                };
            }
        }
    }
}
