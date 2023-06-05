// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public class DecoratorTests
{
    private readonly CosmosExceptionHandlingDecorator _decorator = new();

    public class TestContext
    {
        // Test class
    }

    public class TestException : Exception
    {
        // Test exception
    }

    public class NewException : Exception
    {
        // Test exception
    }

    public class TestInvalidState : Exception
    {
        // Test exception
    }

    internal class TestDecorator :
        IOnAfterCosmosDecorator<TestContext>,
        IOnBeforeCosmosDecorator<TestContext>,
        IOnCallCosmosDecorator<TestContext>,
        IOnExceptionCosmosDecorator<TestContext>,
        IOnFinallyCosmosDecorator<TestContext>
    {
        private readonly object? _result;
        private readonly bool _skipException;
        private readonly bool _rethrowException;

        private bool _beforeCalled;
        private bool _afterCalled;
        private bool _callCalled;
        private bool _exceptionCalled;
        private bool _finallyCalled;

        public TestDecorator(object? result, bool skipException, bool rethrowException = false)
        {
            _result = result;
            _skipException = skipException;
            _rethrowException = rethrowException;
        }

        public void OnAfter<T>(TestContext context, T result)
        {
            _afterCalled = true;
            Assert.Equal(_result, result);
        }

        public void OnBefore(TestContext context)
        {
            _beforeCalled = true;
        }

        public Task<T> OnCallAsync<T>(
            Func<Func<TestContext, Func<Exception, T>, CancellationToken, Task<T>>, TestContext, Func<Exception, T>, CancellationToken, Task<T>> callToBeDecorated,
            Func<TestContext, Func<Exception, T>, CancellationToken, Task<T>> functionParameter,
            TestContext context, Func<Exception, T> exceptionHandler, CancellationToken cancelationToken)
        {
            _callCalled = true;
            return callToBeDecorated(functionParameter, context, exceptionHandler, cancelationToken);
        }

        public bool OnException(TestContext context, Exception exception)
        {
            _exceptionCalled = true;
            Assert.IsType<TestException>(exception);

            if (_rethrowException)
            {
                throw new NewException();
            }

            return _skipException;
        }

        public void OnFinally(TestContext context)
        {
            _finallyCalled = true;
        }

        public void AssertAllCalled()
        {
            Assert.True(_beforeCalled && _afterCalled && _callCalled && _finallyCalled);
        }

        public void AssertAllCalledException()
        {
            Assert.True(_beforeCalled && _callCalled && _exceptionCalled && _finallyCalled);
        }
    }

    [Fact]
    public void TestNullValue()
    {
        RequestOptions<string> requestOptions = new() { ItemVersion = "etag" };
        DecoratedCosmosContext context = new("operation", requestOptions, null, null);

        var exception = Assert.Throws<InvalidDataException>(() => context.GetItemOf<object>());
        Assert.Equal("Item is null", exception.Message);
    }

    [Fact]
    public void TestDefaultValue()
    {
        var response = DecoratedCosmosClient<TestDocument>.ProcessException<string>(new CosmosException("", HttpStatusCode.OK, 1, "", 0));
        response.HasStatus(HttpStatusCode.OK).Should().BeTrue();

        response = DecoratedCosmosClient<TestDocument>.ProcessException<string>(new ArgumentNullException());
        response.HasStatus(HttpStatusCode.InternalServerError).Should().BeTrue();
    }

    [Fact]
    public void TestDecorationContext()
    {
        RequestOptions<string> requestOptions = new() { ItemVersion = "etag" };
        DecoratedCosmosContext context = new("operation", requestOptions, null, "item");

        context.OperationName.Should().Be("operation");
        context.RequestOptions.Should().Be(requestOptions);
        context.Item.Should().Be("item");
    }

    [Fact]
    public void GetItemOfThrows()
    {
        RequestOptions<string> requestOptions = new() { ItemVersion = "etag" };
        DecoratedCosmosContext context = new("operation", requestOptions, null, null);

        Assert.Throws<InvalidDataException>(() => context.GetItemOf<int>());
    }

    [Fact]
    public async Task TestDecoratorAsync()
    {
        string result = "result";
        TestDecorator testDecorator = new TestDecorator(result, false);

        ICallDecorationPipeline<TestContext> decorators = testDecorator.MakeCallDecorationPipeline();

        string callResult = await decorators.DoCallAsync<string>((_, _, _) => Task.FromResult(result),
            new TestContext(),
            _ => throw new TestInvalidState(),
            CancellationToken.None);

        Assert.Equal(result, callResult);
        testDecorator.AssertAllCalled();
    }

    [Fact]
    public async Task TestDecoratorExceptionHandledAsync()
    {
        string result = "result";
        TestDecorator testDecorator = new TestDecorator(result, true);

        ICallDecorationPipeline<TestContext> decorators = testDecorator.MakeCallDecorationPipeline();

        string callResult = await decorators.DoCallAsync((_, _, _) => throw new TestException(),
            new TestContext(),
            _ => result,
            CancellationToken.None);

        Assert.Equal(result, callResult);
        testDecorator.AssertAllCalledException();
    }

    [Fact]
    public async Task TestDecoratorExceptionHandledVsRethrowAsync()
    {
        string result = "result";
        TestDecorator testDecorator = new TestDecorator(result, true, true);

        ICallDecorationPipeline<TestContext> decorators = testDecorator.MakeCallDecorationPipeline();

        await Assert.ThrowsAsync<NewException>(() => decorators.DoCallAsync<string>((_, _, _) => throw new TestException(),
            new TestContext(),
            _ => throw new TestInvalidState(),
            CancellationToken.None));

        testDecorator.AssertAllCalledException();
    }

    [Fact]
    public async Task TestDecoratorExceptionNotHandledAsync()
    {
        string result = "result";
        TestDecorator testDecorator = new TestDecorator(result, false);

        ICallDecorationPipeline<TestContext> decorators = testDecorator.MakeCallDecorationPipeline();

        await Assert.ThrowsAsync<TestException>(() => decorators.DoCallAsync<string>((_, _, _) => throw new TestException(),
            new TestContext(),
            _ => throw new TestInvalidState(),
            CancellationToken.None));

        testDecorator.AssertAllCalledException();
    }

    [Fact]
    public void CosmosExceptionDecoratorTest()
    {
        CosmosExceptionTest<DatabaseServerException>(HttpStatusCode.BadRequest);
        CosmosExceptionTest<DatabaseServerException>(HttpStatusCode.Unauthorized);
        CosmosExceptionTest<DatabaseServerException>(HttpStatusCode.Forbidden);
        CosmosExceptionTest<DatabaseServerException>(HttpStatusCode.RequestEntityTooLarge);
        CosmosExceptionTest<DatabaseServerException>(HttpStatusCode.PreconditionFailed);

        CosmosExceptionTest<DatabaseRetryableException>((HttpStatusCode)429);
        CosmosExceptionTest<DatabaseRetryableException>(HttpStatusCode.RequestTimeout);
        CosmosExceptionTest<DatabaseRetryableException>(HttpStatusCode.ServiceUnavailable);
        CosmosExceptionTest<DatabaseRetryableException>(HttpStatusCode.NotFound, 1002);
        CosmosExceptionTest<DatabaseRetryableException>(HttpStatusCode.InternalServerError);
        CosmosExceptionTest<DatabaseRetryableException>(HttpStatusCode.Gone);
        CosmosExceptionTest<DatabaseRetryableException>((HttpStatusCode)449);

        CosmosExceptionTest<DatabaseException>(HttpStatusCode.BadGateway); // not covered explicitly
        CosmosExceptionTest<DatabaseException>(HttpStatusCode.OK); // not covered explicitly

        CosmosExceptionNotThrownTest(HttpStatusCode.NotFound, true);
        CosmosExceptionNotThrownTest(HttpStatusCode.Conflict, true);

        bool result = _decorator.OnException(default, new TestException());
        Assert.False(result);
    }

    private void CosmosExceptionTest<T>(HttpStatusCode source, int substatus = default)
        where T : DatabaseException
    {
        T exception = Assert.Throws<T>(() => _decorator.OnException(new("", new(), null, null), new CosmosException("message", source, substatus, "activity", 0)));
        Assert.Equal((int)source, exception.StatusCode);
    }

    private void CosmosExceptionNotThrownTest(HttpStatusCode source, bool returnValue)
    {
        bool result = _decorator.OnException(new("", new(), null, null), new CosmosException("message", source, default, "activity", 0));
        Assert.Equal(returnValue, result);
    }
}
