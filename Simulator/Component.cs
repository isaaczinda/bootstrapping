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

		protected ComponentCollection Parent;

		[JsonProperty]
		protected ComponentState[] OutputStates;
		[JsonProperty]
		protected ComponentReference[] InputStates;

		protected List<BoundingBox> InputBoxes = new List<BoundingBox>();
		protected List<BoundingBox> OutputBoxes = new List<BoundingBox>();
		protected BoundingBox ButtonBox = null;

		// specific parameters for drawing
		public const int PADDING = 10;
		public const int REFERENCE_WIDTH = 50;
		public const int REFERENCE_HEIGHT = 30;

		public const int BUTTON_WIDTH = 50;
		public const int BUTTON_HEIGHT = 15;

		public const float FONT_SIZE = 16;

		public static string[] BaseComponentNames = new string[] { "input", "output", "b_and", "b_not", "clock" };

		public Component()
		{
		}

		public static bool IsBaseComponent(string componentName)
		{
			return Component.BaseComponentNames.Contains(componentName);
		}

		public bool IsBase()
		{
			return Component.BaseComponentNames.Contains(this.getName());
		}

        public Component(ComponentCollection MemberOf, Coord Position, ComponentType Type, String ComponentName) {
			// add to component collection, set Id accordingly
			this.Position = Position;
            this.Type = Type;
			this.Name = ComponentName;
			this.Parent = MemberOf;
            this.Id = MemberOf.Add(this);
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

		protected void setupComponent(int numberInputs, int numberOutputs)
		{
			this.OutputStates = new ComponentState[numberOutputs];
			this.InputStates = new ComponentReference[numberInputs];
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
			foreach (Edge edge in this.Parent.getEdgeList())
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
						foreach (Component component in this.Parent.getItems())
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
			foreach (Component component in Parent.getItems())
			{
				for (int i = 0; i < component.getNumberInputs(); i++)
				{
					// make sure that the input is specified
					if (component.getInputs()[i] != null)
					{
						if (component.getInputs()[i].Equals(new ComponentReference(this, Index)))
						{
							component.setInput(null, i);
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

		public void setInputs(ComponentReference[] inputs)
		{
			for (int i = 0; i < inputs.Length; i++)
			{
				this.setInput(inputs[i], i);
			}
		}

		public void setInput(ComponentReference input, int index)
		{
			if (index >= this.getNumberInputs())
			{
				throw new Exception("the index was out of range");
			}

			this.InputStates[index] = input;
		}
    }
}