using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Engine
{
	[JsonObject(MemberSerialization.OptIn)]
    public partial class Component
    {
		[JsonProperty]
		protected Coord Position;
		[JsonProperty]
		protected Coord Dimensions;
		[JsonProperty]
        protected int Id;
		[JsonProperty]
		protected string Name;
		[JsonProperty]
        protected ComponentType Type;

		protected Blueprint Parent;

		private bool InCycle;

		[JsonProperty]
		protected ComponentState[] OutputStates;
		[JsonProperty]
		private ComponentReference[] InputStates;

		protected List<BoundingBox> InputBoxes = new List<BoundingBox>();
		protected List<BoundingBox> OutputBoxes = new List<BoundingBox>();
		protected BoundingBox ButtonBox = null;

		// specific parameters for drawing
		public const int PADDING = 10;
		public const int REFERENCE_WIDTH = 30;
		public const int REFERENCE_HEIGHT = 20;

		public const int BUTTON_WIDTH = 30;
		public const int BUTTON_HEIGHT = 10;

		public const float FONT_SIZE = 16;

		public static string[] BaseComponentNames = new string[] { "input", "output", "b_and", "b_not", "clock" };

		//public Component()
		//{
		//	this.Initialize();
		//}

        public Component(Coord Position, ComponentType Type, String ComponentName) 
		{
			this.Position = Position;
            this.Type = Type;
			this.Name = ComponentName;
            this.InCycle = false; // since it has no references, it can't be in a 
        }

		/// <summary>
		/// adds the component to a parent blueprint
		/// </summary>
		/// <returns>the id that the blueprint has assigne to the component.</returns>
		/// <param name="MemberOf">blueprint to add the component to.</param>
		public int AddToBlueprint(Blueprint MemberOf)
		{
            this.Parent = MemberOf;
			return MemberOf.Add(this);
		}

		public static bool IsBaseComponent(string componentName)
		{
		return Component.BaseComponentNames.Contains(componentName);
		}

		public bool IsBase()
		{
			return Component.BaseComponentNames.Contains(this.getName());
		}

		public void Move(Coord amountToMove)
		{
			this.Position += amountToMove;
		}

		public void setPosition(Coord position)
		{
			this.Position = position;
		}

		public Coord getPosition()
		{
			return this.Position + Parent.GetPosition();
		}

		public Coord getDimensions()
		{
			return this.Dimensions;
		}

		public String getName()
		{
			return this.Name;
		}

		public ComponentType getType()
		{
			return this.Type;
		}

		/// <summary>
		/// Initialize the input and output states of a gate
		/// </summary>
		/// <param name="Inputs">Inputs.</param>
		/// <param name="Outputs">Outputs.</param>
		protected void SetupComponent(ComponentReference[] Inputs, ComponentState[] Outputs)
		{
			this.InputStates = Inputs;
			this.OutputStates = Outputs;
			this.resetOutputs();
		}

		protected void SetupComponent(int NumberInputs, int NumberOutputs)
		{
			this.InputStates = new ComponentReference[NumberInputs];
			this.OutputStates = new ComponentState[NumberOutputs];
		}

        public Component(ComponentType Type) {
            this.Type = Type;
        }

        public int getId() {
            return this.Id;
        }

		// RELATED TO BOUNDING BOXES

		protected void setButtonBox(BoundingBox box)
		{
			this.ButtonBox = box;
		}

		public BoundingBox getButtonBox()
		{
			return this.ButtonBox;
		}

		protected void clearBoundingBoxes()
		{
			this.InputBoxes = new List<BoundingBox>();
			this.OutputBoxes = new List<BoundingBox>();
		}

		protected void addInputBoundingBox(BoundingBox box)
		{
			this.InputBoxes.Add(box);
		}

		protected void addOutputBoundingBox(BoundingBox box)
		{
			this.OutputBoxes.Add(box);
		}

		public List<BoundingBox> getBoundingBoxes()
		{
			return InputBoxes.Concat(OutputBoxes).ToList();
		}

		public List<BoundingBox> getInputBoundingBoxes()
		{
			return InputBoxes;
		}

		public List<BoundingBox> getOutputBoundingBoxes()
		{
			return OutputBoxes;
		}

		// RELATED TO OUTPUTS:

		public int getNumberOutputs()
		{
			return this.OutputStates.Length;
		}

		public ComponentState[] getOutputs()
		{
			return this.OutputStates;
		}

		public bool setOutputs(ComponentState[] outputs)
		{
			// make sure that there are the correct number of outputs
			if (OutputStates.Length == this.getNumberOutputs())
			{
				this.OutputStates = outputs;
				return true;
			}
			else {
				return false;
			}
		}

		public void resetOutputs()
		{
			this.OutputStates = Enumerable.Repeat<ComponentState>(ComponentState.Float, this.getNumberOutputs()).ToArray();
		}

		public List<ComponentReference> getChildren(int index)
		{
			List<ComponentReference> componentReferences = new List<ComponentReference>();

			// find all components that reference the current component
			foreach (Edge edge in this.Parent.GetEdgeList())
			{
				ComponentReference temp = new ComponentReference(this, index);
				if (edge.Destination.Equals(temp))
				{
					componentReferences.Add(edge.Source);
				}
			}
				
			// find all buffers that reference the current component
			foreach (Buffer buffer in this.Parent.Buffers.GetItems())
			{
				// make sure the buffer actually has a reference
				if (buffer.GetComponentReference() != null)
				{
					// check to see if the buffer references this component
					if (buffer.GetComponentReference().Equals(new ComponentReference(this, index)))
					{
						List<Buffer> connectedBuffers = buffer.GetConnected();
						List<ComponentReference> referencesToBuffers = connectedBuffers.Select((arg) => new ComponentReference(arg)).ToList();

						// find all components that reference ANY of the buffers in the group
						foreach (Component component in this.Parent.GetComponentList())
						{
							for (int i = 0; i < component.getNumberInputs(); i++)
							{
								if (referencesToBuffers.Contains(component.getInputs()[i]))
								{
									componentReferences.Add(new ComponentReference(component, i));
								}
							}
						}
					}
				}
			}

			return componentReferences;
		}

		public void removeReferencesToOutput(int Index)
		{
			if (Index >= this.getNumberOutputs())
			{
				throw new Exception("index out of range.");
			}

			// search through all components in the collection and remove references to the targeted output
			// only search the collection that this component is a member of
			foreach (Component component in Parent.GetComponentList())
			{
				for (int i = 0; i < component.getNumberInputs(); i++)
				{
					// make sure that the input is specified
					if (component.getInputs()[i] != null)
					{
						if (component.getInputs()[i].Equals(new ComponentReference(this, Index)))
						{
							component.SetInput(null, i);
						}
					}
				}
			}

			// delete all buffer references to the component
			foreach (Buffer buffer in this.Parent.Buffers.GetItems())
			{
				// if a buffer references this component, remove that reference
				if (buffer.GetComponentReference() != null)
				{
					if (buffer.GetComponentReference().Equals(new ComponentReference(this, Index)))
					{
						buffer.RemoveReference(new ComponentReference(this, Index));
					}
				}
			}
		}

		// RELATED TO INPUTS:

		public ComponentReference[] getInputs()
		{
			return this.InputStates;
		}

		public int getNumberInputs()
		{
			return this.InputStates.Length;
		}

		/// <summary>
		/// set the input of a component at a certain index
		/// </summary>
		/// <param name="input">Input.</param>
		/// <param name="index">Index.</param>
		public void SetInput(ComponentReference input, int index)
		{
			if (index >= this.getNumberInputs())
			{
				throw new Exception("the index was out of range");
			}

			this.InputStates[index] = input;

			// since a reference has been added, checks if node is in a cycle
            this.UpdateCycleStatus();

		}

		/// <summary>
		/// Set the component's inputs
		/// </summary>
		/// <param name="input">Input.</param>
		public void SetInputs(ComponentReference[] input)
		{
			ComponentReference[] temp = new ComponentReference[input.Length];
			input.CopyTo(temp, 0);

			this.InputStates = temp;

			// since a reference has been added, checks if node is in a cycle
			this.UpdateCycleStatus();

		}

		/// <summary>
		/// checks whether this node is in a cycle or not
		/// </summary>
		/// <returns><c>true</c>, if node is in cycle, <c>false</c> otherwise.</returns>
		public bool IsInCycle()
		{
			return this.InCycle;
		}

		/// <summary>
		/// Determines if a node's outputs have an impact on its inputs
		/// </summary>
		public void UpdateCycleStatus()
		{
			Queue<Component> toSearch = new Queue<Component>();
			List<Component> searched = new List<Component>();

			//enqueue every node that references the current node to the queue
			foreach (ComponentReference reference in this.getInputs())
			{
				// make sure that the reference is not null
				if (reference != null)
				{
					if (this.Parent.ComponentExists(reference.getId()))
					{
						toSearch.Enqueue(this.Parent.GetComponentById(reference.getId()));
					}
				}
			}

			while (toSearch.Count != 0)
			{
				// dequeue a node, note that we have searched it
				Component currentNode = toSearch.Dequeue();
				searched.Add(currentNode);

				// get current node's children
				List<Component> children = new List<Component>();

				// get the component reference for each 
				foreach (ComponentReference reference in currentNode.getInputs())
				{
					if (reference != null)
					{
						if (this.Parent.ComponentExists(reference.getId()))
						{
							children.Add(this.Parent.GetComponentById(reference.getId()));
						}
					}
				}

				// if we have arrived back at the master node, return true
				if (children.Contains(this) || currentNode == this)
				{
					this.InCycle = true;
					return;
				}

				// find all children that haven't already been searched
				List<Component> unsearchedChildren = children.Where((arg) => !searched.Contains(arg)).ToList();

				// add all unsearched children to the queue
				unsearchedChildren.ForEach((obj) => toSearch.Enqueue(obj));
			}

			// if there are no more items in the queue
			this.InCycle = false;
			return;
		}
    }
}