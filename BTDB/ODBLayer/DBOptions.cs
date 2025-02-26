﻿using System.Diagnostics.CodeAnalysis;
using BTDB.Encrypted;
using BTDB.FieldHandler;
using BTDB.IOC;
using BTDB.KVDBLayer;

namespace BTDB.ODBLayer;

public class DBOptions
{
    public DBOptions()
    {
        AutoRegisterType = true;
        AutoRegisterRelations = true;
    }

    public DBOptions WithoutAutoRegistration()
    {
        AutoRegisterType = false;
        return this;
    }

    public DBOptions WithoutAutoRegistrationOfRelations()
    {
        AutoRegisterRelations = false;
        return this;
    }

    public DBOptions WithAutoSkipUnknownTypes()
    {
        AutoSkipUnknownTypes = true;
        return this;
    }

    public DBOptions WithCustomType2NameRegistry(IType2NameRegistry registry)
    {
        CustomType2NameRegistry = registry;
        return this;
    }

    public DBOptions WithSelfHealing()
    {
        SelfHealing = true;
        return this;
    }

    public DBOptions WithSymmetricCipher(ISymmetricCipher cipher)
    {
        SymmetricCipher = cipher;
        return this;
    }

    public DBOptions WithContainer(IContainer container)
    {
        Container = container;
        return this;
    }

    public DBOptions WithName(string name)
    {
        Name = name;
        return this;
    }

    public DBOptions WithFieldHandlerLogger(IFieldHandlerLogger fieldHandlerLogger)
    {
        FieldHandlerLogger = fieldHandlerLogger;
        return this;
    }

    public DBOptions WithLogger(IObjectDBLogger logger)
    {
        Logger = logger;
        return this;
    }

    public bool AutoRegisterRelations { get; private set; }
    public bool AutoRegisterType { get; private set; }
    public bool AutoSkipUnknownTypes { get; private set; }
    public IType2NameRegistry? CustomType2NameRegistry { get; private set; }
    public bool SelfHealing { get; private set; }
    public IContainer? Container { get; private set; }
    public string? Name { get; private set; }
    public IFieldHandlerLogger? FieldHandlerLogger { get; private set; }

    public ISymmetricCipher? SymmetricCipher { get; private set; }

    public IObjectDBLogger? Logger { get; private set; }

    [DoesNotReturn]
    public void ThrowBTDBException(string message)
    {
        if (Name != null)
        {
            throw new BTDBException(message + " (" + Name + ")");
        }

        throw new BTDBException(message);
    }
}
