using System;
namespace Engine
{
	public class Output : Component
	{
		// used to load components from memory
		public Output(Blueprint MemberOf, Coord Position, Coord Dimensions, int Id, string Name, ComponentState[] OutputStates, ComponentReference[] InputStates)
		{
			this.Position = Position;
			this.Dimensions = Dimensions;
			this.Id = Id;
			this.Name = Name;
			this.Type = ComponentType.Output;
			this.OutputStates = OutputStates;
			this.InputStates = InputStates;
			this.Parent = MemberOf;
			MemberOf.Add(this);
			this.calculateBoundingBoxes();
		}

		public Output(Blueprint MemberOf, Coord Position) : base(MemberOf, Position, ComponentType.Output, "output")
		{
			base.setupComponent(1, 1);
			base.resetOutputs();
			calculateBoundingBoxes();

            this.Id = MemberOf.Add(this);
		}

		protected void calculateBoundingBoxes()
		{
			int referencesToFit = this.InputStates.Length;
			int width = referencesToFit * REFERENCE_WIDTH + (referencesToFit + 1) * PADDING;
			int height = PADDING * 3 + REFERENCE_HEIGHT + BUTTON_HEIGHT;

			Coord buttonUpperLeft = new Coord(PADDING, PADDING * 2 + REFERENCE_HEIGHT);
			Coord buttonLowerRight = new Coord(buttonUpperLeft.x + BUTTON_WIDTH, buttonUpperLeft.y + BUTTON_HEIGHT);

			this.setButtonBox(new BoundingBox(buttonUpperLeft, buttonLowerRight));

			for (int inputNumber = 0; inputNumber < this.InputStates.Length; inputNumber++)
			{
				Coord upperLeft = new Coord((inputNumber * REFERENCE_WIDTH) + (inputNumber + 1) * (PADDING), PADDING);
				Coord lowerRight = new Coord(upperLeft.x + REFERENCE_WIDTH, upperLeft.y + REFERENCE_HEIGHT);

				base.addInputBoundingBox(new BoundingBox(upperLeft, lowerRight));
			}

			// set dimensions of entire gate
			this.Dimensions = new Coord(width, height);
		}

		// each output may have only one reference
		public Output(Blueprint MemberOf, ComponentReference Reference, int Id) : base(ComponentType.Output)
		{
			this.InputStates = new ComponentReference[] { Reference };
			MemberOf.Add(this);
			base.Id = Id;
		}
	}
}
