using System;
using System.Collections.Generic;

namespace Engine
{
	public static class BlueprintLibrary
	{
		private static Dictionary<String, Blueprint> CollectionList = new Dictionary<String, Blueprint>();
		private static Blueprint ActiveCollection = null;
		public static EventManager Events = null;

		public static void SetEventManager(EventManager eventManager)
		{
			BlueprintLibrary.Events = eventManager;
		}

		public static Blueprint NewBlueprint(String Name)
		{
			// throw an exception if the name is taken
			if (!Storage.IsNameFree(Name))
			{
				throw new Exception("collection name is already taken.");
			}

			// create a new collection, add it to the dictionary
			Blueprint newCollection = new Blueprint(Name);
			CollectionList.Add(Name, newCollection);

			return newCollection;
		}

		public static bool ComponentExists(String Name)
		{
			return CollectionList.ContainsKey(Name);
		}

		public static Blueprint Lookup(String Name)
		{
			if (!CollectionList.ContainsKey(Name))
			{
				throw new Exception("The component that you are trying to use does not exist");
			}

			return CollectionList[Name];
		}

		public static void SetActiveCollection(string ComponentName)
		{
			BlueprintLibrary.ActiveCollection = BlueprintLibrary.Lookup(ComponentName);
		}

		public static List<string> GetCollectionNames()
		{
			return new List<String>(BlueprintLibrary.CollectionList.Keys);
		}

		public static List<Blueprint> GetCollections()
		{
			return new List<Blueprint>(BlueprintLibrary.CollectionList.Values);
		}

		public static Blueprint GetActiveCollection()
		{
			if (ActiveCollection == null)
			{
				throw new Exception("An active collection has not yet been set");
			}

			return BlueprintLibrary.ActiveCollection;
		}
	}

	public class Edge
	{
		public ComponentReference Source;
		public ComponentReference Destination;

		public Edge(ComponentReference Source, ComponentReference Destination)
		{
			this.Source = Source;
			this.Destination = Destination;
		}	
	}
}
