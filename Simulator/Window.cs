using System;
using Gtk;
using Engine;
using System.Collections.Generic;

public class Window : Gtk.Window
{
	private EventManager eventManager;

	public HPaned container;
	private DrawingArea da;
		
	public virtual void Build()
	{

		global::Stetic.Gui.Initialize(this);
		// Widget MainWindow
		this.Name = "MainWindow";
		this.Title = global::Mono.Unix.Catalog.GetString("test");
		this.WindowPosition = ((global::Gtk.WindowPosition)(4));
		this.Resizable = true;
		// Container child MainWindow.Gtk.Container+ContainerChild
		// Container child vpaned1.Gtk.Paned+PanedChild
		this.da = new global::Gtk.DrawingArea();
		this.da.Name = "drawingarea1";
		this.Add(this.da);
		this.da.Show();
	}

	public Window() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		// create the initial gate that will be used for bootstrapping
		this.createNAndGate();

		// load the project from memory
		Storage.LoadProject();

		// this is the collection that we will always see first
		CollectionManager.SetActiveCollection("main");

		// sets up entire event framework
		eventManager = new EventManager(da, this);
		CollectionManager.SetEventManager(eventManager);

		// tell the program that the circut has changed
		UserInterface.CircutChanged();
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	private void createNAndGate()
	{
		// create nand default gate gates
		ComponentCollection nand = CollectionManager.CreateComponentCollection("nand");
		Engine.Input inputOne = new Engine.Input(nand, new Coord(0, 0));
		Engine.Input inputTwo = new Engine.Input(nand, new Coord(0, 0));
		And andGate = new And(nand, new Coord(0, 0));
		andGate.setInputs(new ComponentReference[] { new ComponentReference(inputOne, 0), new ComponentReference(inputTwo, 0) });

		BufferCollection buffers = nand.Buffers;
		Engine.Buffer bufferOne = buffers.New(new Coord(100, 100));
		bufferOne.AddReference(new ComponentReference(andGate, 0));
		Engine.Buffer bufferTwo = buffers.New(new Coord(150, 150));
		bufferTwo.AddReference(new ComponentReference(bufferOne));


		Not notGate = new Not(nand, new Coord(0, 0));
		notGate.setInputs(new ComponentReference[] { new ComponentReference(bufferTwo)});

		Output output = new Output(nand, new Coord(0, 0));
		output.setInputs(new ComponentReference[] { new ComponentReference(notGate, 0) });
	}
}