using Cairo;
using System;
using Gtk;
using Gdk;
using System.Windows.Input;
using System.Collections.Generic;
using System.Timers;
using Engine;
using System.Threading;

namespace Engine
{
	class Program
	{
		// application entry point
		public static void Main(string[] args)
		{
			Gtk.Application.Init();
			Window w = new Window();
			w.Resize(640, 480);
			w.Show();

			Gtk.Application.Run();
		}
	}
}