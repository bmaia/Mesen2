using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using Mesen.Config.Shortcuts;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using System.ComponentModel;
using Mesen.Interop;
using Avalonia.Threading;
using Avalonia;
using Mesen.Config;
using Mesen.Utilities.GlobalMouseLib;
using Mesen.Utilities;
using Mesen.Localization;

namespace Mesen.Windows
{
	public class GetKeyWindow : Window
	{
		private DispatcherTimer _timer; 
		
		private List<UInt16> _prevScanCodes = new List<UInt16>();
		private TextBlock lblCurrentKey;
		private bool _allowMouseButtons;

		public string HintLabel { get; }
		public bool SingleKeyMode { get; set; } = false;
		
		public DbgShortKeys DbgShortcutKey { get; set; } = new DbgShortKeys();
		public KeyCombination ShortcutKey { get; set; } = new KeyCombination();

		[Obsolete("For designer only")]
		public GetKeyWindow() : this(true) { }

		public GetKeyWindow(bool allowMouseButtons)
		{
			_allowMouseButtons = allowMouseButtons;
			HintLabel = ResourceHelper.GetMessage(_allowMouseButtons ? "SetKeyMouseHint" : "SetKeyHint");

			InitializeComponent();

			lblCurrentKey = this.GetControl<TextBlock>("lblCurrentKey");
			
			_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(25), DispatcherPriority.Normal, (s, e) => UpdateKeyDisplay());
			_timer.Start();

			//Allow us to catch LeftAlt/RightAlt key presses
			this.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, true);
			this.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUp, RoutingStrategies.Tunnel, true);
		}

		protected override void OnClosed(EventArgs e)
		{
			_timer?.Stop();
			base.OnClosed(e);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
		{
			InputApi.SetKeyState((UInt16)e.Key, true);
			DbgShortcutKey = new DbgShortKeys(e.KeyModifiers, e.Key);
			e.Handled = true;
		}

		private void OnPreviewKeyUp(object? sender, KeyEventArgs e)
		{
			InputApi.SetKeyState((UInt16)e.Key, false);
			e.Handled = true;
		}

		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);

			lblCurrentKey.IsVisible = !this.SingleKeyMode;
			lblCurrentKey.Height = this.SingleKeyMode ? 0 : 40;

			ShortcutKey = new KeyCombination();
			InputApi.UpdateInputDevices();
			InputApi.ResetKeyState();

			//Prevent other keybindings from interfering/activating
			InputApi.DisableAllKeys(true);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			InputApi.DisableAllKeys(false);
			DataContext = null;
		}

		private void SelectKeyCombination(KeyCombination key)
		{
			if(!string.IsNullOrWhiteSpace(key.ToString())) {
				ShortcutKey = key;
				this.Close();
			}
		}

		private void UpdateKeyDisplay()
		{
			if(_allowMouseButtons) {
				MousePosition p = GlobalMouse.GetMousePosition();
				PixelPoint mousePos = new PixelPoint(p.X, p.Y);
				PixelRect clientBounds = new PixelRect(this.PointToScreen(new Point(0, 0)), PixelSize.FromSize(Bounds.Size, 1.0));
				bool mouseInsideWindow = clientBounds.Contains(mousePos);
				InputApi.SetKeyState(MouseManager.LeftMouseButtonKeyCode, mouseInsideWindow && GlobalMouse.IsMouseButtonPressed(MouseButtons.Left));
				InputApi.SetKeyState(MouseManager.RightMouseButtonKeyCode, mouseInsideWindow && GlobalMouse.IsMouseButtonPressed(MouseButtons.Right));
				InputApi.SetKeyState(MouseManager.MiddleMouseButtonKeyCode, mouseInsideWindow && GlobalMouse.IsMouseButtonPressed(MouseButtons.Middle));
				InputApi.SetKeyState(MouseManager.MouseButton4KeyCode, mouseInsideWindow && GlobalMouse.IsMouseButtonPressed(MouseButtons.Button4));
				InputApi.SetKeyState(MouseManager.MouseButton5KeyCode, mouseInsideWindow && GlobalMouse.IsMouseButtonPressed(MouseButtons.Button5));
			}

			List<UInt16> scanCodes = InputApi.GetPressedKeys();

			if(this.SingleKeyMode) {
				if(scanCodes.Count >= 1) {
					//Always use the largest scancode (when multiple buttons are pressed at once)
					scanCodes = new List<UInt16> { scanCodes.OrderBy(code => -code).First() };
					this.SelectKeyCombination(new KeyCombination(scanCodes));
				}
			} else {
				KeyCombination key = new KeyCombination(_prevScanCodes);
				this.GetControl<TextBlock>("lblCurrentKey").Text = key.ToString();

				if(scanCodes.Count < _prevScanCodes.Count) {
					//Confirm key selection when the user releases a key
					this.SelectKeyCombination(key);
				}

				_prevScanCodes = scanCodes;
			}
		}
	}
}
