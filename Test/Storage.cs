using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Engine
{
    public static class Storage
    {
        private const string WORKING_FOLDER = "CustomComponents";
        private const string EXTENSION = ".gate";

        public static bool IsNameFree(String gateName) {
            gateName += Storage.EXTENSION; // add extension to gateName
            
            string[] files = Directory.GetFiles("CustomComponents");
            
            for (int i = 0; i < files.Length; i++) {
                if (files[i] == gateName) {
                    return false;
                }
            }
            return true;
        }

        private static string FilepathFromName(string name) {
            return Path.Combine(Storage.WORKING_FOLDER, name + Storage.EXTENSION);
        }

        public static void SaveComponent (ComponentCollection Component) {
            // create working folder if it doesn't exist
            if (!Directory.Exists(Storage.WORKING_FOLDER)) {
                Directory.CreateDirectory(Storage.WORKING_FOLDER);
            }

            // get where the file should be stored given its name
            string filePath = Storage.FilepathFromName(Component.GetName());
                        
            String serializedJson = JsonConvert.SerializeObject(Component);
            
            // creates a new file, then writes data there
            File.WriteAllText(filePath, serializedJson);
        }

        // public static ComponentCollection LoadComponent (string name) {
        //     string filePath = Storage.FilepathFromName(name);
        //     string jsonString = File.ReadAllText(filePath);
        //     JObject jObject = JObject.Parse(jsonString);

        //     // get the name
        //     String componentName = (String) jObject["Name"];

        //     // create a component collection that we will save to
        //     ComponentCollection collection = new ComponentCollection(componentName);

        //     // extract items and converts to List<ComponentCollection>
        //     var items = from item in jObject["Items"] select (JObject)item;
            
        //     for(int i = 0; i < items.Count(); i++) {
        //         ComponentType type = (ComponentType)(int)items.ElementAt(i)["Type"]; 
        //         int numberOutputs = (int)items.ElementAt(i)["NumberOutputs"];
        //         int numberInputs = (int)items.ElementAt(i)["NumberInputs"];
        //         int id = (int)items.ElementAt(i)["Id"];

        //         // converts items into an array of component references
        //         JToken[] inputTokens = items.ElementAt(i)["Inputs"].ToArray();

        //         if (inputTokens.Length == 0) {

        //         }

        //         IEnumerable<ComponentReference> inputList = inputTokens.Select(input => 
        //             new ComponentReference(Convert.ToInt32(input["ReferenceId"].ToString()), Convert.ToInt32(input["Index"].ToString())));
        //             ComponentReference[] inputArray = inputList.ToArray();
                
        //         // populate output array with ComponentState.Float values
        //         ComponentState[] outputArray = Enumerable.Range(0, numberOutputs).Select(number => ComponentState.Float).ToArray();

        //         if (type == ComponentType.Input) {
        //             new Input(collection, inpu)
        //         }
        //         // create a new component object, which is automatically bound to a component collection
        //         new Component(collection, inputArray, outputArray, type, id);
        //     }

        //     return collection;
        // }

        
    }
}