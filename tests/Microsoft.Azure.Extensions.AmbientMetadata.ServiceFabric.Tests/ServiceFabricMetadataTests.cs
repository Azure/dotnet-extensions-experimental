// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using ServiceFabric.Mocks;
using Xunit;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric.Test;

public class ServiceFabricMetadataTests
{
    private readonly ServiceFabricMetadata _testClass;

    public ServiceFabricMetadataTests()
    {
        _testClass = new ServiceFabricMetadata();
    }

    [Fact]
    public void CanConstruct()
    {
        var instance = new ServiceFabricMetadata();
        Assert.NotNull(instance);
    }

    [Fact]
    public void DefaultChecks()
    {
        var instance = new ServiceFabricMetadata();
        Assert.Equal(string.Empty, instance.ServiceName);
        Assert.Equal(string.Empty, instance.ApplicationName);
        Assert.Equal(string.Empty, instance.NodeName);
        Assert.Equal(string.Empty, instance.Cloud);
        Assert.Equal(string.Empty, instance.Geo);
        Assert.Equal(string.Empty, instance.Region);

        Assert.Null(instance.ServiceTypeName);
        Assert.Null(instance.ApplicationTypeName);
        Assert.Null(instance.NodeType);
    }

    [Fact]
    public void CanSetAndGetServiceName()
    {
        const string TestValue = "TestValue1526235764";
        _testClass.ServiceName = TestValue;
        Assert.Equal(TestValue, _testClass.ServiceName);
    }

    [Fact]
    public void CanSetAndGetServiceTypeName()
    {
        const string TestValue = "TestValue2141991402";
        _testClass.ServiceTypeName = TestValue;
        Assert.Equal(TestValue, _testClass.ServiceTypeName);
    }

    [Fact]
    public void CanSetAndGetReplicaOrInstanceId()
    {
        const long TestValue = 814_005_478L;
        _testClass.ReplicaOrInstanceId = TestValue;
        Assert.Equal(TestValue, _testClass.ReplicaOrInstanceId);
    }

    [Fact]
    public void CanSetAndGetPartitionId()
    {
        var testValue = new Guid("cf00b4bc-8f7c-47b1-858b-894c18284394");
        _testClass.PartitionId = testValue;
        Assert.Equal(testValue, _testClass.PartitionId);
    }

    [Fact]
    public void CanSetAndGetApplicationName()
    {
        const string TestValue = "TestValue406690786";
        _testClass.ApplicationName = TestValue;
        Assert.Equal(TestValue, _testClass.ApplicationName);
    }

    [Fact]
    public void CanSetAndGetApplicationTypeName()
    {
        const string TestValue = "TestValue1367354274";
        _testClass.ApplicationTypeName = TestValue;
        Assert.Equal(TestValue, _testClass.ApplicationTypeName);
    }

    [Fact]
    public void CanSetAndGetNodeName()
    {
        const string TestValue = "TestValue4643028";
        _testClass.NodeName = TestValue;
        Assert.Equal(TestValue, _testClass.NodeName);
    }

    [Fact]
    public void CanSetAndGetNodeType()
    {
        const string TestValue = "TestValue865617768";
        _testClass.NodeType = TestValue;
        Assert.Equal(TestValue, _testClass.NodeType);
    }

    [Fact]
    public void CanSetAndGetCloud()
    {
        const string TestValue = "cloud";
        _testClass.Cloud = TestValue;
        Assert.Equal(TestValue, _testClass.Cloud);
    }

    [Fact]
    public void CanSetAndGetGeo()
    {
        const string TestValue = "geo";
        _testClass.Geo = TestValue;
        Assert.Equal(TestValue, _testClass.Geo);
    }

    [Fact]
    public void CanSetAndGetRegion()
    {
        const string TestValue = "region";
        _testClass.Region = TestValue;
        Assert.Equal(TestValue, _testClass.Region);
    }

    [Fact]
    public void NullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceFabricMetadataSource(null!, "sectionName"));
        Assert.Throws<ArgumentNullException>(() => ServiceFabricMetadataExtensions.AddServiceFabricMetadata(new ConfigurationBuilder(), MockStatelessServiceContextFactory.Default, null!));
        Assert.Throws<ArgumentNullException>(() => ServiceFabricMetadataExtensions.AddServiceFabricMetadata(null!, MockStatelessServiceContextFactory.Default));
        Assert.Throws<ArgumentNullException>(() => ServiceFabricMetadataExtensions.UseServiceFabricMetadata(null!, MockStatelessServiceContextFactory.Default));
        Assert.Throws<ArgumentNullException>(() => FakeHost.CreateBuilder().UseServiceFabricMetadata(MockStatelessServiceContextFactory.Default, null!));
        Assert.Throws<ArgumentNullException>(() => FakeHost.CreateBuilder().UseServiceFabricMetadata(null!));

        Assert.Throws<ArgumentNullException>(() => FakeHost.CreateBuilder()
            .ConfigureHostConfiguration(
                static configurationBuilder => _ = configurationBuilder.AddServiceFabricMetadata(null!))
            .Build());
    }
}
