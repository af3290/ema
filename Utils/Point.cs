namespace Utils
{
    public class Point<T>
    {
        public T X, Y;
        
        public Point(T x, T y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            var p = obj as Point<T>;

            if (p!=null)
            {
                return X.Equals(p.X) && Y.Equals(p.Y);
            }

            return base.Equals(obj);
        }
    }
}