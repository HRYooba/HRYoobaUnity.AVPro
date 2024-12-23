using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;

#if UNITY_EDITOR
using UnityEditor;
#endif  

namespace HRYooba.AVPro
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class AlwaysExecuteDisplayUGUI : DisplayUGUI
    {
#if UNITY_EDITOR
        private MethodInfo _updateInternalMaterialMethod;
        private MethodInfo _getDrawingDimensionsMethod;

        private Texture _lastTexture_always;
        private int _lastWidth_always;
        private int _lastHeight_always;
        private Orientation _lastOrientation_always;
        private bool _flipY_always;
        private List<UIVertex> _vertices_always = new List<UIVertex>(4);
        private static List<int> QuadIndices_always = new List<int>(new int[] { 0, 1, 2, 2, 3, 0 });

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) return;

            if (_updateInternalMaterialMethod == null) _updateInternalMaterialMethod = typeof(DisplayUGUI).GetMethod("UpdateInternalMaterial", BindingFlags.Instance | BindingFlags.NonPublic);

            var _mediaPlayer = CurrentMediaPlayer;
            var _setNativeSize = ApplyNativeSize;
            var _isUserMaterial = (this.material != null);

            if (_setNativeSize)
            {
                SetNativeSize();
            }

            if (_lastTexture_always != mainTexture)
            {
                _lastTexture_always = mainTexture;
                SetVerticesDirty();
                SetMaterialDirty();
            }

            if (HasValidTexture_always())
            {
                if (mainTexture != null)
                {
                    Orientation orientation = Helper.GetOrientation(_mediaPlayer.Info.GetTextureTransform());
                    if (mainTexture.width != _lastWidth_always || mainTexture.height != _lastHeight_always || orientation != _lastOrientation_always)
                    {
                        _lastWidth_always = mainTexture.width;
                        _lastHeight_always = mainTexture.height;
                        _lastOrientation_always = orientation;
                        SetVerticesDirty();
                        SetMaterialDirty();
                    }
                }
            }

            // if (Application.isPlaying)
            // {
            if (!_isUserMaterial)
            {
                // UpdateInternalMaterial();
                _updateInternalMaterialMethod?.Invoke(this, null);
            }
            // }

            if (material != null && _mediaPlayer != null)
            {
                // TODO: only run when dirty
                VideoRender.SetupMaterialForMedia(materialForRendering, _mediaPlayer);
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (Application.isPlaying)
                {
                    return base.mainTexture;
                }

                var _mediaPlayer = CurrentMediaPlayer;
                var _defaultTexture = DefaultTexture;
                var _noDefaultDisplay = NoDefaultDisplay;
                var _displayInEditor = DisplayInEditor;

                Texture result = Texture2D.whiteTexture;
                if (HasValidTexture_always())
                {
                    Texture resamplerTex = _mediaPlayer.FrameResampler == null || _mediaPlayer.FrameResampler.OutputTexture == null ? null : _mediaPlayer.FrameResampler.OutputTexture[0];
                    result = _mediaPlayer.UseResampler ? resamplerTex : _mediaPlayer.TextureProducer.GetTexture();
                }
                else
                {
                    if (_noDefaultDisplay)
                    {
                        result = null;
                    }
                    else if (_defaultTexture != null)
                    {
                        result = _defaultTexture;
                    }

#if UNITY_EDITOR
                    if (result == null && _displayInEditor)
                    {
                        result = Resources.Load<Texture2D>("AVProVideoIcon");
                    }
#endif
                }
                return result;
            }
        }

        private bool HasValidTexture_always()
        {
            var _mediaPlayer = CurrentMediaPlayer;
            return (_mediaPlayer != null && _mediaPlayer.TextureProducer != null && _mediaPlayer.TextureProducer.GetTexture() != null);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (Application.isPlaying)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            vh.Clear();

            _OnFillVBO(_vertices_always);

            vh.AddUIVertexStream(_vertices_always, QuadIndices_always);
        }

        private void _OnFillVBO(List<UIVertex> vbo)
        {
            var _uvRect = UVRect;
            var _mediaPlayer = CurrentMediaPlayer;
            var _scaleMode = ScaleMode;
            if (_getDrawingDimensionsMethod == null) _getDrawingDimensionsMethod = typeof(DisplayUGUI).GetMethod("GetDrawingDimensions", BindingFlags.Instance | BindingFlags.NonPublic);

            _flipY_always = false;
            if (HasValidTexture_always())
            {
                _flipY_always = _mediaPlayer.TextureProducer.RequiresVerticalFlip();
            }

            Rect uvRect = _uvRect;
            // Vector4 v = GetDrawingDimensions(_scaleMode, ref uvRect);
            Vector4 v = (Vector4)_getDrawingDimensionsMethod.Invoke(this, new object[] { _scaleMode, uvRect });

#if UNITY_PLATFORM_SUPPORTS_VIDEOTRANSFORM
			Matrix4x4 m = Matrix4x4.identity;
			if (HasValidTexture_always())
			{
				m = Helper.GetMatrixForOrientation(Helper.GetOrientation(_mediaPlayer.Info.GetTextureTransform()));
			}
#endif

            vbo.Clear();

            var vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = new Vector2(v.x, v.y);

            vert.uv0 = new Vector2(uvRect.xMin, uvRect.yMin);
            if (_flipY_always)
            {
                vert.uv0 = new Vector2(uvRect.xMin, 1.0f - uvRect.yMin);
            }
#if UNITY_PLATFORM_SUPPORTS_VIDEOTRANSFORM
			vert.uv0 = m.MultiplyPoint3x4(vert.uv0);
#endif
            vbo.Add(vert);

            vert.position = new Vector2(v.x, v.w);
            vert.uv0 = new Vector2(uvRect.xMin, uvRect.yMax);
            if (_flipY_always)
            {
                vert.uv0 = new Vector2(uvRect.xMin, 1.0f - uvRect.yMax);
            }
#if UNITY_PLATFORM_SUPPORTS_VIDEOTRANSFORM
			vert.uv0 = m.MultiplyPoint3x4(vert.uv0);
#endif
            vbo.Add(vert);

            vert.position = new Vector2(v.z, v.w);
            vert.uv0 = new Vector2(uvRect.xMax, uvRect.yMax);
            if (_flipY_always)
            {
                vert.uv0 = new Vector2(uvRect.xMax, 1.0f - uvRect.yMax);
            }
#if UNITY_PLATFORM_SUPPORTS_VIDEOTRANSFORM
			vert.uv0 = m.MultiplyPoint3x4(vert.uv0);
#endif
            vbo.Add(vert);

            vert.position = new Vector2(v.z, v.y);
            vert.uv0 = new Vector2(uvRect.xMax, uvRect.yMin);
            if (_flipY_always)
            {
                vert.uv0 = new Vector2(uvRect.xMax, 1.0f - uvRect.yMin);
            }
#if UNITY_PLATFORM_SUPPORTS_VIDEOTRANSFORM
			vert.uv0 = m.MultiplyPoint3x4(vert.uv0);
#endif
            vbo.Add(vert);
        }

        [MenuItem("GameObject/UI/AVPro Video uGUI (AlwaysExecute)", false, 0)]
        private static void Create(MenuCommand menuCommand)
        {
            GameObject obj = new GameObject("AVPro Video (AlwaysExecute)");
            obj.AddComponent<AlwaysExecuteDisplayUGUI>();

            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);

            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
            Selection.activeObject = obj;
        }
#endif
    }
}