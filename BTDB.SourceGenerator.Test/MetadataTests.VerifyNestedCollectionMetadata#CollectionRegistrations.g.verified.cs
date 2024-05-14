﻿//HintName: CollectionRegistrations.g.cs
// <auto-generated/>
#nullable enable
#pragma warning disable 612,618
using System;
using System.Runtime.CompilerServices;
[CompilerGenerated]
static file class CollectionRegistrations
{
    [ModuleInitializer]
    internal static void Register4BTDB()
    {
        BTDB.Serialization.ReflectionMetadata.RegisterCollection(new()
        {
            Type = typeof(global::System.Collections.Generic.List<string>),
            ElementKeyType = typeof(string),
            Creator = &Create1,
            Adder = &Add1
        });

        static object Create1(uint capacity)
        {
            return new global::System.Collections.Generic.List<string>((int)capacity);
        }

        static void Add1(object c, ref byte value)
        {
            Unsafe.As<global::System.Collections.Generic.List<string>>(c).Add(Unsafe.As<byte, string>(ref value));
        }

        BTDB.Serialization.ReflectionMetadata.RegisterCollection(new()
        {
            Type = typeof(global::System.Collections.Generic.Dictionary<int, global::System.Collections.Generic.List<string>>),
            ElementKeyType = typeof(int),
            ElementValueType = typeof(global::System.Collections.Generic.List<string>),
            Creator = &Create2,
            AdderKeyValue = &Add2
        });

        static object Create2(uint capacity)
        {
            return new global::System.Collections.Generic.Dictionary<int, global::System.Collections.Generic.List<string>>((int)capacity);
        }

        static void Add2(object c, ref byte key, ref byte value)
        {
            Unsafe.As<global::System.Collections.Generic.Dictionary<int, global::System.Collections.Generic.List<string>>>(c).Add(Unsafe.As<byte, int>(ref key), Unsafe.As<byte, global::System.Collections.Generic.List<string>>(ref value));
        }
    }
}