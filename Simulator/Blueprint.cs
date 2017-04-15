using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Engine
{
	// objects will not be serialized unless specified
	[JsonObject(MemberSerialization.OptIn)]
	public partial class Blueprint
    {
		[JsonProperty]
		private List<Component> Items = new List<Component>();
		private List<Component> Outputs = new List<Component>();
        private List<Component> Inputs = new List<Component>();

		private List<String> Dependencies = new List<String>();

		[JsonProperty]
		public BufferCollection Buffers;

		[JsonProperty]
		private String Name;
		 
		[JsonProperty]
		private Coord Position;

		// event that fires whenever the component collection has changed
		public event EventHandler<EventArgs> Changed;

		public Blueprint(String Name)
		{
			// makes sure same name is not reused
			if (Storage.IsNameFree(Name) == false)
			{
				throw new Exception("Name is already taken.");
			}

			this.Name = Name;
			this.Position = new Coord(0, 0);

			// create a new tool for managing the buffers
			this.Buffers = new BufferCollection(this);
		}

		/// <summary>
		/// checks if there is a cycle in the blueprint
		/// </summary>
		/// <returns><c>true</c>, if a cycle exists in the blueprint, <c>false</c> otherwise.</returns>
		public bool ContainsCycles()
		{
			foreach (Component component in this.GetComponentList())
			{
				if (component.IsInCycle())
				{
					return true;
				}
			}
			return false;
		}

		public List<Edge> GetEdgeList()
		{
			List<Edge> edgeList = new List<Edge>();

			foreach (Component Item in Items)
			{
				int index = 0;
				foreach (ComponentReference destination in Item.getInputs())
				{
					if (destination != null)
					{
						// get the source node
						ComponentReference source = new ComponentReference(Item, index);
						edgeList.Add(new Edge(source, destination));
					}
					index++;
				}
			}

			return edgeList;
		}

		protected virtual void CollectionChanged(EventArgs e)
		{
			if (Changed != null)
			{
				Changed(this, e);
			}
		}

		/// <summary>
		///  Gets a list of each component in blueprint
		/// </summary>
		/// <returns>The list of components.</returns>
		public List<Component> GetComponentList()
		{
			return this.Items;
		}

        public int getNumberOutputs() {
            return this.Outputs.Count;
        }

        public int getNumberInputs() {
            return this.Inputs.Count;
        }

        public String GetName() {
            return this.Name;
        }

		/// <summary>
		/// get if a component with a given id exists
		/// </summary>
		/// <returns><c>true</c>, if exists, <c>false</c> otherwise.</returns>
		/// <param name="Id">Identifier.</param>
		public bool ComponentExists(int Id)
		{
			foreach (Component component in this.GetComponentList())
			{
				if (component.getId() == Id)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the component by identifier.
		/// </summary>
		/// <returns>The component by identifier.</returns>
		/// <param name="Id">Identifier.</param>
		public Component GetComponentById(int Id)
		{
			foreach (Component component in this.GetComponentList())
			{
				if (component.getId() == Id)
				{
					return component;
				}
			}

			throw new Exception("no component exists with the specified Id");
		}

		public List<string> getDependencies()
		{
			return this.Dependencies;
		}

		public Coord GetPosition()
		{
			return this.Position;
		}

		public void setPosition(Coord position)
		{
			this.Position = position;
		}

		public void Move(Coord amountToMove)
		{
			this.Position += amountToMove;
		}

		private int nextAvailableId()
		{
			List<int> ids = (from item in this.GetComponentList() select item.getId()).ToList();
			return ids.Max() + 1;
		}

		private bool IsDependencyOf(Blueprint collection)
		{
			Queue<string> namesToResolve = new Queue<string>();

			// see if the collections are the same
			if (collection.GetName() == this.GetName())
			{
				return true;
			}

			// add each dependency of the collection to the queue
			foreach (string dependency in collection.getDependencies())
			{
				if (!Component.IsBaseComponent(dependency))
				{
					namesToResolve.Enqueue(dependency);
				}
			}

			while (namesToResolve.Count != 0)
			{
				string currentCollectionName = namesToResolve.Dequeue();

				// if of of the passses collection's dependencies is actually this collection
				if (currentCollectionName == this.GetName())
				{
					return true;
				}

				List<string> collectionDependencies = BlueprintLibrary.Lookup(currentCollectionName).getDependencies();

				// add each dependency of the collection to the queue
				foreach (string dependency in collectionDependencies)
				{
					if (!Component.IsBaseComponent(dependency))
					{
						namesToResolve.Enqueue(dependency);
					}
				}
			}

			return false;
		}

        // returns the id of the gate in question
        public int Add(Component ToAdd) 
		{
			// make sure that adding this item won't add dependency cycles
			if (!ToAdd.IsBase())
			{
				if (this.IsDependencyOf(BlueprintLibrary.Lookup(ToAdd.getName())))
				{
					Console.WriteLine("you cannot create referental loops with gates");
					return -1;
				}
			}

			// add the compoennt to the items list
			this.Items.Add(ToAdd);

			// add the item to the Blueprint's memory, its value will be float
			List<ComponentState> tempStates = new List<ComponentState>();

			for (int i = 0; i < ToAdd.getNumberOutputs(); i++)
			{
				tempStates.Add(ComponentState.Float);
			}

			this.Memory.Add(CopyList(new List<Component>(), ToAdd), tempStates.ToArray());

			// add the Item's name to the list of dependencies, we can only add things that are gates
			if (!this.Dependencies.Contains(ToAdd.getName()) && !Component.IsBaseComponent(ToAdd.getName()))
			{
				this.Dependencies.Add(ToAdd.getName());
			}

            // if the gate to add is an output or input, add to special list
            if (ToAdd.getType() == ComponentType.Output) 
			{
                this.Outputs.Add((Output)ToAdd); // cast to output
            } 
			else if (ToAdd.getType() == ComponentType.Input) 
			{
                this.Inputs.Add((Input)ToAdd); // cast to input
            }

			// fire the "Changed" event, if an input or output was added
			if (ToAdd.getType() == ComponentType.Input || ToAdd.getType() == ComponentType.Output)
			{
				this.CollectionChanged(EventArgs.Empty);
			}

			return nextAvailableId(); // return the id of the newly added component
        }

		public void Delete(Component ToDelete)
		{
			// delete all references to the component
			for (int index = 0; index < ToDelete.getNumberOutputs(); index++)
			{
				ToDelete.removeReferencesToOutput(index);
			}

			// delete the actual gate
			Items.Remove(ToDelete);

			// remove from special collections, fire the "Changed" event
			if (ToDelete.getType() == ComponentType.Input)
			{
				Inputs.Remove(ToDelete);
				this.CollectionChanged(EventArgs.Empty);
			}
			if (ToDelete.getType() == ComponentType.Output)
			{
				Outputs.Remove(ToDelete);
				this.CollectionChanged(EventArgs.Empty);
			}


			// if the collection contains no other items like the one we just deleted, remove the deleted component from dependencies
			if (!this.Items.Select((arg) => arg.getName()).Contains(ToDelete.getName()))
			{
				this.Dependencies.Remove(ToDelete.getName());
			}
		}

		private void setInputStates(ComponentState[] InputStates) {
            // sort inputs by where they are located on the screen
			List<Component> sortedInputs = this.Inputs.OrderBy((Component arg) => arg.getPosition().x).ToList();


			// set all input states
            for (int i = 0; i < InputStates.Length; i++) {
				sortedInputs[i].setOutputs(new ComponentState[] {InputStates[i]});
            }
        }

		// gets inputs ordered from left to right
		public ComponentState[] getInputStates()
		{
			// sort inputs by where they are located on the screen
			List<Component> sortedInputs = this.Inputs.OrderBy((Component arg) => arg.getPosition().x).ToList();

			// get the state for each input IN ORDER
			ComponentState[] inputs = new ComponentState[this.getNumberInputs()];
			for (int i = 0; i < this.getNumberInputs(); i++)
			{
				inputs[i] = sortedInputs[i].getOutputs()[0];
			}

			return inputs;
		}

		// gets inputs ordered from left to right
		private Component[] getInputComponents()
		{
			// sort inputs by where they are located on the screen
			var sortedInputs = this.Inputs.OrderBy((Component arg) => arg.getPosition().x);

			return (from item in sortedInputs select (Input)item).ToArray();
		}

		private Output[] getOutputComponents()
		{
			// sort outputs by where they are located on the screen
			return (from item in this.Outputs.OrderBy((Component arg) => arg.getPosition().x) select (Output)item).ToArray();
		}
    }
}