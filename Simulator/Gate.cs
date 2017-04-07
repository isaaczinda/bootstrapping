using System;
using System.Collections.Generic;


namespace Engine
{
    public abstract class Gate : Component
	{
		public Gate(ComponentState[] OutputStates, ComponentReference[] InputStates)
		{
			this.Type = ComponentType.Gate;
			this.OutputStates = OutputStates;
			this.InputStates = InputStates;
			calculateBoundingBoxes();
		}

		public Gate(Blueprint MemberOf, int NumberInputs, int NumberOutputs, Coord Position, String Name) : base(MemberOf, Position, ComponentType.Gate, Name)
		{
			base.setupComponent(NumberInputs, NumberOutputs);
			base.resetOutputs();
			calculateBoundingBoxes();

            this.Id = MemberOf.Add(this);
		}

		//set the positions of input and output boxes based on how many inputs and outputs we have
		protected void calculateBoundingBoxes()
		{
			int referencesToFit = Math.Max(this.InputStates.Length, this.OutputStates.Length);
			int width = referencesToFit * REFERENCE_WIDTH + (referencesToFit + 1) * PADDING;
			int height = PADDING * 3 + REFERENCE_HEIGHT * 2;

			// clear the bounding boxes
			this.clearBoundingBoxes();

			for (int inputNumber = 0; inputNumber < this.InputStates.Length; inputNumber++)
			{
				Coord upperLeftCorner = new Coord((inputNumber * REFERENCE_WIDTH) + (inputNumber + 1) * (PADDING), PADDING);
				Coord lowerRightCorner = new Coord(upperLeftCorner.x + REFERENCE_WIDTH, upperLeftCorner.y + REFERENCE_HEIGHT);

				base.addInputBoundingBox(new BoundingBox(upperLeftCorner, lowerRightCorner));
			}

			for (int outputNumber = 0; outputNumber < this.OutputStates.Length; outputNumber++)
			{
				Coord upperLeftCorner = new Coord((outputNumber * REFERENCE_WIDTH) + (outputNumber + 1) * PADDING, PADDING * 2 + REFERENCE_HEIGHT);
				Coord lowerRightCorner = new Coord(upperLeftCorner.x + REFERENCE_WIDTH, upperLeftCorner.y + REFERENCE_HEIGHT);

				base.addOutputBoundingBox(new BoundingBox(upperLeftCorner, lowerRightCorner));
			}

			// set dimensions of entire gate
			this.Dimensions = new Coord(width, height);
		}

		// all inheritences must override this
		public abstract ComponentState[] Function(ComponentState[] Inputs, Dictionary<List<Component>, ComponentState[]> Memory, List<Component> MasterChain);
	}
}