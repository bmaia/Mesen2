#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using Mesen.Debugger.Controls;
using Mesen.Debugger.ViewModels;
using Avalonia.Platform;
using Mesen.Interop;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mesen.Debugger.Windows
{
	public class TilemapViewerWindow : Window
	{
		private NotificationListener _listener;
		private TilemapViewerViewModel _model;
		private PictureViewer _picViewer;
		
		[Obsolete("For designer only")]
		public TilemapViewerWindow() : this(CpuType.Cpu, ConsoleType.Snes) { }

		public TilemapViewerWindow(CpuType cpuType, ConsoleType consoleType)
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif

			_picViewer = this.FindControl<PictureViewer>("picViewer");
			_model = new TilemapViewerViewModel(cpuType, consoleType, _picViewer, this);
			DataContext = _model;
			_model.Config.LoadWindowSettings(this);
			_listener = new NotificationListener();

			if(Design.IsDesignMode) {
				return;
			}

			_picViewer.PointerMoved += PicViewer_PointerMoved;
			_picViewer.PointerLeave += PicViewer_PointerLeave;
			_picViewer.Source = _model.ViewerBitmap;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnOpened(EventArgs e)
		{
			if(Design.IsDesignMode) {
				return;
			}

			_listener.OnNotification += listener_OnNotification;
			_model.RefreshData();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_listener?.Dispose();
			_model.Config.SaveWindowSettings(this);
		}

		private void PicViewer_PointerMoved(object? sender, PointerEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				PixelPoint p = PixelPoint.FromPoint(e.GetCurrentPoint(viewer).Position / viewer.Zoom, 1);
				DynamicTooltip? tooltip = _model.GetPreviewPanel(p);

				if(tooltip != null) {
					ToolTip.SetTip(viewer, tooltip);

					//Force tooltip to update its position
					ToolTip.SetHorizontalOffset(viewer, 4);
					ToolTip.SetHorizontalOffset(viewer, 5);
					ToolTip.SetIsOpen(viewer, true);
				} else {
					ToolTip.SetTip(viewer, null);
					ToolTip.SetIsOpen(viewer, false);
				}
			}
		}

		private void PicViewer_PointerLeave(object? sender, PointerEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				ToolTip.SetTip(viewer, null);
				ToolTip.SetIsOpen(viewer, false);
			}
		}

		private void OnSettingsClick(object sender, RoutedEventArgs e)
		{
			_model.Config.ShowSettingsPanel = !_model.Config.ShowSettingsPanel;
		}

		private void listener_OnNotification(NotificationEventArgs e)
		{
			switch(e.NotificationType) {
				case ConsoleNotificationType.EventViewerRefresh:
					if(_model.Config.AutoRefresh) {
						_model.RefreshData();
					}
					break;

				case ConsoleNotificationType.CodeBreak:
					if(_model.Config.RefreshOnBreakPause) {
						_model.RefreshData();
					}
					break;
			}
		}
	}
}
