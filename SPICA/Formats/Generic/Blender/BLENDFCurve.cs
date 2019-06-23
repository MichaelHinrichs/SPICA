using SPICA.Formats.CtrH3D.Animation;
using System.Numerics;

namespace SPICA.Formats.Generic.Blender
{
	public class BLENDFCurve
	{
		private readonly H3DAnimTransform _transform;
		private readonly H3DAnimQuatTransform _quaternion;

		public BLENDFCurve(object transform)
		{
			if (transform is H3DAnimTransform tr)
			{
				_transform = tr;
				_quaternion = null;
			}
			else if (transform is H3DAnimQuatTransform quat)
			{
				_transform = null;
				_quaternion = quat;
			}
		}

		public bool IsNull => _transform == null && _quaternion == null;
		public bool IsQuaternion => _transform == null && _quaternion != null;

		public bool NothingExists()
		{
			var skip = true;

			if (_transform != null)
			{
				skip = !_transform.TranslationExists
				&& !_transform.RotationExists
				&& !_transform.ScaleExists;
			}
			else if (_quaternion != null)
			{
				skip = !_quaternion.HasTranslation
				&& !_quaternion.HasRotation
				&& !_quaternion.HasScale;
			}

			return skip;
		}

		public Vector3 GetLocationAtFrame(int frame)
		{
			if (_transform != null)
			{
				return new Vector3(
					_transform.TranslationX.GetFrameValue(frame),
					_transform.TranslationY.GetFrameValue(frame),
					_transform.TranslationZ.GetFrameValue(frame)
				);
			}
			else if (_quaternion != null)
			{
				return _quaternion.GetTranslationValue(frame);
			}

			return default;
		}

		public object GetRotationAtFrame(int frame)
		{
			if (_transform != null)
			{
				return new Vector3(
					_transform.RotationX.GetFrameValue(frame),
					_transform.RotationY.GetFrameValue(frame),
					_transform.RotationZ.GetFrameValue(frame)
				);
			}
			else if (_quaternion != null)
			{
				return _quaternion.GetRotationValue(frame);
			}

			return null;
		}

		public Vector3 GetScaleAtFrame(int frame)
		{
			if (_transform != null)
			{
				return new Vector3(
					_transform.ScaleX.GetFrameValue(frame),
					_transform.ScaleY.GetFrameValue(frame),
					_transform.ScaleZ.GetFrameValue(frame)
				);
			}
			else if (_quaternion != null)
			{
				return _quaternion.GetScaleValue(frame);
			}

			return default;
		}
	}
}
