#nullable enable

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reqnroll.IdeSupport.LSP.Server.Tracing;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Tracing;

public class TraceServiceTests
{
    private static TraceService CreateSut(ILanguageServerFacade facade) => new(facade);

    [Fact]
    public void Trace_sends_nothing_when_level_is_Off()
    {
        var facade = Substitute.For<ILanguageServerFacade>();
        var sut = CreateSut(facade);
        sut.Level = InitializeTrace.Off;

        sut.Trace("hello");

        facade.DidNotReceiveWithAnyArgs().SendNotification(default(IRequest)!);
    }

    [Fact]
    public void Trace_sends_a_logTrace_notification_when_level_is_Messages()
    {
        var facade = Substitute.For<ILanguageServerFacade>();
        var sut = CreateSut(facade);
        sut.Level = InitializeTrace.Messages;

        sut.Trace("hello");

        facade.Received(1).SendNotification(Arg.Is<IRequest>(r =>
            ((LogTraceParams)r).Message == "hello" && ((LogTraceParams)r).Verbose == null));
    }

    [Fact]
    public void Trace_does_not_invoke_the_verbose_callback_when_level_is_Messages()
    {
        var facade = Substitute.For<ILanguageServerFacade>();
        var sut = CreateSut(facade);
        sut.Level = InitializeTrace.Messages;
        var verboseCalled = false;

        sut.Trace("hello", () => { verboseCalled = true; return "detail"; });

        verboseCalled.Should().BeFalse("verbose detail should only be computed when trace is Verbose");
    }

    [Fact]
    public void Trace_includes_verbose_detail_when_level_is_Verbose()
    {
        var facade = Substitute.For<ILanguageServerFacade>();
        var sut = CreateSut(facade);
        sut.Level = InitializeTrace.Verbose;

        sut.Trace("hello", () => "detail");

        facade.Received(1).SendNotification(Arg.Is<IRequest>(r =>
            ((LogTraceParams)r).Message == "hello" && ((LogTraceParams)r).Verbose == "detail"));
    }
}
