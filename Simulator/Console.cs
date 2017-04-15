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

		private static void addComponent(string componentName, int number)
		{
			Blueprint activeCollection = BlueprintLibrary.GetActiveCollection();

			for (int i = 0; i < number; i++)
			{
				Coord coordinate = new Coord(100 + i * 10, 100) - activeCollection.GetPosition();

				switch (componentName)
				{
					case "input":
						new Engine.Input(activeCollection, coordinate);
						break;
					case "output":
						new Output(activeCollection, coordinate);
						break;
					case "clock":
						new Clock(activeCollection, coordinate);
						break;
					default:
						List<string> test = BlueprintLibrary.GetCollectionNames();

						if (BlueprintLibrary.GetCollectionNames().Contains(componentName))
						{
							new CustomGate(activeCollection, componentName, coordinate);
						}
						else
						{
							Console.WriteLine("invalid gate name.");
						}
						break;
				}
			}
		}

		private static void changeActiveCollection(string newCollection)
		{
			
			if (BlueprintLibrary.ComponentExists(newCollection))
			{
				// change the active collection
				UserInterface.SetCurrentState(ProgramState.None);
				BlueprintLibrary.SetActiveCollection(newCollection);

				// resolve the new active collection
				BlueprintLibrary.GetActiveCollection().ResolveOutputs();
			}
			else
			{
				Console.WriteLine("collection does not exist.");
			}


		}

		private static void listActiveCollections()
		{
			List<String> collectionNames = BlueprintLibrary.GetCollectionNames();
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

			}
			// make sure a command was acutally issued
			else if (commands.Length >= 2)
			{
				switch (commands[0])
				{
					case "mk":
						BlueprintLibrary.NewBlueprint(commands[1]);
						break;
					case "cd":
						changeActiveCollection(commands[1]);
						break;
					case "add":
						int numberToAdd = 1;
						if (commands.Length == 3)
						{
							try
							{
								numberToAdd = Convert.ToInt16(commands[2]);
							}
							catch
							{
								Console.WriteLine("invalid number of components");
								break;
							}
						}

						addComponent(commands[1], numberToAdd);
						break;
				}
			}

			// write the repel symbol again
			Console.Write("--> ");
		}

		private static void AcceptCommand()
		{
			// there is no sender
			CommandEntered(null, Console.ReadLine());
		}
	}
}
