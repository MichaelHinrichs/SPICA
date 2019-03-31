using SPICA.PICA.Converters;

namespace SPICA.Formats.Generic.Blender
{
	public class BLENDVertex
	{
		public PICAVertex vert;
		public int vertGroup;

		public BLENDVertex(PICAVertex v, int g)
		{
			vert = v;
			vertGroup = g;
		}

		public bool Matches(PICAVertex other) => other.Position.X == vert.Position.X && other.Position.Y == vert.Position.Y && other.Position.Z == vert.Position.Z;
	}
}
