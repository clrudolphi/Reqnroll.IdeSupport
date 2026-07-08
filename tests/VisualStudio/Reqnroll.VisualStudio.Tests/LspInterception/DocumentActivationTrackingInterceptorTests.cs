using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.LspInterception;

/// <summary>
/// <see cref="DocumentActivationTrackingInterceptor"/> drives <see cref="DocumentActivationState"/>
/// from <c>didOpen</c>/<c>didClose</c> traffic. The one case where it needs to inject a message
/// itself (activation raced ahead of <c>didOpen</c>) requires a real <c>LspInterceptingPipe</c>,
/// which is VS-bound plumbing not constructed in these tests — mirroring
/// <see cref="ScaffoldTrackingInterceptorTests"/>, we verify the observable contract that doesn't
/// require it: message classification, state-machine wiring, and safety when the pipe is
/// unavailable.
/// </summary>
public class DocumentActivationTrackingInterceptorTests
{
    private static DocumentActivationTrackingInterceptor Create(DocumentActivationState state) =>
        new(state, getPipe: () => null, trace: new TraceSource("test"));

    private static LspMessage Send(JObject body) => new(LspMessageDirection.Send, body, DateTimeOffset.Now);

    private static JObject DidOpen(string uri) => new()
    {
        ["jsonrpc"] = "2.0",
        ["method"]  = "textDocument/didOpen",
        ["params"]  = new JObject { ["textDocument"] = new JObject { ["uri"] = uri } },
    };

    private static JObject DidClose(string uri) => new()
    {
        ["jsonrpc"] = "2.0",
        ["method"]  = "textDocument/didClose",
        ["params"]  = new JObject { ["textDocument"] = new JObject { ["uri"] = uri } },
    };

    [Fact]
    public async Task DidOpen_for_a_feature_file_with_no_prior_activation_passes_through()
    {
        var result = await Create(new DocumentActivationState()).InterceptAsync(
            Send(DidOpen("file:///c:/w/Calculator.feature")), CancellationToken.None);

        result.Should().Be(LspInterceptorResult.PassThrough);
    }

    [Fact]
    public async Task DidOpen_for_a_non_feature_file_passes_through_untouched()
    {
        var state = new DocumentActivationState();
        state.OnWindowActivated(@"c:\w\Steps.cs");

        var result = await Create(state).InterceptAsync(
            Send(DidOpen("file:///c:/w/Steps.cs")), CancellationToken.None);

        result.Should().Be(LspInterceptorResult.PassThrough);
    }

    [Fact]
    public async Task DidClose_for_a_feature_file_passes_through()
    {
        var result = await Create(new DocumentActivationState()).InterceptAsync(
            Send(DidClose("file:///c:/w/Calculator.feature")), CancellationToken.None);

        result.Should().Be(LspInterceptorResult.PassThrough);
    }

    [Fact]
    public async Task An_unrelated_send_message_passes_through()
    {
        var result = await Create(new DocumentActivationState()).InterceptAsync(
            Send(new JObject { ["jsonrpc"] = "2.0", ["method"] = "textDocument/didChange" }),
            CancellationToken.None);

        result.Should().Be(LspInterceptorResult.PassThrough);
    }

    [Fact]
    public async Task Activation_pending_didOpen_is_safe_when_the_pipe_is_unavailable()
    {
        const string uri  = "file:///c:/w/Calculator.feature";
        const string path = @"c:\w\Calculator.feature";
        var state = new DocumentActivationState();
        state.OnWindowActivated(path); // activation races ahead of didOpen

        var sut = Create(state);
        var act = async () => await sut.InterceptAsync(Send(DidOpen(uri)), CancellationToken.None);

        // Pipe is null (not yet constructed), so the interceptor must degrade to a plain
        // passthrough rather than throwing — same contract ScaffoldTrackingInterceptor has for
        // its own "monitor unavailable" case.
        (await act.Should().NotThrowAsync()).Which.Should().Be(LspInterceptorResult.PassThrough);
    }
}
