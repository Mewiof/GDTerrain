#if TOOLS

using Godot;

namespace GDTerrain {

	[Tool]
	public partial class BrushEditor : Control {

		#region Components
		private Button _shapeButton;
		private TextureRect _shapeButtonTextureRect;

		private Slider _sizeSlider;
		private Label _sizeValueLabel;

		private Control _opacityContainer;
		private Slider _opacitySlider;
		private Label _opacityValueLabel;

		private Control _heightContainer;
		private SpinBox _heightSpinBox;
		private Button _heightSetButton;

		private Control _colorContainer;
		private ColorPickerButton _colorPickerButton;

		private Control _densityContainer;
		private Slider _densitySlider;
		private Label _densityValueLabel;

		private Control _eraseContainer;
		private CheckBox _eraseCheckBox;

		private Control _slopeLimitContainer;
		private IntervalSlider _slopeLimitIntervalSlider;
		private Label _slopeLimitValueLabel;
		#endregion

		private TerrainPainter _terrainPainter;

		#region On Changed
		private void OnSizeSliderValueChanged(double value) {
			int intValue = (int)value;
			if (_terrainPainter != null) {
				_terrainPainter.BrushSize = intValue;
			}
			_sizeValueLabel.Text = intValue.ToString();
		}

		private void OnOpacitySliderValueChanged(double value) {
			float floatValue = (float)value;
			if (_terrainPainter != null) {
				_terrainPainter.Opacity = floatValue;
			}
			_opacityValueLabel.Text = floatValue.ToString("0.00");
		}

		private void OnHeightSpinBoxValueChanged(double value) {
			if (_terrainPainter != null) {
				//_terrainPainter.Height = (float)value;
			}
		}

		private void OnColorPickerButtonColorChanged(Color value) {
			if (_terrainPainter != null) {
				_terrainPainter.color = value;
			}
		}

		private void OnDensitySliderValueChanged(double value) {
			float floatValue = (float)value;
			if (_terrainPainter != null) {
				//_terrainPainter.Density = floatValue;
			}
			_densityValueLabel.Text = floatValue.ToString();//?
		}

		private void OnEraseCheckBoxToggled(bool value) {
			if (_terrainPainter != null) {
				_terrainPainter.maskFlag = !value;
			}
		}

		private void OnSlopeLimitIntervalSliderChanged() {
			float a = _slopeLimitIntervalSlider.ValueA;
			float b = _slopeLimitIntervalSlider.ValueB;
			if (_terrainPainter != null) {
				//_terrainPainter.SetSlopeLimitAngles(Mathf.DegToRad(a), Mathf.DegToRad(b));
			}
			_slopeLimitValueLabel.Text = a.ToString("0.00") + ", " + b.ToString("0.00");
		}
		#endregion

		public override void _Ready() {
			#region Components
			_shapeButton = GetNode<Button>("ShapeButton");
			_shapeButtonTextureRect = GetNode<TextureRect>("ShapeButton/TextureRect");

			static string GetContainerNodePath(string name) {
				return "VBoxContainer/" + name;
			}

			static string GetNodePath(string name, string value) {
				return GetContainerNodePath(name) + '/' + value;
			}

			Control GetContainer(string name) {
				return GetNode<Control>(GetContainerNodePath(name));
			}

			Slider GetSlider(string name) {
				return GetNode<Slider>(GetNodePath(name, "Control/HSlider"));
			}

			Label GetValueLabel(string name) {
				return GetNode<Label>(GetNodePath(name, "Control/ValueLabel"));
			}

			_sizeSlider = GetSlider("Size");
			_sizeValueLabel = GetValueLabel("Size");

			_opacityContainer = GetContainer("Opacity");
			_opacitySlider = GetSlider("Opacity");
			_opacityValueLabel = GetValueLabel("Opacity");

			_heightContainer = GetContainer("Height");
			_heightSpinBox = GetNode<SpinBox>(GetNodePath("Height", "Control/SpinBox"));
			_heightSetButton = GetNode<Button>(GetNodePath("Height", "Control/Button"));

			_colorContainer = GetContainer("Color");
			_colorPickerButton = GetNode<ColorPickerButton>(GetNodePath("Color", "ColorPickerButton"));

			_densityContainer = GetContainer("Density");
			_densitySlider = GetSlider("Density");
			_densityValueLabel = GetValueLabel("Density");

			_eraseContainer = GetContainer("Erase");
			_eraseCheckBox = GetNode<CheckBox>(GetNodePath("Erase", "CheckBox"));

			_slopeLimitContainer = GetContainer("SlopeLimit");
			_slopeLimitIntervalSlider = GetNode<IntervalSlider>(GetNodePath("SlopeLimit", "Control/IntervalSlider"));
			_slopeLimitValueLabel = GetValueLabel("SlopeLimit");
			#endregion

			#region On Changed
			_sizeSlider.ValueChanged += OnSizeSliderValueChanged;
			_opacitySlider.ValueChanged += OnOpacitySliderValueChanged;
			_heightSetButton.Pressed += () => OnHeightSpinBoxValueChanged(_heightSpinBox.Value);
			_colorPickerButton.ColorChanged += OnColorPickerButtonColorChanged;
			_densitySlider.ValueChanged += OnDensitySliderValueChanged;
			_eraseCheckBox.Toggled += OnEraseCheckBoxToggled;
			_slopeLimitIntervalSlider.Changed += OnSlopeLimitIntervalSliderChanged;
			#endregion

			_sizeSlider.MaxValue = Brush.MAX_SIZE_FOR_SLIDERS;
		}

		public void SetupDialogs(Control baseControl) {
			// TODO: dialogs
		}

		public override void _ExitTree() {
			// TODO: dialogs
		}

		private void OnHeightChanged() {
			//_heightSpinBox.Value = _terrainPainter.Height;
			_heightSetButton.ButtonPressed = false;
		}

		private void OnBrushShapesChanged() {
			_shapeButtonTextureRect.Texture = _terrainPainter.Brush.Shapes[0];
		}

		public void SetTerrainPainter(TerrainPainter terrainPainter) {
			// old
			if (_terrainPainter != null) {
				//_terrainPainter.HeightChanged -= OnTerrainPainterHeightChanged;
				_terrainPainter.Brush.ShapesChanged -= OnBrushShapesChanged;
			}

			_terrainPainter = terrainPainter;

			// new
			if (_terrainPainter != null) {
				_sizeSlider.Value = _terrainPainter.Brush.Size;
				_opacitySlider.Ratio = _terrainPainter.Brush.Opacity;
				//_heightSpinBox.Value = _terrainPainter.Height;
				_colorPickerButton.GetPicker().Color = _terrainPainter.color;
				//_densitySlider.Value = _terrainPainter.Density;
				_eraseCheckBox.ButtonPressed = !_terrainPainter.maskFlag;

				//float a = Mathf.RadToDeg(_terrainPainter.SlopeLimitAngleA);
				//float b = Mathf.RadToDeg(_terrainPainter.SlopeLimitAngleB);
				//_slopeLimitIntervalSlider.ValueA = a;
				//_slopeLimitIntervalSlider.ValueB = b;

				SetPaintMode(_terrainPainter.mode);

				Brush brush = _terrainPainter.Brush;
				ImageTexture defaultShape = Brush.LoadShapeFromFile(Brush.DEFAULT_SHAPE_FILE_PATH);
				brush.Shapes = new ImageTexture[] { defaultShape };
				OnBrushShapesChanged();

				//_terrainPainter.HeightChanged += OnTerrainPainterHeightChanged;
				brush.ShapesChanged += OnBrushShapesChanged;
			}
		}

		public void SetPaintMode(TerrainPainter.Mode mode) {
			bool showOpacityContainer = mode != TerrainPainter.Mode.Mask;
			bool showHeightContainer = mode == TerrainPainter.Mode.Flatten;
			bool showColorContainer = mode == TerrainPainter.Mode.Color;
			bool showDensityContainer = mode == TerrainPainter.Mode.Detail;
			bool showEraseContainer = mode == TerrainPainter.Mode.Mask;
			bool showSlopeLimitContainer = mode == TerrainPainter.Mode.Splat || mode == TerrainPainter.Mode.Detail;

			_opacityContainer.Visible = showOpacityContainer;
			_heightContainer.Visible = showHeightContainer;
			_colorContainer.Visible = showColorContainer;
			_densityContainer.Visible = showDensityContainer;
			_eraseContainer.Visible = showEraseContainer;
			_slopeLimitContainer.Visible = showSlopeLimitContainer;

			_heightSetButton.ButtonPressed = false;//?
		}
	}
}
#endif
