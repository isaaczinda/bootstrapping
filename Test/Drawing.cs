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

		private static void drawLine(Coord FirstPoint, Coord SecondPoint)
		{
			Drawing.context.SetSourceColor(new Cairo.Color(0, 0, 0));
			Drawing.context.MoveTo(FirstPoint.x, FirstPoint.y);
			Drawing.context.LineTo(SecondPoint.x, SecondPoint.y);

			Drawing.context.Stroke();
		}

		private static void drawComponent(Component component)
		{
			//draw background
			Coord upperLeft = component.Position;
			Coord lowerRight = new Coord(component.Position.x + component.Dimensions.x, component.Position.y + component.Dimensions.y);
			BoundingBox toFill = new BoundingBox(upperLeft, lowerRight);

			// draw the component outline
			Drawing.drawSquare(new Cairo.Color(.3, .7, .7), toFill);

			// draw name of component
			context.SetSourceColor(new Cairo.Color(0, 0, 0));
			context.SetFontSize(Component.FONT_SIZE);
			context.MoveTo(upperLeft.x, lowerRight.y + Component.FONT_SIZE);
			context.ShowText(component.getName());

			// draw bounding boxes
			foreach (BoundingBox box in component.getBoundingBoxes())
			{
				Drawing.drawSquare(new Cairo.Color(1, 1, 0), box.Add(component.Position)); //these positions are relative
			}

			// put an overlay on 'selected' bounding boxes
			if (this.CurrentState == ProgramState.ReferencingFromInput)
			{
				Component selectedComponent = CollectionManager.GetActiveCollection().getComponentById(this.SavedComponentReference.ComponentId);

				BoundingBox box = selectedComponent.getInputBoundingBoxes()[this.SavedComponentReference.Index];

				Drawing.drawSquare(context, new Cairo.Color(0, 0, 0, .1), box.Add(selectedComponent.Position));
			}
			else if (this.CurrentState == ProgramState.ReferencingFromOutput)
			{
				Component selectedComponent = CollectionManager.GetActiveCollection().getComponentById(this.SavedComponentReference.ComponentId);

				BoundingBox box = selectedComponent.getOutputBoundingBoxes()[this.SavedComponentReference.Index];

				Drawing.drawSquare(new Cairo.Color(0, 0, 0, .1), box.Add(selectedComponent.Position));
			}
			else if (this.CurrentState == ProgramState.SelectedGate)
			{
				Component selectedComponent = CollectionManager.GetActiveCollection().getComponentById(this.SavedComponentReference.ComponentId);

				BoundingBox box = new BoundingBox(selectedComponent.Position, selectedComponent.Position + selectedComponent.Dimensions);

				Drawing.drawSquare(new Cairo.Color(0, 0, 0, .1), box);
			}

			//draw button box (if it exists)
			if (component.getButtonBox() != null)
			{

				BoundingBox button = component.getButtonBox().Add(component.Position);

				Drawing.drawSquare(context, Drawing.ColorLookup[component.getOutputs()[0]], button);
			}
		}

		public static void Update(Context context)
		{
			Drawing.context = context;

			// setup color lookup

			ComponentCollection main = CollectionManager.GetActiveCollection();

			// DRAW GATES
			foreach (Component component in main.getItems())
			{
				// draw the base component
				Drawing.drawComponent(component);
			}

			// DRAW REFERENCES
			foreach (Component component in main.getItems())
			{
				// only check components that can hold references
				if (component.getType() == ComponentType.Gate || component.getType() == ComponentType.Output)
				{
					for (int i = 0; i < component.getNumberInputs(); i++)
					{
						ComponentReference reference = component.getInputs()[i];

						if (reference != null)
						{
							Component targetComponent = main.getComponentById(reference.ComponentId);

							Coord target = targetComponent.getOutputBoundingBoxes()[reference.Index].UpperLeft + targetComponent.Position + new Coord(Component.REFERENCE_WIDTH / 2f, Component.REFERENCE_HEIGHT / 2f);
							Coord source = component.getInputBoundingBoxes()[i].UpperLeft + component.Position + new Coord(Component.REFERENCE_WIDTH / 2f, Component.REFERENCE_HEIGHT / 2f);

							Drawing.drawLine(target, source);
						}
					}
				}
			}
		}

	}
}
