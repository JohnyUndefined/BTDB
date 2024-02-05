﻿//HintName: ErrorHandler.g.cs
// <auto-generated/>
#pragma warning disable 612,618
using System;
using System.Runtime.CompilerServices;

[CompilerGenerated]
static file class ErrorHandlerRegistration
{
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static global::ErrorHandler Constr(global::ILogger logger);
    [ModuleInitializer]
    internal static unsafe void Register4BTDB()
    {
        global::BTDB.IOC.IContainer.RegisterFactory(typeof(global::ErrorHandler), (container, ctx) =>
        {
            var f0 = container.CreateFactory(ctx, typeof(global::ILogger), "logger");
            if (f0 == null) throw new global::System.ArgumentException("Cannot resolve ILogger logger parameter of ErrorHandler");
            return (container2, ctx2) =>
            {
                var res = Constr(Unsafe.As<global::ILogger>(f0(container2, ctx2)));
                return res;
            };
        });
    }
}
