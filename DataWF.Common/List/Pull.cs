﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DataWF.Common
{
    public abstract class Pull
    {
        private static readonly Type[] ctorTypes = new Type[] { typeof(int) };

        public static Pull Fabric(Type type, int blockSize)
        {
            Type gtype = null;
            if (type.IsValueType || type.IsEnum)
            {
                gtype = typeof(NullablePull<>).MakeGenericType(type);
            }
            else
            {
                gtype = typeof(Pull<>).MakeGenericType(type);
            }
            return (Pull)EmitInvoker.CreateObject(gtype, ctorTypes, new object[] { blockSize }, true);
        }

        public static void Memset<T>(T[] array, T elem, int index)
        {
            int length = array.Length - index;
            if (length <= 0) return;

            array[index] = elem;
            int count;
            if (typeof(T).IsPrimitive)
            {
                var size = Marshal.SizeOf(typeof(T));
                for (count = 1; count <= length / 2; count *= 2)
                    Buffer.BlockCopy(array, index, array, index + count, count * size);
                Buffer.BlockCopy(array, index, array, index + count, (length - count) * size);
            }
            else //if (!typeof(T).IsClass)
            {
                for (count = 1; count <= length / 2; count *= 2)
                    Array.Copy(array, index, array, index + count, count);
                Array.Copy(array, index, array, index + count, length - count);
            }
        }

        public static void ReAlloc<T>(ref T[] array, int len, T NullValue)
        {
            var temp = array;
            var narray = new T[len];
            len = len > array.Length ? array.Length : len;
            if (typeof(T).IsPrimitive)
                Buffer.BlockCopy(temp, 0, narray, 0, len * Marshal.SizeOf(typeof(T)));
            else
                Array.Copy(temp, narray, len);

            Memset<T>(narray, NullValue, array.Length);
            array = narray;
        }

        public static int GetHIndex(int index, int blockSize)
        {
            short a = (short)(index / blockSize);
            short b = (short)(index % blockSize);
            return (a << 16) | (b & 0xFFFF);
        }

        public unsafe static int GetHIndexUnsafe(int index, int blockSize)
        {
            int result = 0;
            short* p = (short*)&result;
            *p = (short)(index % blockSize);
            *(p + 1) = (short)(index / blockSize);
            return result;
        }

        public static void GetBlockIndex(int index, out short block, out short blockIndex)
        {
            block = (short)(index >> 16);
            blockIndex = (short)(index & 0xFFFF);
        }

        public unsafe static void GetBlockIndexUnsafe(int index, out short block, out short blockIndex)
        {
            short* p = (short*)&index;
            blockIndex = (*p);
            block = (*(p + 1));
        }

        protected int blockSize;
        private Type itemType;


        internal Pull(int BlockSize)
        {
            blockSize = BlockSize;
        }

        public abstract object Get(int index);

        public abstract void Set(int index, object value);

        public T GetValue<T>(int index)
        {
            return ((Pull<T>)this).GetValueInternal(index);
        }

        public void SetValue<T>(int index, T value)
        {
            ((Pull<T>)this).SetValueInternal(index, value);
        }

        public virtual int Capacity { get { return 0; } }
        public int BlockSize
        {
            get { return blockSize; }
            set
            {
                if (blockSize != 0)
                {
                    blockSize = value;
                }
                else
                {
                    throw new Exception("Unable set block size after data modified");
                }
            }
        }

        public Type ItemType
        {
            get { return itemType; }
            set { itemType = value; }
        }

        public abstract bool EqualNull(object value);

        public virtual void Clear()
        {
        }

        public abstract void Trunc(int maxIndex);
    }

    public class DBNullablePull<T> : Pull<DBNullable<T>>, IEnumerable<DBNullable<T>> where T : struct
    {
        public DBNullablePull(int BlockSize) : base(BlockSize)
        {
            ItemType = typeof(T);
        }

        public override void Set(int index, object value)
        {
            SetValue(index, DBNullable<T>.CheckNull(value));
        }
    }

    public class NullablePull<T> : Pull<T?>, IEnumerable<T?> where T : struct
    {
        public NullablePull(int BlockSize) : base(BlockSize)
        {
            ItemType = typeof(T);
        }

        public override void Set(int index, object value)
        {
            SetValue(index, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }
    }

    public class Pull<T> : Pull, IEnumerable<T>
    {
        private List<T[]> array = new List<T[]>();
        private short maxIndex;

        public Pull(int blockSize) : base(blockSize)
        {
            ItemType = typeof(T);
        }

        public override int Capacity { get { return array.Count * blockSize; } }

        public override void Clear()
        {
            foreach (var a in array)
            {
                if (a != null)
                    Array.Clear(a, 0, a.Length);
            }

            array.Clear();
        }

        public override bool EqualNull(object value)
        {
            return value == null;
        }

        public override object Get(int index)
        {
            return GetValueInternal(index);
        }

        public override void Set(int index, object value)
        {
            SetValueInternal(index, (T)value);
        }

        public T GetValueInternal(int index)
        {
            GetBlockIndexUnsafe(index, out short block, out short blockIndex);
            if (block >= array.Count || array[block] == null)
                return default(T);
            return array[block][blockIndex];
        }

        public void SetValueInternal(int index, T value)
        {
            GetBlockIndexUnsafe(index, out short block, out short blockIndex);
            while (block > array.Count)
                array.Add(null);
            if (block == array.Count)
            {
                array.Add(new T[blockSize]);
                maxIndex = 0;
            }
            if (array[block] == null)
            {
                array[block] = new T[blockSize];
            }
            array[block][blockIndex] = value;
            if (block == array.Count - 1)
            {
                maxIndex = Math.Max(maxIndex, blockIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < array.Count; i++)
            {
                var block = array[i];
                var size = i == array.Count - 1 ? maxIndex : blockSize;
                for (int j = 0; j < size; j++)
                {
                    if (block == null)
                        yield return default(T);
                    yield return block[j];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Trunc(int maxIndex)
        {
            GetBlockIndexUnsafe(maxIndex, out short block, out short blockIndex);
            while (block < array.Count - 1)
            {
                array.RemoveAt(array.Count - 1);
            }
            if (block < array.Count && blockIndex + 1 < BlockSize)
            {
                Memset<T>(array[block], default(T), blockIndex + 1);
            }
        }
    }
}
