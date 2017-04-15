using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
	public class ListEquityComparer : IEqualityComparer<List<Component>>
	{
		public bool Equals(List<Component> x, List<Component> y)
		{
			if (x.Count != y.Count)
			{
				return false;
			}
			for (int i = 0; i < x.Count; i++)
			{
				if (x[i] != y[i])
				{
					return false;
				}
			}
			return true;
		}

		public int GetHashCode(List<Component> obj)
		{
			int res = 0x2D2816FE;
			foreach (var item in obj)
			{
				res = res * 31 + (item == null ? 0 : item.GetHashCode());
			}
			return res;
		}
	}

	public partial class Blueprint
	{
		Dictionary<List<Component>, ComponentState[]> Memory = new Dictionary<List<Component>, ComponentState[]>(new ListEquityComparer());

		public event EventHandler<EventArgs> CircutResolved;

		private static void RepeatAction(int repeatCount, Action action)
		{
			for (int i = 0; i < repeatCount; i++)
				action();
		}

		public ComponentState[] GetComponentOutputs(Component component)
		{
			return Memory[CopyList(new List<Component>(), component)];
		}

		private List<Component> CopyList(List<Component> list, Component ToAdd)
		{
			List<Component> temp = list.ToArray().ToList();
			temp.Add(ToAdd);
			return temp;
		}

		public ComponentState[] ResolveOutputs()
		{
			return ResolveOutputs(this.Memory, this.getInputStates(), new List<Component>());
		}

		// pass null if we are working with the MASTER component
		public ComponentState[] ResolveOutputs(Dictionary<List<Component>, ComponentState[]> memoryArgument, ComponentState[] inputs, List<Component> MasterChain)
		{
			if (MasterChain.Count == 0)
			{
				Analytics.Reset();
			}

			Analytics.Record("-");

			Dictionary<Component, int> targetNumberInputs = new Dictionary<Component, int>();
			Dictionary<Component, int> actualNumberInputs = new Dictionary<Component, int>();

			Dictionary<Component, ComponentState[]> inputStates = new Dictionary<Component, ComponentState[]>();
			var outputStates = memoryArgument;

			// gates that have been fully resolved and whose children need to be defined
			Queue<Component> toResolveQueue = new Queue<Component>();
			List<Component> fullyResolved = new List<Component>();

			Analytics.Record("declare variables");

			foreach (Component component in this.GetComponentList())
			{
				// set all input states to floating
				List<ComponentState> tempInputStates = new List<ComponentState>();
				RepeatAction(component.getNumberInputs(), () => tempInputStates.Add(ComponentState.Float));
				inputStates.Add(component, tempInputStates.ToArray());

				// if a component is not remembered, set its outputs to float
				List<Component> componentKey = CopyList(MasterChain, component);

				if (!outputStates.ContainsKey(componentKey))
				{
					List<ComponentState> tempOutputStates = new List<ComponentState>();
					RepeatAction(component.getNumberOutputs(), () => tempOutputStates.Add(ComponentState.Float));
					outputStates.Add(componentKey, tempOutputStates.ToArray());
				}

				// set the number references that we need
				int numberReferences = 0;
				foreach (ComponentReference reference in component.getInputs())
				{
					if (reference != null)
					{
						numberReferences++;
					}
				}
				targetNumberInputs[component] = numberReferences;

				// all components have no resolved inputs yet
				actualNumberInputs[component] = 0;
			}

			Analytics.Record("set reference targets");

			Component[] inputComponents = this.getInputComponents();

			// set all of the input components' output states
			for (int i = 0; i < inputComponents.Length; i++)
			{
				var key = CopyList(MasterChain, inputComponents[i]);

				outputStates[key] = new ComponentState[] { inputs[i] };
				toResolveQueue.Enqueue(inputComponents[i]);
			}

			Analytics.Record("set inputs");

			// repeat while there are still element outputs to propagate
			while (toResolveQueue.Count != 0)
			{
				Component parentComponent = toResolveQueue.Dequeue();

				// cycle over each output index
				for (int outputIndex = 0; outputIndex < parentComponent.getNumberOutputs(); outputIndex++)
				{
					Analytics.Record("-");
					var children = parentComponent.getChildren(outputIndex);
					Analytics.Record("get children call");
					       
					// for a given output index, find all nodes that reference it
					foreach (ComponentReference child in children)
					{
						Analytics.Record("-");

						Component childComponent = this.GetComponentById(child.getId());
						// set the input of the child component to the output of the parent component
						inputStates[childComponent][child.getIndex()] = outputStates[CopyList(MasterChain, parentComponent)][outputIndex];

						actualNumberInputs[childComponent]++; //child component has one more reference now

						Analytics.Record("increment input count");

						// if we have set all of the references, we are forced to resolve the component
						if (actualNumberInputs[childComponent] == targetNumberInputs[childComponent])
						{
							
							// if the component is a gate, resolve it properly
							if (childComponent.getType() == ComponentType.Gate)
							{
								Gate childGate = (Gate)childComponent;

								// pass the master chain to the 
								ComponentState[] resolvedOutputStates = childGate.Function(inputStates[childComponent], outputStates, CopyList(MasterChain, childComponent));

								Analytics.Record("-");
								outputStates[CopyList(MasterChain, childComponent)] = resolvedOutputStates;

								if (!fullyResolved.Contains(childComponent))
								{
									// check this component soon
									toResolveQueue.Enqueue(childComponent);
									fullyResolved.Add(childComponent);
								}
								Analytics.Record("enqueue complete");
							}
							else if (childComponent.getType() == ComponentType.Output)
							{
								outputStates[CopyList(MasterChain, childComponent)] = inputStates[childComponent];
							}
						}
						// if the component resolves fully without all of its references, do this
						else
						{
							if (childComponent.getType() == ComponentType.Gate)
							{
								Gate childGate = (Gate)childComponent;
								ComponentState[] resolvedOutputStates = childGate.Function(inputStates[childComponent], outputStates, CopyList(MasterChain, childComponent));

								Analytics.Record("-");
								// if there are no floating states in the resolved function, and if the component's children have not been resolved yet
								if (!resolvedOutputStates.Contains(ComponentState.Float) && !fullyResolved.Contains(childComponent))
								{                                    
									outputStates[CopyList(MasterChain, childComponent)] = resolvedOutputStates;

									toResolveQueue.Enqueue(childComponent);
									fullyResolved.Add(childComponent);
								}
								Analytics.Record("enqueue incomplete");
							}
						}
					}
				}
			}

			Analytics.Record("-");

			// ALL COMPONENTS RESOLVED AT THIS POINT

			// consolidate the values of all outputs into an array
			List<ComponentState> returnStates = new List<ComponentState>();
			foreach (Component output in this.getOutputComponents())
			{
				returnStates.Add(outputStates[CopyList(MasterChain, output)][0]);
			}

			Analytics.Record("setup return");

			// if we are on the top level, print
			if (MasterChain.Count == 0)
			{
				Analytics.Print();
			}

			Console.WriteLine(this.GetName() + ": " + this.ContainsCycles().ToString());

			return returnStates.ToArray();
		}
	}
}
