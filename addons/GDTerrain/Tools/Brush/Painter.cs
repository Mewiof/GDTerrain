#if TOOLS

/* Core logic for painting textures using shaders, with undo/redo support.
 * The operations are delayed, so results are only available in next frame.
 * This does not implement user interface or brush behaviour, only rendering logic
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class Painter : Node3D {

		public const int UNDO_CHUNK_SIZE = 64;

		private static readonly Shader _noBlendShader = Plugin.LoadShader("Tools/Brush/Shaders/no_blend.gdshader");

		// Common parameters
		public const string SHADER_PARAM_SRC_TEXTURE = "u_src_texture";
		public const string SHADER_PARAM_SRC_RECT = "u_src_rect";
		public const string SHADER_PARAM_OPACITY = "u_opacity";

		public Action<Rect2i> textureRegionChanged;

		private SubViewport _subViewport;
		private Sprite2D _viewportBackgroundSprite;
		private Sprite2D _viewportBrushSprite;
		/// <summary>
		/// [!] Setting will cause the internal viewport to be resized, which is expensive
		/// <para>If you need to change the brush size frequently while painting, you may prefer to use scale</para>
		/// </summary>
		public int brushSize = 32;
		/// <summary>
		/// The difference between size and scale is that size is specified in pixels, while scale is a multiplier
		/// <para>[!] The scale is also much cheaper to change</para>
		/// </summary>
		#region Brush Scale
		private float _brushScale = 1f;
		public float BrushScale {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _brushScale;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _brushScale = Mathf.Clamp(value, 0f, 1f);
		}
		#endregion
		private Vector2i _brushPosition;
		#region Brush Opacity
		private float _brushOpacity;
		public float BrushOpacity {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _brushOpacity;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _brushOpacity = Mathf.Clamp(value, 0f, 1f);
		}
		#endregion
		private Texture _brushTexture;//?
		private Vector2i _lastBrushPosition;
		private ShaderMaterial _brushMaterial = new();
		private Image _image;
		private ImageTexture _texture;
		private bool _commandPaint;
		private bool _pendingPaintRender;
		#region Brush Rotation
		public float BrushRotation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _viewportBrushSprite.Rotation;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _viewportBrushSprite.Rotation = value;
		}
		#endregion
		private readonly List<string> _modifiedShaderParams = new();

		public Painter() : base() {
			// sub viewport
			_subViewport = new() {
				Size = new(brushSize, brushSize),
				RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
				RenderTargetClearMode = SubViewport.ClearMode.Once,
				TransparentBg = true
			};

			// background
			ShaderMaterial noBlendMaterial = new() {
				Shader = _noBlendShader
			};
			_viewportBackgroundSprite = new() {
				Centered = false,
				Material = noBlendMaterial
			};
			_subViewport.AddChild(_viewportBackgroundSprite);

			// brush
			_viewportBrushSprite = new() {
				Centered = true,
				Material = _brushMaterial,
				Position = _subViewport.Size / 2
			};
			_subViewport.AddChild(_viewportBrushSprite);

			AddChild(_subViewport);
		}

		#region Modified Chunks
		private readonly List<Vector2i> _modifiedChunks = new();

		/// <summary>Do not commit while this equals true</summary>
		public bool PendingOperation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _pendingPaintRender || _commandPaint;
		}

		public bool HasModifiedChunks {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _modifiedChunks.Count > 0;
		}

		/// <summary>Set modified</summary>
		private void MarkModifiedChunks(int bX, int bY, int bW, int bH) {

			int cMinX = bX / UNDO_CHUNK_SIZE;
			int cMinY = bY / UNDO_CHUNK_SIZE;
			int cMaxX = (bX + bW - 1) / UNDO_CHUNK_SIZE + 1;
			int cMaxY = (bY + bH - 1) / UNDO_CHUNK_SIZE + 1;

			for (int cY = cMinY; cY < cMaxY; cY++) {
				for (int cX = cMinX; cX < cMaxX; cX++) {
					_modifiedChunks.Add(new(cX, cY));
				}
			}
		}

		/// <summary>[!] Use 'Commit' instead</summary>
		private (List<Vector2i> cPositions, List<Image> cInitData, List<Image> cFinalData) CommitModifiedChunks() {
			// TODO: cache?
			List<Vector2i> cPositions = new();
			List<Image> cInitData = new();
			List<Image> cFinalData = new();

			Image finalImage = _texture.GetImage();
			foreach (Vector2i cPos in _modifiedChunks) {
				int cX = cPos.x;
				int cY = cPos.y;

				int x = cX * UNDO_CHUNK_SIZE;
				int y = cY * UNDO_CHUNK_SIZE;
				int w = Math.Min(UNDO_CHUNK_SIZE, _image.GetWidth() - x);
				int h = Math.Min(UNDO_CHUNK_SIZE, _image.GetHeight() - y);

				Rect2i rect = new(x, y, w, h);
				Image initData = _image.GetRect(rect);
				Image finalData = finalImage.GetRect(rect);

				cPositions.Add(cPos);
				cInitData.Add(initData);
				cFinalData.Add(finalData);

				_image.BlitRect(finalImage, rect, rect.Position);
			}
			_modifiedChunks.Clear();

			return (cPositions, cInitData, cFinalData);
		}

		/// <summary>Applies changes to the image and returns modified chunks for undo/redo</summary>
		public (List<Vector2i> cPositions, List<Image> cInitData, List<Image> cFinalData) Commit() {
			if (PendingOperation) {
				throw new Exception($"'{nameof(PendingOperation)}'");
			}
			return CommitModifiedChunks();
		}
		#endregion

		public override void _Process(double delta) {
			if (_pendingPaintRender) {
				_pendingPaintRender = false;

				Image data = _subViewport.GetTexture().GetImage();
				data.Convert(_image.GetFormat());

				Vector2i brushPos = _lastBrushPosition;

				int textureWidth = _texture.GetWidth();
				int textureHeight = _texture.GetHeight();

				int destX = Util.Clamp(brushPos.x, 0, textureWidth);
				int destY = Util.Clamp(brushPos.y, 0, textureHeight);

				int sourceX = Math.Max(-brushPos.x, 0);
				int sourceY = Math.Max(-brushPos.y, 0);
				int sourceW = Math.Min(Math.Max(_subViewport.Size.x - sourceX, 0), textureWidth - destX);
				int sourceH = Math.Min(Math.Max(_subViewport.Size.y - sourceY, 0), textureHeight - destY);

				if (sourceW != 0 && sourceH != 0) {
					MarkModifiedChunks(destX, destY, sourceW, sourceH);
					// TODO: partial texture update has not yet been implemented, so for now we are updating the full texture
					RenderingServer.Texture2dUpdate(_texture.GetRid(), data, 0);
					textureRegionChanged.Invoke(new(destX, destY, sourceW, sourceH));
				}
			}

			if (_commandPaint) {
				_commandPaint = false;

				_pendingPaintRender = true;
				_lastBrushPosition = _brushPosition;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector2i GetSizeFitForRotation(Vector2i sourceSize) {
			int d = (int)MathF.Ceiling(sourceSize.Length());
			return new(d, d);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector2i Multiply(Vector2i source, float value) {
			return new((int)(source.x * value), (int)(source.y * value));
		}

		// TODO: Vector2i?
		public void PaintInput(Vector2i centerPos) {
			Vector2i viewportSize = GetSizeFitForRotation(new(brushSize, brushSize));
			if (_subViewport.Size != viewportSize) {
				// do this lazily so that the brush slider doesn't lag when adjusting
				_subViewport.Size = viewportSize;
				_viewportBrushSprite.Position = _subViewport.Size / 2;
			}

			// it is necessary to floor position in case brush has an odd size
			Vector2i brushPos = centerPos - Multiply(_subViewport.Size, .5f);
			_subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;//?
			_subViewport.RenderTargetClearMode = SubViewport.ClearMode.Once;//?
			_viewportBackgroundSprite.Position = -brushPos;
			_brushPosition = brushPos;
			_commandPaint = true;

			// we want this quad to have a certain size, regardless of the texture assigned to it
			_viewportBrushSprite.Scale = _brushScale * new Vector2(brushSize, brushSize) / _viewportBrushSprite.Texture.GetSize();

			int textureWidht = _texture.GetWidth();
			int textureHeight = _texture.GetHeight();

			// use Color, because Godot does not understand vec4
			Color rect = new(
				brushPos.x / textureWidht,
				brushPos.y / textureHeight,
				_subViewport.Size.x / textureWidht,
				_subViewport.Size.y / textureHeight
				);
			_brushMaterial.SetShaderParameter(SHADER_PARAM_SRC_RECT, rect);
			_brushMaterial.SetShaderParameter(SHADER_PARAM_OPACITY, _brushOpacity);
		}

		#region Debug Display
		private TextureRect _debugDisplay;

		public void SetDebugDisplay(TextureRect value) {//?
			_debugDisplay = value;
			_debugDisplay.Texture = _subViewport.GetTexture();
		}
		#endregion

		public void SetImage(Image value, ImageTexture texture) {
			_image = value;
			_texture = texture;
			_viewportBackgroundSprite.Texture = _texture;
			_brushMaterial.SetShaderParameter(SHADER_PARAM_SRC_TEXTURE, _texture);
		}

		public void SetBrushTexture(Texture2D value) {
			_viewportBrushSprite.Texture = value;
		}

		public void SetBrushShader(Shader value) {
			if (_brushMaterial.Shader != value) {
				_brushMaterial.Shader = value;
			}
		}

		public void SetBrushShaderParam(string param, Variant value) {
			_modifiedShaderParams.Add(param);
			_brushMaterial.SetShaderParameter(param, value);
		}

		public void ClearModifiedBrushShaderParams() {
			foreach (string param in _modifiedShaderParams) {
				_brushMaterial.SetShaderParameter(param, default);
			}
			_modifiedShaderParams.Clear();
		}
	}
}
#endif
