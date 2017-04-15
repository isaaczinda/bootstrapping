using System;
using Gtk;


namespace Engine
{
	// different states that a component can output
	public enum ProgramState { None, DraggingGate, ReferencingFromInput, ReferencingFromOutput, SelectedGate, Panning, DraggingBuffer, SelectedBuffer};

	public static class UserInterface
	{
		private static uint previousEventTime = 0;

		// has to do with state management
		private static ProgramState CurrentState = ProgramState.None;

		// has to do with gate dragging
		public static Component DraggedComponent = null;
		public static Coord ClickLocation = new Coord(0, 0);

		// has to do with buffer dragging
		public static Buffer DraggedBuffer = null;

		// has to do with adding and deleting references
		private static ComponentReference SavedComponentReference = null;

		public static Coord mousePosition = new Coord(0, 0);

		public static Component GetSavedComponent()
		{
			return BlueprintLibrary.GetActiveCollection().GetComponentById(UserInterface.SavedComponentReference.getId());
		}

		public static ComponentReference GetSavedComponentReference()
		{
			return UserInterface.SavedComponentReference;
		}

		public static ProgramState GetCurrentState()
		{
			return UserInterface.CurrentState;
		}

		public static bool IsCurrentState(ProgramState ToCheck)
		{
			return ToCheck == UserInterface.GetCurrentState();
		}

		public static void SetCurrentState(ProgramState ToSet)
		{
			UserInterface.CurrentState = ToSet;
		}

		public static void KeyPress(Gtk.KeyPressEventArgs e)
		{
			// when control d is pressed, delete the reference
			if (Keyboard.IsControlPressed() && e.Event.Key == Gdk.Key.d)
			{
				UserInterface.deleteCurrentReference();
			}

			if (Keyboard.IsControlPressed() && e.Event.Key == Gdk.Key.s)
			{
				// save the current collection
				Storage.SaveProject();

				Console.WriteLine("saved.");
			}

			if (Keyboard.IsControlPressed() && e.Event.Key == Gdk.Key.q)
			{
				ConsoleInput.Stop();
				Gtk.Application.Quit();
				Environment.Exit(0);
			}
		}

		public static void MouseButtonPress(ButtonPressEventArgs args)
		{
			previousEventTime = args.Event.Time;

			switch (args.Event.Button)
			{
				case 1:
					UserInterface.leftClick();
					break;
				case 3:
					UserInterface.rightClick();
					break;
			}
		}

		public static void MouseMove(MotionNotifyEventArgs args)
		{
			UserInterface.mousePosition = new Coord(args.Event.X, args.Event.Y);

			switch (UserInterface.CurrentState)
			{
				case ProgramState.DraggingGate:
					UserInterface.DraggedComponent.Move(UserInterface.mousePosition - UserInterface.ClickLocation);
					UserInterface.ClickLocation = mousePosition;
					break;
				case ProgramState.DraggingBuffer:
					UserInterface.DraggedBuffer.Move(UserInterface.mousePosition - UserInterface.ClickLocation);
					UserInterface.ClickLocation = mousePosition;
					break;
				case ProgramState.Panning:
					Blueprint activeCollection = BlueprintLibrary.GetActiveCollection();

					activeCollection.Move(UserInterface.mousePosition - UserInterface.ClickLocation);

					UserInterface.ClickLocation = mousePosition;
					break;
			}
		}

		public static void MouseButtonRelease(ButtonReleaseEventArgs args)
		{
			switch (UserInterface.GetCurrentState())
			{
				case ProgramState.DraggingGate:
					UserInterface.SetCurrentState(ProgramState.None);
					break;
				case ProgramState.DraggingBuffer:
					UserInterface.SetCurrentState(ProgramState.None);
					break;
				case ProgramState.Panning:
					UserInterface.SetCurrentState(ProgramState.None);
					break;
			}
		}

		private static void deleteCurrentReference()
		{
			Blueprint collection = BlueprintLibrary.GetActiveCollection();

			switch (UserInterface.CurrentState)
			{
				case ProgramState.ReferencingFromInput:
					// lookup the component that was references
					Component StoredComponent = collection.GetComponentById(UserInterface.SavedComponentReference.getId());
					// set then input to the component that was referenced
					StoredComponent.SetInput(null, UserInterface.SavedComponentReference.getIndex());
					UserInterface.CurrentState = ProgramState.None;
					UserInterface.CircutChanged();
					break;
				case ProgramState.ReferencingFromOutput:
					Component component = collection.GetComponentById(SavedComponentReference.getId());
					component.removeReferencesToOutput(SavedComponentReference.getIndex());
					UserInterface.CircutChanged();
					break;
				case ProgramState.SelectedGate:
					Component toDelete = collection.GetComponentById(UserInterface.SavedComponentReference.getId());
					BlueprintLibrary.GetActiveCollection().Delete(toDelete);
					UserInterface.CurrentState = ProgramState.None;
					UserInterface.CircutChanged();
					break;
				case ProgramState.SelectedBuffer:
					Buffer bufferToDelete = collection.Buffers.GetBufferById(UserInterface.SavedComponentReference.getId());
					bufferToDelete.GetParent().Delete(bufferToDelete);
					UserInterface.CurrentState = ProgramState.None;
					UserInterface.CircutChanged();
					break;
				default:
					UserInterface.invalidAction();
					break;
			}
		}

		private static void invalidAction()
		{
			//Console.WriteLine("you can't click that.");
		}

		public static void CircutChanged()
		{
			BlueprintLibrary.GetActiveCollection().ResolveOutputs();
		}

		private static void leftClick()
		{
			Blueprint collection = BlueprintLibrary.GetActiveCollection();

			// logic to check button box clicks
			foreach (Component component in collection.GetComponentList())
			{
				if (component.getButtonBox() != null && component.getType() == ComponentType.Input)
				{
					if (mousePosition.isInside(component.getButtonBox().Add(component.getPosition())) && UserInterface.CurrentState == ProgramState.None)
					{
						// if the clock is clicked, toggle whether or not it is running
						if (component.getName() == "clock")
						{
							((Clock)component).ToggleClockState();
							return;
						}
				
						// if the component is not a clock
						((Input)component).Toggle();
						return;
					}
				}

				// look for collisions with input bounding boxes
				for (int i = 0; i < component.getInputBoundingBoxes().Count; i++)
				{
					if (mousePosition.isInside(component.getInputBoundingBoxes()[i].Add(component.getPosition())))
					{
						switch (UserInterface.CurrentState)
						{
							case ProgramState.None:
								UserInterface.SavedComponentReference = new ComponentReference(component, i);
								UserInterface.SetCurrentState(ProgramState.ReferencingFromInput);
								return;
							case ProgramState.ReferencingFromOutput: 
							case ProgramState.SelectedBuffer:
								component.SetInput(UserInterface.SavedComponentReference, i);
								UserInterface.CircutChanged();
								UserInterface.CurrentState = ProgramState.None;
								return;
							default:
								UserInterface.invalidAction();
								UserInterface.CurrentState = ProgramState.None;
								return;
						}
					}
				}

				// look for collisions with output bounding boxes
				for (int i = 0; i < component.getOutputBoundingBoxes().Count; i++)
				{
					if (mousePosition.isInside(component.getOutputBoundingBoxes()[i].Add(component.getPosition())))
					{
						switch (UserInterface.CurrentState)
						{
							case ProgramState.None:
								UserInterface.CurrentState = ProgramState.ReferencingFromOutput;
								// store the output location that we have just clicked on
								UserInterface.SavedComponentReference = new ComponentReference(component, i);
								return;
							case ProgramState.ReferencingFromInput:
								// get the component that was previously clicked
								Component StoredComponent = BlueprintLibrary.GetActiveCollection().GetComponentById(UserInterface.SavedComponentReference.getId());

								// add reference from current spot to output
								if (StoredComponent.getType() == ComponentType.Output || StoredComponent.getType() == ComponentType.Gate)
								{
									StoredComponent.SetInput(new ComponentReference(component, i), UserInterface.SavedComponentReference.getIndex());
									UserInterface.CircutChanged();
								}
								else
								{
									UserInterface.invalidAction();
								}

								UserInterface.CurrentState = ProgramState.None;
								return;
							case ProgramState.SelectedBuffer:
								Buffer selectedBuffer = collection.Buffers.GetBufferById(UserInterface.SavedComponentReference.getId());

								if (selectedBuffer.GetGroupComponentReference() == null)
								{
									selectedBuffer.AddReference(new ComponentReference(component, i));
									UserInterface.CircutChanged();
								}
								else
								{
									invalidAction();
								}

								UserInterface.CurrentState = ProgramState.None;
								return;
							default:
								UserInterface.invalidAction();
								UserInterface.CurrentState = ProgramState.None;
								return;
						}
					}
				}
			}

			// checks to see if we are clicking on the gate
			foreach (Component component in collection.GetComponentList())
			{
				if (mousePosition.isInside(new BoundingBox(component.getPosition(), component.getPosition() + component.getDimensions())))
				{
					if (UserInterface.CurrentState == ProgramState.None)
					{
						UserInterface.CurrentState = ProgramState.SelectedGate;
						UserInterface.SavedComponentReference = new ComponentReference(component, -1);
						return;
					}
				}
			}

			// checks to see if buffer was clicked
			foreach (Buffer buffer in collection.Buffers.GetItems())
			{
				if (UserInterface.CurrentState == ProgramState.None && mousePosition.isInside(buffer.getBoundingBox()))
				{
					UserInterface.CurrentState = ProgramState.SelectedBuffer;
					UserInterface.SavedComponentReference = new ComponentReference(buffer);
					return;
				}

				// if a buffer is selected and another one is clicked
				if (UserInterface.CurrentState == ProgramState.SelectedBuffer && mousePosition.isInside(buffer.getBoundingBox()))
				{
					Buffer selectedBuffer = collection.Buffers.GetBufferById(UserInterface.SavedComponentReference.getId());
					selectedBuffer.AddReference(new ComponentReference(buffer));
					UserInterface.CurrentState = ProgramState.None;
				}
			}

			// if a buffer was selected and we have clicked into space, create a new one
			if (UserInterface.GetCurrentState() == ProgramState.SelectedBuffer)
			{
				Buffer temp = collection.Buffers.New(mousePosition - collection.GetPosition());
				Buffer selectedBuffer = collection.Buffers.GetBufferById(UserInterface.SavedComponentReference.getId());
				temp.AddReference(new ComponentReference(selectedBuffer));

				UserInterface.CurrentState = ProgramState.None;
				return;
			}

			// if an input was selected and we clicked into space, create a new buffer
			if (UserInterface.GetCurrentState() == ProgramState.ReferencingFromInput)
			{
				Buffer temp = collection.Buffers.New(mousePosition - collection.GetPosition());
				Component component = collection.GetComponentById(UserInterface.SavedComponentReference.getId());

				component.SetInput(new ComponentReference(temp), UserInterface.SavedComponentReference.getIndex());

				UserInterface.CircutChanged();
				UserInterface.CurrentState = ProgramState.None;
				return;
			}

			// if an output was selected and we clicked into space, create a new buffer
			if (UserInterface.GetCurrentState() == ProgramState.ReferencingFromOutput)
			{
				Buffer temp = collection.Buffers.New(mousePosition - collection.GetPosition());
				Component component = collection.GetComponentById(UserInterface.SavedComponentReference.getId());
				int componentIndex = UserInterface.SavedComponentReference.getIndex();

				temp.AddReference(new ComponentReference(component, componentIndex));

				UserInterface.CircutChanged();
				UserInterface.CurrentState = ProgramState.None;
				return;
			}

			// if nothing was clicked, we will reset the state
			UserInterface.CurrentState = ProgramState.None;
		}

		private static void rightClick()
		{
			Blueprint collection = BlueprintLibrary.GetActiveCollection();

			// logic to move any component
			foreach (Component component in collection.GetComponentList())
			{
				if (mousePosition.isInside(new BoundingBox(component.getPosition(), component.getPosition() + component.getDimensions())))
				{
					UserInterface.CurrentState = ProgramState.DraggingGate;
					UserInterface.DraggedComponent = component;
					UserInterface.ClickLocation = UserInterface.mousePosition;
					return;
				}
			}

			// logic to move buffers
			foreach (Buffer buffer in collection.Buffers.GetItems())
			{
				if (mousePosition.isInside(buffer.getBoundingBox()))
				{
					UserInterface.CurrentState = ProgramState.DraggingBuffer;
					UserInterface.DraggedBuffer = buffer;
					UserInterface.ClickLocation = UserInterface.mousePosition;
					return;
				}
			}

			// if nothing was clicked, we are panning
			UserInterface.CurrentState = ProgramState.Panning;
			UserInterface.ClickLocation = mousePosition;
		}
	}
}