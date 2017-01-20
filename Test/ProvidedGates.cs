using System;
using System.Collections.Generic;

namespace Engine
{
    public class CustomGate : Gate
    {
        private ComponentCollection RootCollection;

        public override ComponentState[] Function(ComponentState[] Inputs) {
            // each input can only have one state, so we can compare like this
            if (Inputs.Length != RootCollection.getNumberInputs()) {
                throw new Exception("An incorrect number of inputs were passed");
            }

            // resolve function outputs
            return RootCollection.ResolveOutputs(Inputs);
        }

		public CustomGate(ComponentCollection MemberOf, string GateName, Coord Position) 
            : base(MemberOf, CollectionManager.Lookup(GateName).getNumberInputs(), CollectionManager.Lookup(GateName).getNumberOutputs(), Position) {
                // lookup gate name, save it as field
                this.RootCollection = CollectionManager.Lookup(GateName);
        }
    }

    public class Or : Gate
    {
		public Or(ComponentCollection MemberOf, Coord Position) : base(MemberOf, 2, 1, Position){
        }

        public override ComponentState[] Function(ComponentState[] Inputs) {
            if (Inputs[0] == ComponentState.Float || Inputs[1] == ComponentState.Float) {
                return new ComponentState[] {ComponentState.Float};
            // make sure that any floating value returns a floating value
            } else if (Inputs[0] == ComponentState.True || Inputs[1] == ComponentState.True) {
                return new ComponentState[] {ComponentState.True};
            } else {
                return new ComponentState[] {ComponentState.False};
            }
        }
    }

    public class And : Gate
    {
		public And(ComponentCollection MemberOf, Coord Position) : base(MemberOf, 2, 1, Position) {
        }

        public override ComponentState[] Function(ComponentState[] Inputs) {
            if (Inputs[0] == ComponentState.True && Inputs[1] == ComponentState.True) {
                return new ComponentState[] {ComponentState.True};
            // make sure that any floating value returns a floating value
            } else if (Inputs[0] == ComponentState.Float || Inputs[1] == ComponentState.Float) {
                return new ComponentState[] {ComponentState.Float};
            } else {
                return new ComponentState[] {ComponentState.False};
            }
        }
    }

    public class Not : Gate
    {
		public Not(ComponentCollection MemberOf, Coord Position) : base(MemberOf, 1, 1, Position) {
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