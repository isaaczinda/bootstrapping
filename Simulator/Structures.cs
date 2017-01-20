using System;
using Newtonsoft.Json;

namespace Engine
{
	// different states that a component can output
	public enum ComponentState { False, True, Float };

	// lists the three different types of component
	public enum ComponentType { Input, Output, Gate };

	public enum BoxType { Input, Output };

	// references a certain output of a given component
	[JsonObject(MemberSerialization.OptIn)]
	public class ComponentReference : IEquatable<ComponentReference>
	{
		[JsonProperty]
		private int Id;
		[JsonProperty]
		private int Index;
		[JsonProperty]
		private bool BufferReference = false;
		[JsonProperty]
		private string CollectionName;

		public bool IsBufferReference()
		{
			return this.BufferReference;
		}

		public int getId()
		{
			return this.Id;
		}

		public int getIndex()
		{
			return this.Index;
		}

		public ComponentReference(Buffer Reference)
		{
			this.BufferReference = true;
			this.Index = -1;
			this.Id = Reference.getId();
			this.CollectionName = Reference.GetParent().GetParent().GetName();
		}

		public ComponentReference(int ReferenceId, BufferCollection Parent)
		{
			this.BufferReference = true;
			this.Index = -1;
			this.Id = ReferenceId;
			this.CollectionName = Parent.GetParent().GetName();
		}

		public ComponentReference(int ReferenceId, String ParentName)
		{
			this.BufferReference = true;
			this.Index = -1;
			this.Id = ReferenceId;
			this.CollectionName = ParentName;
		}

		public ComponentReference(Component Reference, int Index)
		{
			this.Id = Reference.getId();
			this.Index = Index;
			this.CollectionName = null;
		}

		public ComponentReference(int ReferenceId, int Index)
		{
			this.Id = ReferenceId;
			this.Index = Index;
			this.CollectionName = null;
		}

		public bool Equals(ComponentReference reference)
		{
			return (this.getId() == reference.getId() && this.getIndex() == reference.getIndex() && 
			        this.BufferReference == reference.BufferReference && this.CollectionName == reference.CollectionName);
		}
	}

	public class Coord
	{
		public new String ToString()
		{
			return this.x.ToString() + ", " + this.y.ToString();
		}

		public bool isInside(BoundingBox box)
		{
			return (this > box.UpperLeft && this < box.LowerRight);
		}

		public double x; public double y;

		public Coord(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public static Coord operator +(Coord c1, Coord c2)
		{
			return new Coord(c1.x + c2.x, c1.y + c2.y);
		}

		public static Coord operator -(Coord c1, Coord c2)
		{
			return new Coord(c1.x - c2.x, c1.y - c2.y);
		}

		public static bool operator <(Coord c1, Coord c2)
		{
			return c1.x < c2.x && c1.y < c2.y;
		}

		public static bool operator >(Coord c1, Coord c2)
		{
			return c1.x > c2.x && c1.y > c2.y;
		}
	}

	public class BoundingBox
	{
		public Coord UpperLeft;
		public Coord LowerRight;

		public BoundingBox(Coord UpperLeft, Coord LowerRight)
		{
			this.UpperLeft = UpperLeft;
			this.LowerRight = LowerRight;
		}

		public Coord GetCenter()
		{
			return new Coord((this.UpperLeft.x + this.LowerRight.x) / 2, (this.UpperLeft.y + this.LowerRight.y) / 2);
		}

		public BoundingBox Add(Coord AmountToAdd)
		{
			return new BoundingBox(this.UpperLeft + AmountToAdd, this.LowerRight + AmountToAdd);
		}
	}
}
