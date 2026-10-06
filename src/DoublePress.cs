namespace ModStatistik
{
    public sealed class DoublePress
    {
        private bool pending;
        private float first;
        public void Reset() { pending = false; }
        public bool Update(float now, bool pressed)
        {
            if (pending && (now < first || now - first > 0.35f)) pending = false;
            if (!pressed) return false;
            if (pending) { pending = false; return true; }
            pending = true;
            first = now;
            return false;
        }
    }
}
