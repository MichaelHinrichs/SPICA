using SPICA.PICA.Converters;

namespace SPICA.Formats.Generic.Blender
{
	public struct BLENDVertex
	{
		public PICAVertex vert;
		public int vertGroup;

		public BLENDVertex(PICAVertex v, int g)
		{
			vert = v;
			vertGroup = g;
		}
	}
}
