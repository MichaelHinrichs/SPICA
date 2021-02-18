using System.Collections.Generic;

namespace SPICA.Formats.Generic.UnityAnim
{
    public class ANIMCurve<T>
    {
        public string attribute;
        public string path;
        public List<ANIMKeyFrame<T>> keyFrames = new List<ANIMKeyFrame<T>>();

        public ANIMCurve(string path, string attribute = "")
        {
            this.path = path;
            this.attribute = attribute;
        }
        
        public void Add(ANIMKeyFrame<T> keyFrame)
        {
            keyFrames.Add(keyFrame);
        }
        
        public void Add(float time, T value)
        {
            keyFrames.Add(new ANIMKeyFrame<T>(time, value));
        }
    }
}