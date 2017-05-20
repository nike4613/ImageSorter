using System;
using System.Collections;
using System.Collections.Generic;

namespace Utilites
{
    public class Pair<T> : IEnumerable<T>
    {
        public T First { get; set; }
        public T Last { get; set; }

        public Pair(T a, T b)
        {
            First = a;
            Last = b;
        }

        public Pair() : this(default(T),default(T))
        {

        }

        public Pair(T[] ar) : this(ar[0],ar[1])
        {

        }

        public override string ToString()
        {
            return "Pair({0},{1})".SFormat(First,Last);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new PairEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class PairEnumerator<T> : IEnumerator<T>
    {
        private Pair<T> pair;
        private int pos = 0;

        internal PairEnumerator(Pair<T> p)
        {
            pair = p;
        }

        public T Current => pos == 1 ? pair.First : pair.Last;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            pos += 1;
            return pos < 3;
        }

        public void Reset()
        {
            pos = 0;
        }
    }
}