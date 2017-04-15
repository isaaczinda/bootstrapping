using System;
using System.Collections.Generic;

namespace Engine
{
    public class CustomGate : Gate
    {
		private Blueprint Master;

		public CustomGate(Blueprint MemberOf, string GateName, Coord Position)
			: base(Position, GateName)
		{
			this.SetupComponent(BlueprintLibrary.Lookup(GateName).getNumberInputs(), BlueprintLibrary.Lookup(GateName).getNumberOutputs());

			// lookup gate name, save it as field
			this.Master = BlueprintLibrary.Lookup(this.Name);
			this.Master.Changed += MasterChanged;

			Id = this.AddToBlueprint(MemberOf);
		}

		// only used for serialization
		public CustomGate(Blueprint MemberOf, Coord Position, Coord Dimensions, int Id, string GateName, ComponentState[] OutputStates, ComponentReference[] InputStates)
			: base(Position, GateName)
		{
			this.SetupComponent(InputStates, OutputStates);
			this.resetOutputs();

            this.calculateBoundingBoxes();

			this.Dimensions = Dimensions;
			this.Id = Id;

			// lookup gate name, save it as field
			this.Master = BlueprintLibrary.Lookup(this.Name);
			this.Master.Changed += MasterChanged;

			this.AddToBlueprint(MemberOf);
		}

		private void MasterChanged(object sender, EventArgs e)
		{
			// remove all references to this gate's outputs
			for (int index = 0; index < this.getNumberOutputs(); index++)
			{
				this.removeReferencesToOutput(index);
			}

			// clear all input and output states
			this.OutputStates = new ComponentState[Master.getNumberInputs()];

			this.SetInputs(new ComponentReference[Master.getNumberOutputs()]);

			// recalculate the bounding boxes
			this.calculateBoundingBoxes();
		}

		public override ComponentState[] Function(ComponentState[] Inputs, Dictionary<List<Component>, ComponentState[]> Memory, List<Component> MasterChain) {
            // each input can only have one state, so we can compare like this
            if (Inputs.Length != Master.getNumberInputs()) {
                throw new Exception("An incorrect number of inputs were passed");
            }

			// resolve function outputs
			return Master.ResolveOutputs(Memory, Inputs, MasterChain);
        }
    }

    public class And : Gate
    {
		public And(Blueprint MemberOf, Coord Position) : base(Position, "b_and") 
		{
			this.SetupComponent(2, 1);
            calculateBoundingBoxes();
			this.AddToBlueprint(MemberOf);
        }

		public override ComponentState[] Function(ComponentState[] Inputs, Dictionary<List<Component>, ComponentState[]> Memory, List<Component> MasterChain) {
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
		public Not(Blueprint MemberOf, Coord Position) : base(Position, "b_not") 
		{
			this.SetupComponent(1, 1);
            this.calculateBoundingBoxes();
        	this.AddToBlueprint(MemberOf);
		}

		public override ComponentState[] Function(ComponentState[] Inputs, Dictionary<List<Component>, ComponentState[]> Memory, List<Component> MasterChain) {
            if (Inputs[0] == ComponentState.True) {
                return new ComponentState[] {ComponentState.False};
            } else if (Inputs[0] == ComponentState.False){
                return new ComponentState[] {ComponentState.True};
            }

            return new ComponentState[] {ComponentState.Float};
        }
    }
}