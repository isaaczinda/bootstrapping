using System;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;

namespace Engine
{
    public static class Storage
    {
        private const string WORKING_FOLDER = "CustomComponents";
        private const string EXTENSION = ".gate";
		private const string DEPENDENCY_TREE_LOCATION = "tree.map";

        public static bool IsNameFree(String gateName) {
			// if the name is reserved, it cannot be free
			if (Component.IsBaseComponent(gateName))
			{
				return false;
			}

			List<Blueprint> test = BlueprintLibrary.GetCollections();
			List<String> test2 = new List<String>();

			foreach (Blueprint print in test)
			{
				test2.Add(print.GetName());
			}


			// if any existing collection has the gate name
			if (BlueprintLibrary.GetCollections().Select((arg) => arg.GetName()).Contains(gateName))
			{
				return false;
			}

            return true;
        }

		private static void createStorageDirectory()
		{
			// create working folder if it doesn't exist
			if (!Directory.Exists(Storage.WORKING_FOLDER))
			{
				Directory.CreateDirectory(Storage.WORKING_FOLDER);
			}
		}

        private static string FilepathFromName(string name) {
            return Path.Combine(Storage.WORKING_FOLDER, name + Storage.EXTENSION);
        }

		public static void SaveProject()
		{
			foreach (Blueprint collection in BlueprintLibrary.GetCollections())
			{
				if (collection.GetName() != "nand")
				{
					Storage.SaveComponent(collection);
				}
			}

			// make sure we know what order to load the collections in
			SaveDependencyTree();
		}

		private static void SaveComponent (Blueprint Collection) {
			createStorageDirectory();

            // get where the file should be stored given its name
            string filePath = Storage.FilepathFromName(Collection.GetName());

			String serializedJson = JsonConvert.SerializeObject(Collection);
            
            // creates a new file, then writes data there
            File.WriteAllText(filePath, serializedJson);
        }

		private static Coord ObjectToCoord(JToken jToken)
		{
			return new Coord((double)jToken["x"], (double)jToken["y"]);
		}

		private static ComponentState[] ObjectToOutputs(JToken jToken)
		{
			return jToken.Select(input =>
         		(ComponentState)(int)input).ToArray();
		}

		private static ComponentReference[] ObjectToReferences(JToken jToken)
		{
			List<ComponentReference> references = new List<ComponentReference>();
			
			foreach (JToken token in jToken)
			{
				if (token.Children().Count() == 0)
				{
					references.Add(null);
				}
				else
				{
					bool IsBufferReference = (bool)token["BufferReference"];

					if (IsBufferReference)
					{
						references.Add(new ComponentReference((int)token["Id"], (string)token["CollectionName"]));
					}
					else
					{
						references.Add(new ComponentReference((int)token["Id"], (int)token["Index"]));
					}
				}
			}

			return references.ToArray();
		}

		private static Dictionary<string, List<string>> LoadDependencyTree()
		{
			string filePath = Path.Combine(Storage.WORKING_FOLDER, Storage.DEPENDENCY_TREE_LOCATION);
			string jsonString = File.ReadAllText(filePath);

			JObject jObject = JObject.Parse(jsonString);

			Dictionary<string, List<string>> GlobalDependencies = new Dictionary<string, List<string>>();

			List<string> keys = jObject.Properties().Select(p => p.Name).ToList();
			List<List<string>> values = jObject.Properties().Select(p => (jObject[p.Name].Select(q => (string)q)).ToList()).ToList();

			for (int i = 0; i < keys.Count; i++)
			{
				GlobalDependencies.Add(keys[i], values[i]);
			}

			return GlobalDependencies;
		}

		public static void LoadProject()
		{
			Dictionary<string, List<string>> GlobalDependencies = Storage.LoadDependencyTree();

			// remove the nand dependency from each collection, because we have just created that gate
			foreach (List<string> item in GlobalDependencies.Values)
			{
				item.Remove("nand");
			}

			// continue until all elements have been loaded into memory
			while (GlobalDependencies.Keys.Count > 0)
			{
				foreach (string key in GlobalDependencies.Keys)
				{
					// if a collection has no references left to meet
					if (GlobalDependencies[key].Count == 0)
					{
						Console.WriteLine(key);

						// load the collection
						Storage.LoadComponent(key);

						// remove this collection from the dictionary
						GlobalDependencies.Remove(key);

						// remove references to it
						foreach (string toShortenKey in GlobalDependencies.Keys)
						{
							GlobalDependencies[toShortenKey].Remove(key);
						}

						break;
					}
				}
			}
		}

		private static void SaveDependencyTree()
		{
			Dictionary<string, List<string>> GlobalDependencies = new Dictionary<string, List<string>>();

			// loop through each collection
			foreach (Blueprint collection in BlueprintLibrary.GetCollections())
			{
				// do not save the nand collection
				if (collection.GetName() != "nand")
				{
					GlobalDependencies.Add(collection.GetName(), collection.getDependencies());
				}
			}

			// serialize the dictionary of dependencies
			String serializedJson = JsonConvert.SerializeObject(GlobalDependencies);

			// save the serialized string
			string filepath = Path.Combine(Storage.WORKING_FOLDER, Storage.DEPENDENCY_TREE_LOCATION);
			File.WriteAllText(filepath, serializedJson);
		}

		public static void LoadComponent(string name)
		{
			string filePath = Storage.FilepathFromName(name);
			string jsonString = File.ReadAllText(filePath);
			JObject jObject = JObject.Parse(jsonString);

			// get the name
			String collectionName = (String)jObject["Name"];
			Coord collectionPosition = ObjectToCoord(jObject["Position"]);

			// create a component collection that we will save to
			Blueprint collection = BlueprintLibrary.NewBlueprint(collectionName);
			collection.setPosition(collectionPosition);

			// extract items and converts to List<ComponentCollection>
			var items = from item in jObject["Items"] select (JObject)item;

			for (int i = 0; i < items.Count(); i++)
			{
				Coord position = ObjectToCoord(items.ElementAt(i)["Position"]);
				Coord dimensions = ObjectToCoord(items.ElementAt(i)["Dimensions"]);
				int id = (int)items.ElementAt(i)["Id"];
				string componentName = (string)items.ElementAt(i)["Name"];
				ComponentType type = (ComponentType)(int)items.ElementAt(i)["Type"];
				ComponentState[] outputStates = ObjectToOutputs(items.ElementAt(i)["OutputStates"]);
				ComponentReference[] inputStates = ObjectToReferences(items.ElementAt(i)["InputStates"]);

				switch (type)
				{
					case ComponentType.Input:
						if (componentName == "clock")
						{
							new Clock(collection, position, dimensions, id, componentName, outputStates, inputStates);
							break;
						}
						new Input(collection, position, dimensions, id, componentName, outputStates, inputStates);
						break;
					case ComponentType.Output:
						new Output(collection, position, dimensions, id, componentName, outputStates, inputStates);
						break;
					case ComponentType.Gate:
						new CustomGate(collection, position, dimensions, id, componentName, outputStates, inputStates);
						break;
				}
			}

			// extract items and converts to List<ComponentCollection>
			var buffers = from item in jObject["Buffers"]["Items"] select (JObject)item;

			foreach (var buffer in buffers)
			{
				int id = (int)buffer["Id"];
				Coord position = ObjectToCoord(buffer["Position"]);
				List<ComponentReference> references = ObjectToReferences(buffer["References"]).ToList();

				collection.Buffers.New(position, id, references);
			}

			// now that we have loaded all of the components, check for cycles
			foreach (var component in collection.GetComponentList())
			{
				component.UpdateCycleStatus();
			}
		}
    }
}