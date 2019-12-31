﻿using System.Collections.Generic;

namespace Accelbuffer
{
    internal sealed class ListSerializeProxy<T> : ISerializeProxy<List<T>>
    {
        unsafe List<T> ISerializeProxy<List<T>>.Deserialize(in UnmanagedReader* reader)
        {
            int count = reader->ReadVariableInt32(0);

            if (count == -1)
            {
                return null;
            }

            List<T> result = new List<T>(count);

            while (count > 0)
            {
                result.Add(Serializer<T>.Deserialize(reader));
                count--;
            }

            return result;
        }

        unsafe void ISerializeProxy<List<T>>.Serialize(in List<T> obj, in UnmanagedWriter* writer)
        {
            int count = obj == null ? -1 : obj.Count;
            writer->WriteValue(0, count, Number.Var);

            for (int i = 0; i < count; i++)
            {
                Serializer<T>.Serialize(obj[i], writer);
            }
        }
    }
}
