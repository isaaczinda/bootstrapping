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
	class EventHandlers
	{
		public static EventHandlers current;

		// application entry point
		public static void Main(string[] args)
		{
			Gtk.Application.Init();
			MainWindow w = new MainWindow();
			w.Resize(640, 480);
			w.Show();
			Gtk.Application.Run();
		}

		private void createNAndGate()
		{
			// create nand default gate gates
			ComponentCollection nand = CollectionManager.CreateComponentCollection("nand");
			Engine.Input inputOne = new Engine.Input(nand, new Coord(0, 0));
			Engine.Input inputTwo = new Engine.Input(nand, new Coord(0, 0));
			And andGate = new And(nand, new Coord(0, 0));
			andGate.setInputs(new ComponentReference[] { new ComponentReference(inputOne, 0), new ComponentReference(inputTwo, 0) });

			Not notGate = new Not(nand, new Coord(0, 0));
			notGate.setInputs(new ComponentReference[] { new ComponentReference(andGate, 0) });

			Output output = new Output(nand, new Coord(0, 0));
			output.setInputs(new ComponentReference[] { new ComponentReference(notGate, 0) });
		}

		public EventHandlers()
		{
			// create the base gate
			this.createNAndGate();

			// load the project from memory
			Storage.LoadProject();

			// this is the collection that we will always see first
			CollectionManager.SetActiveCollection("main");

			// set a static reference to the program
			EventHandlers.current = this;
		}
	}
}