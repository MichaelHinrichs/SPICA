namespace SPICA.Formats.Generic.UnityAnim
{
    public class ANIMKeyFrame<T>
    {
        public float time;
        public T value;
        public T inSlope;
        public T outSlope;
        
        public ANIMKeyFrame(float time, T value, T inSlope = default(T), T outSlope = default(T))
        {
            this.time = time;
            this.value = value;
            this.inSlope = inSlope;
            this.outSlope = outSlope;
        }
    }
}