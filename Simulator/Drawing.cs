using System;
using Gtk;
using Gdk;
using System.Collections.Generic;

using Cairo;

namespace Engine
{
	public static class Drawing
	{
		private static Context context;
		private static Dictionary<ComponentState, Cairo.Color> StateColorMap = new Dictionary<ComponentState, Cairo.Color>();
		public static Cairo.Color SELECTED_OVERLAY_COLOR = new Cairo.Color(0, 0, 0, .2);

		private static void SetupStateColorMap()
		{
			StateColorMap.Add(ComponentState.False, new Cairo.Color(0, .3, 0));
			StateColorMap.Add(ComponentState.True, new Cairo.Color(0, .8, 0));
			StateColorMap.Add(ComponentState.Float, new Cairo.Color(.5, .5, .5));
		}

		public static Cairo.Color ColorFromState(ComponentState state)
		{
			if (StateColorMap.Count == 0)
			{
				Drawing.SetupStateColorMap();
			}

			return Drawing.StateColorMap[state];
		}

		private static void drawSquare(Cairo.Color fill, BoundingBox box)
		{
			Drawing.context.MoveTo(box.UpperLeft.x, box.UpperLeft.y);
			Drawing.context.LineTo(box.LowerRight.x, box.UpperLeft.y);
			Drawing.context.LineTo(box.LowerRight.x, box.LowerRight.y);
			Drawing.context.LineTo(box.UpperLeft.x, box.LowerRight.y);
			Drawing.context.ClosePath();

			Drawing.context.SetSourceColor(fill);
			Drawing.context.FillPreserve();
			Drawing.context.Stroke();
		}

		private static void drawLine(Coord FirstPoint, Coord SecondPoint, Cairo.Color color)
		{
			Drawing.context.SetSourceColor(color);
			Drawing.context.MoveTo(FirstPoint.x, FirstPoint.y);
			Drawing.context.LineTo(SecondPoint.x, SecondPoint.y);

			Drawing.context.Stroke();
		}

		private static void drawComponent(Component component)
		{
			Blueprint main = BlueprintLibrary.GetActiveCollection();

			//draw background
			Coord upperLeft = component.getPosition();
			Coord lowerRight = new Coord(component.getPosition().x + component.getDimensions().x, component.getPosition().y + component.getDimensions().y);
			BoundingBox toFill = new BoundingBox(upperLeft, lowerRight);

			// draw the component outline
			Drawing.drawSquare(new Cairo.Color(.3, .7, .7), toFill);

			// draw name of component
			context.SetSourceColor(new Cairo.Color(0, 0, 0));
			context.SetFontSize(Component.FONT_SIZE);
			context.MoveTo(upperLeft.x, lowerRight.y + Component.FONT_SIZE);
			context.ShowText(component.getName());

			// draw bounding boxes
			foreach (BoundingBox boundingBox in component.getBoundingBoxes())
			{
				Drawing.drawSquare(new Cairo.Color(1, 1, 0), boundingBox.Add(component.getPosition())); //these positions are relative
			}


			// put an overlay on 'selected' bounding boxes
			BoundingBox box = null;
			Component selectedComponent = null;

			switch (UserInterface.GetCurrentState())
			{
				case ProgramState.ReferencingFromInput:
					selectedComponent = UserInterface.GetSavedComponent();
					box = selectedComponent.getInputBoundingBoxes()[UserInterface.GetSavedComponentReference().getIndex()].Add(selectedComponent.getPosition());
					break;
				case ProgramState.ReferencingFromOutput:
					selectedComponent = UserInterface.GetSavedComponent();
					box = selectedComponent.getOutputBoundingBoxes()[UserInterface.GetSavedComponentReference().getIndex()].Add(selectedComponent.getPosition());
					break;
				case ProgramState.SelectedGate:
					selectedComponent = UserInterface.GetSavedComponent();
					box = new BoundingBox(selectedComponent.getPosition(), selectedComponent.getPosition() + selectedComponent.getDimensions());
					break;

			}

			// if bounding box was written to, draw the overlay to the screen
			if (box != null)
			{
				Drawing.drawSquare(SELECTED_OVERLAY_COLOR, box);
			}

			//draw button box (if it exists)
			if (component.getButtonBox() != null)
			{
				BoundingBox button = component.getButtonBox().Add(component.getPosition());
				Cairo.Color color;

				if (component.getName() == "clock")
				{
					color = Drawing.ColorFromState((ComponentState)Convert.ToInt32(((Clock)component).IsClockRunning()));
				}
				else
				{
					color = Drawing.ColorFromState(main.GetComponentOutputs(component)[0]);
				}

				Drawing.drawSquare(color, button);
			}
		}

		public static void Update(Context context)
		{
			Drawing.context = context;

			Blueprint main = BlueprintLibrary.GetActiveCollection();

			// DRAW BUFFERS
			foreach (Buffer buffer in main.Buffers.GetItems())
			{
				Coord bufferPosition = buffer.getPosition();
				BoundingBox box = new BoundingBox(bufferPosition, bufferPosition + new Coord(Buffer.HEIGHT, Buffer.WIDTH));

				Drawing.drawSquare(new Cairo.Color(.7, .7, .7), box);

				// if the current buffer is selected
				if (UserInterface.GetCurrentState() == ProgramState.SelectedBuffer)
				{
					if (UserInterface.GetSavedComponentReference().Equals(new ComponentReference(buffer)))
					{
						Drawing.drawSquare(SELECTED_OVERLAY_COLOR, buffer.getBoundingBox());
					}
				}
			}

			// DRAW COMPONENTS
			foreach (Component component in main.getItems())
			{
				// draw the base component
				Drawing.drawComponent(component);
			}

			// DRAW BUFFER REFERENCES
			foreach (Buffer buffer in main.Buffers.GetItems())
			{
				// calculate the color that the buffer reference will be
				ComponentState bufferState = ComponentState.Float;
				// if the buffers reference an external component
				if (buffer.GetGroupComponentReference() != null)
				{
					Component temp = main.getComponentById(buffer.GetGroupComponentReference().getId());
					// THIS MAY BE WRONG
					bufferState = main.GetComponentOutputs(temp)[buffer.GetGroupComponentReference().getIndex()];
				}
				Cairo.Color referenceColor = Drawing.ColorFromState(bufferState);

				// buffer references to eachother
				foreach (ComponentReference reference in buffer.GetBufferReferences())
				{
					Buffer referencedBuffer = main.Buffers.GetBufferById(reference.getId());

					// draw a line from the current buffer to referenced buffer
					Drawing.drawLine(buffer.getBoundingBox().GetCenter(), referencedBuffer.getBoundingBox().GetCenter(), referenceColor);
				}

				// draw line to buffer referencing a real component
				if (buffer.GetComponentReference() != null)
				{
					ComponentReference componentReference = buffer.GetComponentReference();
					Component component = BlueprintLibrary.GetActiveCollection().getComponentById(componentReference.getId());

					Coord target = component.getOutputBoundingBoxes()[componentReference.getIndex()].GetCenter() + component.getPosition();
					Coord source = buffer.getBoundingBox().GetCenter();

					Drawing.drawLine(target, source, referenceColor);
				}
			}

			// DRAW COMPONENT REFERENCES
			foreach (Component component in main.getItems())
			{
				// only check components that can hold references
				if (component.getType() == ComponentType.Gate || component.getType() == ComponentType.Output)
				{
					for (int i = 0; i < component.getNumberInputs(); i++)
					{
						ComponentReference reference = component.getInputs()[i];
						ComponentState componentReferenceState = ComponentState.Float;


						if (reference != null)
						{
							Coord source = component.getInputBoundingBoxes()[i].UpperLeft + component.getPosition() + new Coord(Component.REFERENCE_WIDTH / 2f, Component.REFERENCE_HEIGHT / 2f);
							Coord target;


							if (reference.IsBufferReference())
							{
								Buffer buffer = main.Buffers.GetBufferById(reference.getId());
								target = buffer.getBoundingBox().GetCenter();

								// if the group is attached to some real component
								if (buffer.GetGroupComponentReference() != null)
								{
									Component temp = main.getComponentById(buffer.GetGroupComponentReference().getId());
									componentReferenceState = main.GetComponentOutputs(temp)[buffer.GetGroupComponentReference().getIndex()];
								}
							}
							else
							{
								Component targetComponent = main.getComponentById(reference.getId());
								target = targetComponent.getOutputBoundingBoxes()[reference.getIndex()].UpperLeft + targetComponent.getPosition() + new Coord(Component.REFERENCE_WIDTH / 2f, Component.REFERENCE_HEIGHT / 2f);

								componentReferenceState = main.GetComponentOutputs(targetComponent)[reference.getIndex()];
							}

							Drawing.drawLine(target, source, Drawing.ColorFromState(componentReferenceState));
						}
					}
				}
			}

			context.Dispose();
		}
	}
}