using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine
{
	public static class ConsoleInput
	{
		private static bool Quit = false;
		private static Thread ConsoleThread;

		// event fires whenever input comes from the console
		public static event EventHandler<String> CommandEntered;

		public static void Start()
		{
			// start new thread that will manage the console
			ConsoleThread = new Thread(delegate() {
				// loop until quit is called
				while (!ConsoleInput.Quit)
				{
					ConsoleInput.AcceptCommand();
				}
			});

			ConsoleThread.Start();
		}

		public static void Stop()
		{
			ConsoleInput.Quit = true;
			ConsoleThread.Join();
		}

		private static void addComponent(string componentName)
		{
			ComponentCollection activeCollection = CollectionManager.GetActiveCollection();

			switch (componentName)
			{
				case "input":
					new Engine.Input(activeCollection, new Coord(100, 100));
					break;
				case "output":
					new Output(activeCollection, new Coord(100, 100));
					break;
				case "clock":
					new Clock(activeCollection, new Coord(100, 100));
					break;
				default:
					if (CollectionManager.GetCollectionNames().Contains(componentName))
					{
						new CustomGate(activeCollection, componentName, new Coord(100, 100));
					}
					else
					{
						Console.WriteLine("invalid gate name.");
					}
					break;
			}
		}

		private static void changeActiveCollection(string newCollection)
		{
			if (CollectionManager.ComponentExists(newCollection))
			{
				UserInterface.SetCurrentState(ProgramState.None);
				CollectionManager.SetActiveCollection(newCollection);
			}
			else
			{
				Console.WriteLine("collection does not exist.");
			}
		}

		private static void listActiveCollections()
		{
			List<String> collectionNames = CollectionManager.GetCollectionNames();
			foreach (String name in collectionNames)
			{
				Console.Write(name);
				Console.Write(", ");
			}
			Console.WriteLine("");
		}

		public static void ProcessCommand(string command)
		{
			string[] commands = command.Split(new char[] { ' ' });

			if (commands[0] == "ls")
			{
				listActiveCollections();
				return;
			}

			// make sure a command was acutally issued
			if (commands.Length >= 2)
			{
				switch (commands[0])
				{
					case "mk":
						CollectionManager.CreateComponentCollection(commands[1]);
						break;
					case "cd":
						changeActiveCollection(commands[1]);
						break;
					case "add":
						addComponent(commands[1]);
						break;
				}
			}
		}

		private static void AcceptCommand()
		{
			Console.Write("--> ");

			// there is no sender
			CommandEntered(null, Console.ReadLine());
		}
	}
}
