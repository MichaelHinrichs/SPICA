using SPICA.PICA.Converters;
using System.Collections.Generic;

namespace SPICA.Formats.Generic.Blender
{
	public class BLENDVertex
	{
		public PICAVertex vert;
		public Dictionary<int, float> vertGroup = new Dictionary<int, float>();

		public BLENDVertex(PICAVertex v)
		{
			vert = v;
		}

		public bool Matches(PICAVertex other) => other.Position.X == vert.Position.X && other.Position.Y == vert.Position.Y && other.Position.Z == vert.Position.Z;
	}
}
