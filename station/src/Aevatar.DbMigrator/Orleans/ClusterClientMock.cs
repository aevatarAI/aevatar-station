using System;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.DbMigrator.Orleans;

/// <summary>
/// A class to mock Orlean's ClusterClient so that DBMigrator can run without a real Orleans cluster.
/// </summary>
class ClusterClientMock : IClusterClient
{
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidKey
    {
        throw new NotImplementedException();
    }

    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey
    {
        throw new NotImplementedException();
    }

    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithStringKey
    {
        throw new NotImplementedException();
    }

    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidCompoundKey
    {
        throw new NotImplementedException();
    }

    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerCompoundKey
    {
        throw new NotImplementedException();
    }

    public TGrainObserverInterface CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
    {
        throw new NotImplementedException();
    }

    public void DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
    {
        throw new NotImplementedException();
    }

    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension)
    {
        throw new NotImplementedException();
    }

    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension)
    {
        throw new NotImplementedException();
    }

    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId) where TGrainInterface : IAddressable
    {
        throw new NotImplementedException();
    }

    public IAddressable GetGrain(GrainId grainId)
    {
        throw new NotImplementedException();
    }

    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType)
    {
        throw new NotImplementedException();
    }

    public IServiceProvider ServiceProvider { get; }
}