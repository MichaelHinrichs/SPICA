using System.Collections.Generic;
using System.Numerics;

namespace SPICA.Formats.Generic.UnityAnim
{
    public class ANIMAnimationClip
    {
        public float duration;
        public string name;
        
        public List<ANIMCurve<Vector3>> eulerCurves =  new List<ANIMCurve<Vector3>>();
        public List<ANIMCurve<Vector3>> positionCurves =  new List<ANIMCurve<Vector3>>();
        public List<ANIMCurve<Vector3>> scaleCurves =  new List<ANIMCurve<Vector3>>();
        public List<ANIMCurve<float>>   floatCurves =  new List<ANIMCurve<float>>();

        public ANIMAnimationClip(float duration, string name = "Animation_0")
        {
            this.duration = duration;
            this.name = name;
        }
    }
}