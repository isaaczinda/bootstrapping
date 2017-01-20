using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Gtk;
using Gdk;
using System.Threading;

namespace Engine
{
	public class Event
	{
		public enum Type { KeyPress, KeyRelease, ButtonPress, ButtonRelease, MouseMove, Text, ClockToggle };

		public object arguments;
		public Event.Type type;

		public Event(object arguments, Event.Type type)
		{
			
			this.arguments = arguments;
			this.type = type;
		}
	}

	public static class Keyboard
	{
		private static Dictionary<Gdk.Key, bool> keysDown = new Dictionary<Gdk.Key, bool>();

		public static bool IsControlPressed()
		{
			return IsKeyPressed(Gdk.Key.Control_L) || IsKeyPressed(Gdk.Key.Control_R);
		}

		public static bool IsKeyPressed(Gdk.Key key)
		{
			if (Keyboard.keysDown.ContainsKey(key))
			{
				if (keysDown[key])
				{
					return true;
				}
			}
			return false;
		}

		public static void KeyPress(Gtk.KeyPressEventArgs e)
		{
			Keyboard.keysDown[e.Event.Key] = true;
		}

		public static void KeyRelease(Gtk.KeyReleaseEventArgs e)
		{
			Keyboard.keysDown[e.Event.Key] = false;
		}

	}

	public class EventManager
	{
		private uint previousMouseClickTime = 0;
		private ConcurrentQueue<Engine.Event> eventQueue = new ConcurrentQueue<Engine.Event>();
		private System.Timers.Timer gameLoopTimer = new System.Timers.Timer();
		private Gtk.Window window;

		private string eventToString(Event.Type eventType)
		{
			switch (eventType)
			{
				case Event.Type.KeyPress:
					return "key press";
				case Event.Type.KeyRelease:
					return "key release";
				case Event.Type.ButtonPress:
					return "button press";
				case Event.Type.ButtonRelease:
					return "button release";
				case Event.Type.MouseMove:
					return "mouse move";
				case Event.Type.Text:
					return "text entered";
				default:
					return "unidentified event";
			}
		}

		public void ConsumeEvents()
		{
			Event localValue;

			while (eventQueue.TryDequeue(out localValue))
			{
				switch (localValue.type)
				{
					case Event.Type.KeyPress:
						// updates keyboard state
						Keyboard.KeyPress((KeyPressEventArgs)localValue.arguments);
						// pass the event along to the user interface
						UserInterface.KeyPress((KeyPressEventArgs)localValue.arguments);
						break;
					case Event.Type.KeyRelease:
						// updates keyboard state
						Keyboard.KeyRelease((KeyReleaseEventArgs)localValue.arguments);
						break;
					case Event.Type.ButtonPress:
						ButtonPressEventArgs buttonPressArgs = (ButtonPressEventArgs)localValue.arguments;

						// detect if two events were accidentally sent
						if (previousMouseClickTime == buttonPressArgs.Event.Time)
						{
							break;
						}

						previousMouseClickTime = buttonPressArgs.Event.Time;

						UserInterface.MouseButtonPress(buttonPressArgs);
						break;
					case Event.Type.ButtonRelease:
						UserInterface.MouseButtonRelease((ButtonReleaseEventArgs)localValue.arguments);
						break;
					case Event.Type.MouseMove:
						if (((MotionNotifyEventArgs)localValue.arguments).Event.Time == 0)
						{
							break;
						}

						UserInterface.MouseMove((MotionNotifyEventArgs)localValue.arguments);
						break;
					case Event.Type.Text:
						ConsoleInput.ProcessCommand((String)localValue.arguments);
						break;
					case Event.Type.ClockToggle:
						// toggle the value coming out of the clock input, not whether the clock is on or off
						Input toToggle = (Input)localValue.arguments;
						toToggle.Toggle();
						break;
				}
			}
		}

		public void Add(Engine.Event eventToAdd)
		{
			eventQueue.Enqueue(eventToAdd);
		}

		public EventManager(DrawingArea da, Gtk.Window window)
		{
			this.window = window;

			// allow events to be fired
			window.Events |= EventMask.KeyPressMask | EventMask.KeyReleaseMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
			da.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.KeyPressMask | EventMask.KeyReleaseMask;

			// whenever a related event is fired, add it to the event queue
			window.KeyPressEvent += delegate (object sender, KeyPressEventArgs e)
			{
				Event temp = new Engine.Event(e, Event.Type.KeyPress);
				eventQueue.Enqueue(temp);
			};
			window.KeyReleaseEvent += delegate (object sender, KeyReleaseEventArgs e)
			{
				Event temp = new Engine.Event(e, Event.Type.KeyRelease);
				eventQueue.Enqueue(temp);
			};
			da.ButtonPressEvent += delegate (object sender, ButtonPressEventArgs e)
			{
				Event temp = new Engine.Event(e, Event.Type.ButtonPress);
				eventQueue.Enqueue(temp);
			};
			da.ButtonReleaseEvent += delegate (object sender, ButtonReleaseEventArgs e)
			{
				Event temp = new Engine.Event(e, Event.Type.ButtonRelease);
				eventQueue.Enqueue(temp);
			};
			da.MotionNotifyEvent += delegate (object sender, MotionNotifyEventArgs e)
			{
				Event temp = new Engine.Event(e, Event.Type.MouseMove);

				eventQueue.Enqueue(temp);
			};
			ConsoleInput.CommandEntered += delegate (object sender, String command)
			{
				Event temp = new Event(command, Event.Type.Text);
				eventQueue.Enqueue(temp);
			};


			// start taking console input
			ConsoleInput.Start();

			// queue the first draw. we will keep doing this forever
			da.QueueDraw();

			// anytime the window needs to be refreshed, draw to it
			da.ExposeEvent += delegate (object o, ExposeEventArgs args)
			{
				// consume all events
				this.ConsumeEvents();

				Cairo.Context context = Gdk.CairoHelper.Create(da.GdkWindow);
				Drawing.Update(context);

				// let some other threads do work too
				Thread.Sleep(50);

				da.QueueDraw();
			};
		}
	}
}