using System;
using System.Collections.Generic;


namespace Engine
{
    public abstract class Gate : Component
	{
		public Gate(Coord Position, String Name)
			: base(Position, ComponentType.Gate, Name)
		{
			
		}

		//set the positions of input and output boxes based on how many inputs and outputs we have
		protected void calculateBoundingBoxes()
		{
			int referencesToFit = Math.Max(this.getNumberInputs(), this.OutputStates.Length);
			int width = referencesToFit * REFERENCE_WIDTH + (referencesToFit + 1) * PADDING;
			int height = PADDING * 3 + REFERENCE_HEIGHT * 2;

			// clear the bounding boxes
			this.clearBoundingBoxes();

			for (int inputNumber = 0; inputNumber < this.getNumberInputs(); inputNumber++)
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