using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

[assembly: InternalsVisibleTo("reactive-extensions-test")]
[assembly: InternalsVisibleTo("reactive-extensions-test-core")]
[assembly: InternalsVisibleTo("reactive-extensions-benchmarks")]

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Represents a single-producer, single-consumer unbounded queue.
    /// </summary>
    /// <typeparam name="T">The element type stored.</typeparam>
    internal sealed class SpscLinkedArrayQueue<T> : ISimpleQueue<T>
    {
        Node[] producerArray;

        long producerIndex;

        Node[] consumerArray;

        long consumerIndex;

        internal SpscLinkedArrayQueue(int islandSize)
        {
            var a = new Node[Math.Max(2, pow2(islandSize))];
            producerArray = a;
            consumerArray = a;
            producerIndex = 0L;
            Volatile.Write(ref consumerIndex, 0L);
        }

        internal void Offer(T item)
        {
            var a = producerArray;
            var pi = producerIndex;
            var m = a.Length - 1;

            var offset1 = (int)(pi + 1) & m;
            var offset0 = (int)pi & m;
            var s = Volatile.Read(ref a[offset1].state);
            if (s == 0)
            {
                a[offset0].value = item;
                Volatile.Write(ref a[offset0].state, 1);
                Volatile.Write(ref producerIndex, pi + 1);
            }
            else
            {
                var b = new Node[a.Length];
                b[offset0].value = item;
                b[offset0].state = 1;
                a[offset0].next = b;
                producerArray = b;
                Volatile.Write(ref a[offset0].state, 2);
                Volatile.Write(ref producerIndex, pi + 1);
            }
        }

        public T TryPoll(out bool success)
        {
            var a = consumerArray;
            var ci = consumerIndex;
            var m = a.Length - 1;

            var offset0 = (int)ci & m;
            var s = Volatile.Read(ref a[offset0].state);
            if (s == 0)
            {
                success = false;
                return default(T);
            }
            if (s == 2)
            {
                var b = a[offset0].next;
                a[offset0].next = null;
                consumerArray = b;
                a = b;
            }
            var item = a[offset0].value;
            a[offset0].value = default(T);
            Volatile.Write(ref a[offset0].state, 0);
            Volatile.Write(ref consumerIndex, ci + 1);
            success = true;
            return item;
        }

        public bool IsEmpty()
        {
            return Volatile.Read(ref producerIndex) == Volatile.Read(ref consumerIndex);
        }

        public void Clear()
        {
            for (; ;)
            {
                var v = TryPoll(out var success);
                if (!success && IsEmpty())
                {
                    break;
                }
            }
        }

        public bool TryOffer(T item)
        {
            Offer(item);
            return true;
        }

        internal static int pow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        internal struct Node
        {
            internal int state;
            internal T value;
            internal Node[] next;
        }
    }
}
