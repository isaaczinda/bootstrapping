using System;
using System.Timers;

namespace Engine
{
	public class Input : Component
	{
		public Input(Blueprint MemberOf, Coord Position, Coord Dimensions, int Id, string Name, ComponentState[] OutputStates, ComponentReference[] InputStates)
			: base(Position, ComponentType.Input, Name)
		{
			this.Dimensions = Dimensions;
			this.Id = Id;

			this.SetupComponent(InputStates, OutputStates);

			this.calculateBoundingBoxes();

			this.AddToBlueprint(MemberOf);
		}

		public Input(Blueprint MemberOf, Coord Position) : base(Position, ComponentType.Input, "input")
		{
			this.SetupComponent(0, 1);
			this.setOutputs(new ComponentState[] { ComponentState.False });
			calculateBoundingBoxes();

			this.Id = this.AddToBlueprint(MemberOf);
		}

		public void Toggle()
		{
			//toggle the outputs on the input
			ComponentState previousState = this.getOutputs()[0];
			ComponentState currentState = ComponentState.False;

			if (previousState == ComponentState.False)
			{
				currentState = ComponentState.True;
			}

			this.setOutputs(new ComponentState[] { currentState });

			UserInterface.CircutChanged();
		}

		//set the positions of input and output boxes based on how many inputs and outputs we have
		protected void calculateBoundingBoxes()
		{
			int referencesToFit = this.OutputStates.Length;
			int width = referencesToFit * REFERENCE_WIDTH + (referencesToFit + 1) * PADDING;
			int height = PADDING * 3 + REFERENCE_HEIGHT + BUTTON_HEIGHT;

			this.setButtonBox(new BoundingBox(new Coord(PADDING, PADDING), new Coord(PADDING + BUTTON_WIDTH, PADDING + BUTTON_HEIGHT)));

			for (int outputNumber = 0; outputNumber < this.OutputStates.Length; outputNumber++)
			{
				Coord upperLeftCorner = new Coord((outputNumber * REFERENCE_WIDTH) + (outputNumber + 1) * PADDING, PADDING * 2 + BUTTON_HEIGHT);
				Coord lowerRightCorner = new Coord(upperLeftCorner.x + REFERENCE_WIDTH, upperLeftCorner.y + REFERENCE_HEIGHT);

				base.addOutputBoundingBox(new BoundingBox(upperLeftCorner, lowerRightCorner));
			}

			// set dimensions of entire gate
			this.Dimensions = new Coord(width, height);
		}
	}

	public class Clock : Input
	{
		// the clock starts not running
		private bool Running = false;
		public const int CLOCK_SPEED = 1000;
		public Timer timer;

		public bool IsClockRunning()
		{
			return this.Running;
		}

		// create new toggle input functionality
		public void ToggleClockState()
		{
			// toggle whether or not the clock is on
			Running = !Running;

			// if the clock has started, toggle the input 
			if (Running)
			{
				this.Toggle();
			}
			// if we have just turned the clock off, set the output to false
			else
			{
				timer.Stop();
				this.setOutputs(new ComponentState[] { ComponentState.False });
				UserInterface.CircutChanged();
			}
		}

		public Clock(Blueprint MemberOf, Coord Position, Coord Dimensions, int Id, string Name, ComponentState[] OutputStates, ComponentReference[] InputStates)
			: base(MemberOf, Position, Dimensions, Id, Name, OutputStates, InputStates)

		{
			this.SetupClock();
		}

		public Clock(Blueprint MemberOf, Coord Position) : base(MemberOf, Position)
		{
			this.SetupClock();
		}

		private void SetupClock()
		{
			base.Name = "clock";

			this.Parent.CircutResolved += delegate
			{
				if (this.Running)
				{
					// start counting down to change the clock value
					timer.Start();
				}
			};

			// setup the timer for the clock
			timer = new Timer(CLOCK_SPEED);
			timer.AutoReset = false;
			timer.Elapsed += (o, ea) =>
			{
				// add a new clock toggle event
				BlueprintLibrary.Events.Add(new Event(this, Event.Type.ClockToggle));
			};
		}
	}
}