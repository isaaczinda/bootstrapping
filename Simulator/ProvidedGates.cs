using System;
using System.Collections.Generic;

namespace Engine
{
    public class CustomGate : Gate
    {
        private ComponentCollection RootCollection;

		public CustomGate(ComponentCollection MemberOf, string GateName, Coord Position)
			: base(MemberOf, CollectionManager.Lookup(GateName).getNumberInputs(), CollectionManager.Lookup(GateName).getNumberOutputs(), Position, GateName)
		{
			this.Initialize();
		}

		// only used for serialization
		public CustomGate(ComponentCollection MemberOf, Coord Position, Coord Dimensions, int Id, string GateName, ComponentState[] OutputStates, ComponentReference[] InputStates)
			: base(OutputStates, InputStates)
		{
			this.Position = Position;
			this.Dimensions = Dimensions;
			this.Id = Id;
			this.Name = GateName;
			this.Parent = MemberOf;
			MemberOf.Add(this);

			this.calculateBoundingBoxes();

			this.Initialize();
		}

		private void Initialize()
		{
			// lookup gate name, save it as field
			this.RootCollection = CollectionManager.Lookup(this.Name).addDependentGate(this);
			this.RootCollection.Changed += RootCollectionChanged;
		}

		private void RootCollectionChanged(object sender, EventArgs e)
		{
			// remove all references to this gate's outputs
			for (int index = 0; index < this.getNumberOutputs(); index++)
			{
				this.removeReferencesToOutput(index);
			}

			// clear all input and output states
			this.OutputStates = new ComponentState[RootCollection.getNumberInputs()];
			this.InputStates = new ComponentReference[RootCollection.getNumberOutputs()];


			// recalculate the bounding boxes
			this.calculateBoundingBoxes();
		}

        public override ComponentState[] Function(ComponentState[] Inputs) {
            // each input can only have one state, so we can compare like this
            if (Inputs.Length != RootCollection.getNumberInputs()) {
                throw new Exception("An incorrect number of inputs were passed");
            }

            // resolve function outputs
			return RootCollection.ResolveOutputs(this, Inputs);
        }
    }

    public class And : Gate
    {
		public And(ComponentCollection MemberOf, Coord Position) : base(MemberOf, 2, 1, Position, "b_and") {
        }

        public override ComponentState[] Function(ComponentState[] Inputs) {
			// if both values are true, we can guarentee a true return
			if (Inputs[0] == ComponentState.True && Inputs[1] == ComponentState.True)
			{
				return new ComponentState[] { ComponentState.True };
			}
			// if one value is false, the output must be false
			else if (Inputs[1] == ComponentState.False || Inputs[0] == ComponentState.False)
			{
				return new ComponentState[] { ComponentState.False };
			}
			// we don't know what's up
			else
			{
				return new ComponentState[] { ComponentState.Float };
			}
        }
    }

    public class Not : Gate
    {
		public Not(ComponentCollection MemberOf, Coord Position) : base(MemberOf, 1, 1, Position, "b_not") {
        }

        public override ComponentState[] Function(ComponentState[] Inputs) {
            if (Inputs[0] == ComponentState.True) {
                return new ComponentState[] {ComponentState.False};
            } else if (Inputs[0] == ComponentState.False){
                return new ComponentState[] {ComponentState.True};
            }

            return new ComponentState[] {ComponentState.Float};
        }
    }
}