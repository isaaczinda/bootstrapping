using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Engine
{
	[JsonObject(MemberSerialization.OptIn)]
	public class BufferCollection
	{
		[JsonProperty]
		private List<Buffer> Items = new List<Buffer>();
		private ComponentCollection Parent;

		public BufferCollection(ComponentCollection Parent)
		{
			this.Parent = Parent;
		}

		public ComponentCollection GetParent()
		{
			return this.Parent;
		}

		public List<Buffer> GetItems()
		{
			return this.Items;
		}

		private int getNextAvailableId()
		{
			List<int> ids = (from item in this.Items select item.getId()).ToList();

			if (ids.Count == 0)
			{
				return 0;
			}

			return ids.Max() + 1;
		}

		public Buffer New(Coord Position)
		{
			Buffer tempBuffer = new Buffer(Position, getNextAvailableId(), this);
			this.Items.Add(tempBuffer);
			return tempBuffer;
		}

		public Buffer New(Coord Position, int id, List<ComponentReference> references)
		{
			Buffer tempBuffer = new Buffer(Position, id, this);
			tempBuffer.SetReferences(references);
			this.Items.Add(tempBuffer);
			return tempBuffer;
		}

		public void Delete(Buffer toDelete)
		{
			// remove from master list
			this.Items.Remove(toDelete);

			// remove buffer references to other buffers
			foreach (ComponentReference reference in toDelete.GetReferences())
			{
				// remove all references to it from other buffers
				if (reference.IsBufferReference())
				{
					this.GetBufferById(reference.getId()).RemoveReference(new ComponentReference(toDelete));
				}
			}

			// remove component references to buffers
			foreach (Component component in this.Parent.getItems())
			{
				for (int index = 0; index < component.getNumberInputs(); index++)
				{
					// if the component references a buffer
					if (component.getInputs()[index] != null)
					{
						if (component.getInputs()[index].IsBufferReference())
						{
							// if the component references OUR buffer
							if (component.getInputs()[index].getId() == toDelete.getId())
							{
								// delete the reference
								component.getInputs()[index] = null;
							}
						}
					}
				}
			}
		}

		public Buffer GetBufferById(int Id)
		{
			foreach (Buffer buffer in this.Items)
			{
				if (buffer.getId() == Id)
				{
					return buffer;
				}
			}

			throw new Exception("a buffer with the given id could not be found.");
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class Buffer
	{
		// specific parameters for drawing
		public const float WIDTH = 15;
		public const float HEIGHT = 15;

		[JsonProperty]
		private Coord Position;
		[JsonProperty]
		private int Id;
		private BufferCollection Parent;
		[JsonProperty]
		private List<ComponentReference> References = new List<ComponentReference>();

		public Buffer(Coord Position, int Id, BufferCollection Parent)
		{
			this.Position = Position;
			this.Id = Id;
			this.Parent = Parent;
		}

		public void SetReferences(List<ComponentReference> References)
		{
			this.References = References;
		}

		public BufferCollection GetParent()
		{
			return this.Parent;
		}

		public List<ComponentReference> GetBufferReferences()
		{
			// find components that only reference other buffers
			return this.References.Where(arg => arg.IsBufferReference()).ToList();
		}

		public ComponentReference GetComponentReference()
		{
			List<ComponentReference> references = this.References.Where(arg => !arg.IsBufferReference()).ToList();

			if (references.Count == 0)
			{
				return null;
			}

			return references[0];
		}

		public List<Buffer> GetConnected()
		{
			List<Buffer> Checked = new List<Buffer>();
			Queue<Buffer> toCheck = new Queue<Buffer>();

			toCheck.Enqueue(this);

			while (toCheck.Count != 0)
			{
				Buffer buffer = toCheck.Dequeue();
				Checked.Add(buffer);

				foreach (ComponentReference reference in buffer.GetReferences())
				{
					if (reference.IsBufferReference())
					{
						// search the connected buffer
						Buffer referenced = this.Parent.GetBufferById(reference.getId());
						if (!Checked.Contains(referenced))
						{
							toCheck.Enqueue(referenced);
						}
					}
				}
			}

			return Checked;
		}

		// checks if the buffer is linked to any other buffer that references a real component
		public ComponentReference GetGroupComponentReference()
		{
			List<Buffer> Checked = new List<Buffer>();
			Queue<Buffer> toCheck = new Queue<Buffer>();

			toCheck.Enqueue(this);

			while (toCheck.Count != 0)
			{
				Buffer buffer = toCheck.Dequeue();

				foreach (ComponentReference reference in buffer.GetReferences())
				{
					// if the reference is to a real component
					if (!reference.IsBufferReference())
					{
						return reference;
					}
					else
					{
						Buffer referencedBuffer = Parent.GetBufferById(reference.getId());

						if (!Checked.Contains(referencedBuffer))
						{
							toCheck.Enqueue(referencedBuffer);
							Checked.Add(referencedBuffer);
						}
					}
				}
			}

			// if no references to real components were found, return null
			return null;
		}

		public List<ComponentReference> GetReferences()
		{
			return this.References;
		}

		public void RemoveReference(ComponentReference reference)
		{
			this.References.Remove(reference);
		}

		public void AddReference(ComponentReference reference)
		{
			// add reference to this node
			References.Add(reference);

			// if we are referencing a buffer, these references are bi-diretional
			if (reference.IsBufferReference())
			{
				Buffer referencedBuffer = Parent.GetBufferById(reference.getId());
				referencedBuffer.GetReferences().Add(new ComponentReference(this));
			}
		}

		public int getId()
		{
			return this.Id;
		}

		public void Move(Coord amountToMove)
		{
			this.Position += amountToMove;
		}

		public BoundingBox getBoundingBox()
		{
			return new BoundingBox(this.Position, this.Position + new Coord(Buffer.WIDTH, Buffer.HEIGHT)).Add(this.Parent.GetParent().GetPosition());
		}

		public Coord getPosition()
		{
			return this.Position + this.Parent.GetParent().GetPosition();
		}

	}
}
