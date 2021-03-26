﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Mesen.GUI.Debugger;
using Mesen.GUI.Utilities;

namespace Mesen.GUI.Config
{
	public class DebugConfig
	{
		//public DebuggerShortcutsConfig Shortcuts = new DebuggerShortcutsConfig();
		public TraceLoggerInfo TraceLogger = new TraceLoggerInfo();
		public HexEditorConfig HexEditor = new HexEditorConfig();
		public EventViewerConfig EventViewer = new EventViewerConfig();
		public DebuggerConfig Debugger = new DebuggerConfig();
		public TilemapViewerConfig TilemapViewer = new TilemapViewerConfig();
		public TileViewerConfig TileViewer = new TileViewerConfig();
		public RegisterViewerConfig RegisterViewer = new RegisterViewerConfig();
		public SpriteViewerConfig SpriteViewer = new SpriteViewerConfig();
		public DbgIntegrationConfig DbgIntegration = new DbgIntegrationConfig();
		public ScriptWindowConfig ScriptWindow = new ScriptWindowConfig();
		public ProfilerConfig Profiler = new ProfilerConfig();
		public AssemblerConfig Assembler = new AssemblerConfig();
		public DebugLogConfig DebugLog = new DebugLogConfig();

		public DebugConfig()
		{		
		}
	}

	public enum RefreshSpeed
	{
		Low = 0,
		Normal = 1,
		High = 2
	}

}