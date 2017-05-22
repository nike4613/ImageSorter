using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class DefaultArray<T> : IEnumerable<T>
    {
        private T[] iarr;
        public T[] Array { get { return iarr; } }

        public DefaultArray(T[] arr)
        {
            iarr = arr;
        }

        public T this[int index]
        {
            get
            {
                return iarr[index];
            }
            
            set
            {
                iarr[index] = value;
            }
        }

        public T this[int index, T def]
        {
            get
            {
                return iarr.Length > index ? this[index] : def;
            }
        }

        public static implicit operator DefaultArray<T>(T[] ar)
        {
            return new DefaultArray<T>(ar);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)iarr).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)iarr).GetEnumerator();
        }
    }
}
