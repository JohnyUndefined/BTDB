﻿//HintName: TestNamespace.Logger.g.cs
// <auto-generated/>
#pragma warning disable 618
using System;
using System.Runtime.CompilerServices;

namespace TestNamespace;

public partial class Logger
{
    [ModuleInitializer]
    internal static unsafe void Register4BTDB()
    {
        global::BTDB.IOC.IContainer.RegisterFactory(typeof(global::TestNamespace.Logger), (container, ctx) =>
        {
            var f0 = container.CreateFactory(ctx, typeof(int), "A");
            if (f0 == null) throw new global::System.ArgumentException("Cannot resolve int A property of TestNamespace.Logger");
            var f1 = container.CreateFactory(ctx, typeof(int), "B");
            if (f1 == null) throw new global::System.ArgumentException("Cannot resolve int B property of TestNamespace.Logger");
            return (container2, ctx2) =>
            {
                var res = new global::TestNamespace.Logger();
                res.A = (int)(f0(container2, ctx2));
                res.B = (int)(f1(container2, ctx2));
                return res;
            };
        });
    }
}