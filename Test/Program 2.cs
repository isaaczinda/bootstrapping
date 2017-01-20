using System;

namespace Engine 
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // create the NAND collection
            ComponentCollection NAnd = CollectionManager.CreateComponentCollection("nand");
            
            Input A = new Input(NAnd);
            Input B = new Input(NAnd);
            And and = new And(NAnd, new ComponentReference(A), new ComponentReference(B));
            Not not = new Not(NAnd, new ComponentReference(and));
            Output o = new Output(NAnd, new ComponentReference(not));

            ComponentCollection Or = CollectionManager.CreateComponentCollection("or");
            Input C = new Input(Or);
            Input D = new Input(Or);

            CustomGate nand1 = new CustomGate(Or, "nand");
            nand1.setInputs(new ComponentReference[] {new ComponentReference(C), new ComponentReference(C)});
            CustomGate nand2 = new CustomGate(Or, "nand");
            nand2.setInputs(new ComponentReference[] {new ComponentReference(D), new ComponentReference(D)});

            CustomGate nand3 = new CustomGate(Or, "nand");
            nand3.setInputs(new ComponentReference[] {new ComponentReference(nand1), new ComponentReference(nand2)});

            Output test = new Output(Or, new ComponentReference(nand3));

            printComponentState(Or.ResolveOutputs(new ComponentState[] {ComponentState.False, ComponentState.False}));
            printComponentState(Or.ResolveOutputs(new ComponentState[] {ComponentState.True, ComponentState.False}));
            printComponentState(Or.ResolveOutputs(new ComponentState[] {ComponentState.False, ComponentState.True}));
            printComponentState(Or.ResolveOutputs(new ComponentState[] {ComponentState.True, ComponentState.True}));
            printComponentState(Or.ResolveOutputs(new ComponentState[] {ComponentState.Float, ComponentState.True}));
            printComponentState(Or.ResolveOutputs(new ComponentState[] {ComponentState.True, ComponentState.Float}));
        }
    }
}