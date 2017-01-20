using System.Linq;

namespace Engine
{
    // different states that a component can output
    public enum ComponentState {False, True, Float};

    // lists the three different types of component
    public enum ComponentType {Input, Output, Gate};

    // references a certain output of a given component
    public class ComponentReference
    {
        public int ReferenceId;
        public int Index;

        public ComponentReference(Component Reference, int Index) {
            this.ReferenceId = Reference.getId();
            this.Index = Index;
        }

        public ComponentReference(int ReferenceId, int Index) {
            this.ReferenceId = ReferenceId;
            this.Index = Index;
        }

        public ComponentReference(Component Reference) {
            this.ReferenceId = Reference.getId();
            this.Index = 0;
        }
    }


    public class Component
    {
        protected int Id;
        protected ComponentType Type;

        public ComponentType getType() {
            return this.Type;
        }

        public Component(ComponentCollection MemberOf, ComponentType Type) {
            // add to component collection, set Id accordingly
            this.Type = Type;
            this.Id = MemberOf.AddGate(this);
        }

        public Component(ComponentType Type) {
            this.Type = Type;
        }

        public int getId() {
            return this.Id;
        }
    }

    public class Input : Component
    {
        private ComponentState[] Outputs;

        public Input(ComponentCollection MemberOf) : base(MemberOf, ComponentType.Input) {
            this.Outputs = new ComponentState[] {ComponentState.Float};
        }

        public Input(ComponentCollection MemberOf, ComponentState[] Outputs, int Id) : base (ComponentType.Input) {
            this.Outputs = Outputs;
            MemberOf.AddGate(this);
            base.Id = Id;
        }

        public int getNumberOutputs() {
            return this.Outputs.Length;
        }

        public ComponentState[] getOutputs() {
            return this.Outputs;
        }

        public bool setOutputs(ComponentState[] outputs) {
            // make sure that there are the correct number of outputs
            if (Outputs.Length == this.getNumberOutputs()) {
                this.Outputs = outputs;
                return true;
            } else {
                return false;
            }
        }

        public void resetOutputs() {
            this.Outputs = Enumerable.Repeat<ComponentState>(ComponentState.Float, this.getNumberOutputs()).ToArray();
        }
    }

    public class Output : Component
    {
        private ComponentReference[] Inputs;

        public Output(ComponentCollection MemberOf, ComponentReference Reference) : base(MemberOf, ComponentType.Output) {
            this.Inputs = new ComponentReference[] {Reference};
        }

        // each output may have only one reference
        public Output(ComponentCollection MemberOf, ComponentReference Reference, int Id) : base (ComponentType.Output) {
            this.Inputs = new ComponentReference[] {Reference};
            MemberOf.AddGate(this);
            base.Id = Id;
        }

        public ComponentReference[] getInputs() {
            return this.Inputs;
        }

        public int getNumberInputs() {
            return this.Inputs.Length;
        }

        public bool setInputs(ComponentReference[] inputs) {
            // make sure that there are the correct number of inputs
            if (inputs.Length == this.getNumberInputs()) {
                this.Inputs = inputs;
                return true;
            } else {
                return false;
            }
        }
    }

    public abstract class Gate : Component
    {
        private ComponentReference[] Inputs;
        private ComponentState[] Outputs;

        public Gate(ComponentCollection MemberOf, int NumberInputs, int NumberOutputs) : base(MemberOf, ComponentType.Gate) {
            this.Inputs = new ComponentReference[NumberInputs];
            this.Outputs = new ComponentState[NumberOutputs];
            this.resetOutputs(); // zero the outputs
        }

        // each output may have only one reference
        public Gate(ComponentCollection MemberOf, ComponentReference[] Inputs, ComponentState[] Outputs) : base (ComponentType.Gate) {
            this.Inputs = Inputs;
            this.Outputs = Outputs;
            MemberOf.AddGate(this);
            base.Id = Id;
        }

        public int getNumberOutputs() {
            return this.Outputs.Length;
        }

        public ComponentState[] getOutputs() {
            return this.Outputs;
        }

        public bool setOutputs(ComponentState[] outputs) {
            // make sure that there are the correct number of outputs
            if (Outputs.Length == this.getNumberOutputs()) {
                this.Outputs = outputs;
                return true;
            } else {
                return false;
            }
        }

        public void resetOutputs() {
            this.Outputs = Enumerable.Repeat<ComponentState>(ComponentState.Float, this.getNumberOutputs()).ToArray();
        }

        public ComponentReference[] getInputs() {
            return this.Inputs;
        }

        public int getNumberInputs() {
            return this.Inputs.Length;
        }

        public bool setInputs(ComponentReference[] inputs) {
            // make sure that there are the correct number of inputs
            if (inputs.Length == this.getNumberInputs()) {
                this.Inputs = inputs;
                return true;
            } else {
                return false;
            }
        }

        // all inheritences must override this
        public abstract ComponentState[] Function(ComponentState[] Inputs);
    }
}