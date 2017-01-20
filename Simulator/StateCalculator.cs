using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
	public partial class ComponentCollection
	{
		Dictionary<Component, Dictionary<Component, ComponentState[]>> memory = new Dictionary<Component, Dictionary<Component, ComponentState[]>>();
		private Dictionary<Component, ComponentState[]> masterMemory = new Dictionary<Component, ComponentState[]>();
		public event EventHandler<EventArgs> CircutResolved;

		public ComponentCollection addDependentGate(Component encapsulatingComponent)
		{
			// add a new dictionary to keep track of gate values
			memory.Add(encapsulatingComponent, new Dictionary<Component, ComponentState[]>());

			return this;
		}

		private static void RepeatAction(int repeatCount, Action action)
		{
			for (int i = 0; i < repeatCount; i++)
				action();
		}

		public ComponentState[] ResolveOutputs(Component encapsulatingComponent, ComponentState[] inputs)
		{
			return this.ResolveOutputs(memory[encapsulatingComponent], inputs);
		}

		public ComponentState GetMasterState(Component component, int index)
		{
			if (this.masterMemory.ContainsKey(component))
			{
				return this.masterMemory[component][index];
			}

			return ComponentState.Float;
		}

		public void ResolveMasterOutputs(ComponentState[] inputs)
		{
			this.ResolveOutputs(masterMemory, inputs);

			// set all outputs on the master so that they will display
			foreach (Component component in this.getItems())
			{
				component.setOutputs(masterMemory[component]);
			}

			// note that the circut has been resolved when
			if (CircutResolved != null)
			{
				CircutResolved(this, EventArgs.Empty);
			}
		}
	
		// pass null if we are working with the MASTER component
		public ComponentState[] ResolveOutputs(Dictionary<Component, ComponentState[]> outputStatesArgument, ComponentState[] inputs)
		{
			Dictionary<Component, int> targetNumberInputs = new Dictionary<Component, int>();
			Dictionary<Component, int> actualNumberInputs = new Dictionary<Component, int>();

			Dictionary<Component, ComponentState[]> inputStates = new Dictionary<Component, ComponentState[]>();
			Dictionary<Component, ComponentState[]> outputStates = outputStatesArgument;

			// gates that have been fully resolved and whose children need to be defined
			Queue<Component> toResolveQueue = new Queue<Component>();
			List<Component> fullyResolved = new List<Component>();

			foreach (Component component in this.getItems())
			{
				// set all input states to floating
				List<ComponentState> tempInputStates = new List<ComponentState>();
				RepeatAction(component.getNumberInputs(), () => tempInputStates.Add(ComponentState.Float));
				inputStates.Add(component, tempInputStates.ToArray());

				// if a component is not remembered, set its outputs to float
				if (!outputStates.ContainsKey(component))
				{
					List<ComponentState> tempOutputStates = new List<ComponentState>();
					RepeatAction(component.getNumberOutputs(), () => tempOutputStates.Add(ComponentState.Float));
					outputStates.Add(component, tempOutputStates.ToArray());
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

			Component[] inputComponents = this.getInputComponents();

			// set all of the input components' output states
			for (int i = 0; i < inputComponents.Length; i++)
			{
				outputStates[inputComponents[i]] = new ComponentState[] { inputs[i] };
				toResolveQueue.Enqueue(inputComponents[i]);
			}

			// repeat while there are still element outputs to propagate
			while (toResolveQueue.Count != 0)
			{
				Component parentComponent = toResolveQueue.Dequeue();

				// cycle over each output index
				for (int outputIndex = 0; outputIndex < parentComponent.getNumberOutputs(); outputIndex++)
				{
					// for a given output index, find all nodes that reference it
					foreach (ComponentReference child in parentComponent.getChildren(outputIndex))
					{
						Component childComponent = this.getComponentById(child.getId());
						// set the input of the child component to the output of the parent component
						inputStates[childComponent][child.getIndex()] = outputStates[parentComponent][outputIndex];

						actualNumberInputs[childComponent]++; //child component has one more reference now

						// if we have set all of the references, we are forced to resolve the component
						if (actualNumberInputs[childComponent] == targetNumberInputs[childComponent])
						{
							// if the component is a gate, resolve it properly
							if (childComponent.getType() == ComponentType.Gate)
							{
								Gate childGate = (Gate)childComponent;

								ComponentState[] resolvedOutputStates = childGate.Function(inputStates[childComponent]);
								outputStates[childComponent] = resolvedOutputStates;

								if (!fullyResolved.Contains(childComponent))
								{
									// check this component soon
									toResolveQueue.Enqueue(childComponent);
									fullyResolved.Add(childComponent);
								}
							}
							else if (childComponent.getType() == ComponentType.Output)
							{
								outputStates[childComponent] = inputStates[childComponent];
							}
						}
						// if the component resolves fully without all of its references, do this
						else
						{
							if (childComponent.getType() == ComponentType.Gate)
							{
								Gate childGate = (Gate)childComponent;
								ComponentState[] resolvedOutputStates = childGate.Function(inputStates[childComponent]);

								if (!resolvedOutputStates.Contains(ComponentState.Float) && !fullyResolved.Contains(childComponent))
								{
									outputStates[childComponent] = resolvedOutputStates;

									toResolveQueue.Enqueue(childComponent);
									fullyResolved.Add(childComponent);
								}
							}
						}
					}
				}
			}

			// ALL COMPONENTS RESOLVED AT THIS POINT

			// consolidate the values of all outputs into an array
			List<ComponentState> returnStates = new List<ComponentState>();
			foreach (Component output in this.getOutputComponents())
			{
				returnStates.Add(outputStates[output][0]);
			}

			return returnStates.ToArray();
		}

		public bool IsNodeInCycle(Component node, int inputToCheck)
		{
			Queue<Component> toSearch = new Queue<Component>();
			List<Component> searched = new List<Component>();

			//add the first element
			Component firstChild = this.getComponentById(node.getInputs()[inputToCheck].getId());
			toSearch.Enqueue(firstChild);

			while (toSearch.Count != 0)
			{
				// dequeue a node, note that we have searched it
				Component currentNode = toSearch.Dequeue();
				searched.Add(currentNode);

				// get current node's children
				List<Component> children = currentNode.getInputs().Select((input) => this.getComponentById(input.getId())).ToList();

				// if we have arrived back at the master node, return true
				if (children.Contains(node) || currentNode == node)
				{
					return true;
				}

				// find all children that haven't already been searched
				List<Component> unsearchedChildren = children.Where((arg) => !searched.Contains(arg)).ToList();

				// add all unsearched children to the queue
				unsearchedChildren.ForEach((obj) => toSearch.Enqueue(obj));
			}

			// if there are no more items in the queue
			return false;
		}
	}
}
