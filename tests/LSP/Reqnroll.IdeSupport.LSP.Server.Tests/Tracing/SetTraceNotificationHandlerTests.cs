#nullable enable

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.LSP.Server.Tracing;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Tracing;

public class SetTraceNotificationHandlerTests
{
    [Fact]
    public async Task Handle_updates_the_trace_services_level()
    {
        var traceService = Substitute.For<ITraceService>();
        var sut = new SetTraceNotificationHandler(traceService);

        await sut.Handle(new SetTraceParams { Value = InitializeTrace.Verbose }, CancellationToken.None);

        traceService.Level.Should().Be(InitializeTrace.Verbose);
    }
}
