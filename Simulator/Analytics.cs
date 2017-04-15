using System;
using System.Collections.Generic;

namespace Engine
{
	public static class Analytics
	{
		static Dictionary<string, double> Timestamps;
		static DateTime PreviousTimestamp;

		public static void Reset()
		{
			Timestamps = new Dictionary<string, double>();
			PreviousTimestamp = DateTime.Now;
		}

		public static void Record(string Tag)
		{
			DateTime current = DateTime.Now;

			if (!Timestamps.ContainsKey(Tag))
			{
				Timestamps.Add(Tag, 0);
			}

			Timestamps[Tag] += (current - PreviousTimestamp).TotalMilliseconds;
			PreviousTimestamp = current;
		}

		public static void Print()
		{
			foreach (string key in Timestamps.Keys)
			{
				Console.WriteLine(key + ": " + Timestamps[key].ToString());
			}
		}
	}
}


