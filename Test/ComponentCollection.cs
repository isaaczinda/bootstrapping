using System;
using System.Collections.Generic;

namespace Engine 
{
    public static class CollectionManager
    {
        private static Dictionary<String, ComponentCollection> ActiveCollections = new Dictionary<String, ComponentCollection>();
        
        public static ComponentCollection CreateComponentCollection(String Name) {
            // throw an exception if the name is taken
            if (!Storage.IsNameFree(Name)) {
                throw new Exception("Component name is already taken.");
            }

            // create a new collection, add it to the dictionary
            ComponentCollection newCollection = new ComponentCollection(Name);
            ActiveCollections.Add(Name, newCollection);

            return newCollection;
        }

        public static ComponentCollection Lookup(String Name) {
            if (!ActiveCollections.ContainsKey(Name)) {
                throw new Exception("The component that you are trying to use do esnmot exist");
            }

            return ActiveCollections[Name];
        }
    }

    public class ComponentCollection
    {
        // these have to be public for the serializer
        private List<Component> Items = new List<Component>();
        private List<Output> Outputs = new List<Output>();
        private List<Input> Inputs = new List<Input>();

        private String Name;

        public int getNumberOutputs() {
            return this.Outputs.Count;
        }

        public int getNumberInputs() {
            return this.Inputs.Count;
        }

        public String GetName() {
            return this.Name;
        }

        public Component[] getInputsComponents() {
            return this.Inputs.ToArray();
        }

        public void reorderInputComponents(Input[] inputs) {
            // check to make sure components have same shit
            if (Inputs.Count != this.Inputs.Count) {
                throw new Exception("Incorrect number of inputs.");
            }

            this.Inputs = new List<Input>(inputs);
        }

        public ComponentCollection(String Name) {
            // makes sure same name is not reused
            if (Storage.IsNameFree(Name) == false) {
                throw new Exception("Name is already taken.");
            }

            this.Name = Name;
        }

        // returns the id of the gate in question
        public int AddGate(Component ToAdd) {
            Items.Add(ToAdd);

            // if the gate to add is an output or input, add to special list
            if (ToAdd.getType() == ComponentType.Output) {
                this.Outputs.Add((Output)ToAdd); // cast to output
            } else if (ToAdd.getType() == ComponentType.Input) {
                this.Inputs.Add((Input)ToAdd); // cast to input
            }

            return Items.Count - 1; // return the id of the newly added component
        }

        private void setInputs(ComponentState[] InputStates) {
            // set all input states
            for (int i = 0; i < InputStates.Length; i++) {
                this.Inputs[i].setOutputs(new ComponentState[] {InputStates[i]});
            }
        }

        // method resolves all components in component collection by working from outputs
        public ComponentState[] ResolveOutputs(ComponentState[] InputStates) {
            // set inputs to those that were passed
            this.setInputs(InputStates);
            
            ComponentState[] OutputStates = new ComponentState[this.getNumberOutputs()];
            
            for (int i = 0; i < this.getNumberOutputs(); i++) {
                OutputStates[i] = this.ResolveComponent(this.Outputs[i])[0];
            }

            return OutputStates;
        }

        

        public ComponentState[] ResolveComponent(Component ToResolve) {
            // make certain that the component is in the current component collection
            if (!Items.Contains(ToResolve)) {
                throw new Exception("The component references a component not in this collection.");
            }
            
            // if the object that we are resolving is an input
            if (ToResolve.getType() == ComponentType.Input) {
                return ((Input)ToResolve).getOutputs();
            }

            // if the object that we are resolving is an output
            else if (ToResolve.getType() == ComponentType.Output) {
                Output OutputToResolve = (Output)ToResolve;

                // we know that it only has one reference, and no function operates on this reference
                int SourceIndex = OutputToResolve.getInputs()[0].Index;
                int ComponentToResolveIndex = OutputToResolve.getInputs()[0].ReferenceId;

                // get the outputs of the component we are referencing
                ComponentState[] componentOutputs = ResolveComponent(this.Items[ComponentToResolveIndex]);

                // return the correct value
                ComponentState[] Value = new ComponentState[] { componentOutputs[SourceIndex] };
                return Value;
            }

            // if the object is a gate, apply the function to resolve it
            else if (ToResolve.getType() == ComponentType.Gate) {
                Gate GateToResolve = (Gate) ToResolve;
                
                // create an array that will be filled with resolved inputs
                ComponentState[] InputValues = new ComponentState[GateToResolve.getNumberInputs()];

                // recursively find each of the inputs
                for (int i = 0; i < InputValues.Length; i++) {
                    // resolve the target component, and retrieve the output at the correct index
                    int ComponentToResolveIndex = GateToResolve.getInputs()[i].ReferenceId;
                    InputValues[i] = ResolveComponent(this.Items[ComponentToResolveIndex])[GateToResolve.getInputs()[i].Index];
                }

                // run the funciton that ToResolve 
                ComponentState[] CurrentGateOutputs = GateToResolve.Function(InputValues);

                return CurrentGateOutputs;
            }

            // if we are resolving an unexpected type, return null
            else {
                throw new Exception("Encountered unexpected gate type.");
            }
        } 
    }
}