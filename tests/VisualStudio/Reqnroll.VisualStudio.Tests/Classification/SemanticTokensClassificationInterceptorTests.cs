using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Reqnroll.IdeSupport.VisualStudio.Extension.Classification;
using Reqnroll.IdeSupport.VisualStudio.Extension.LspInterception;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.Classification;

public class SemanticTokensClassificationInterceptorTests
{
    private static readonly string[] Legend =
    {
        "reqnroll.keyword", "reqnroll.tag", "reqnroll.description", "reqnroll.comment",
        "reqnroll.doc_string", "reqnroll.data_table", "reqnroll.data_table_header",
        "reqnroll.step_parameter", "reqnroll.scenario_outline_placeholder", "reqnroll.undefined_step",
    };

    private static (SemanticTokenClassificationStore store, SemanticTokensClassificationInterceptor sut) Create()
    {
        var store = new SemanticTokenClassificationStore();
        return (store, new SemanticTokensClassificationInterceptor(store, new TraceSource("test")));
    }

    private static LspMessage Receive(JObject body) => new(LspMessageDirection.Receive, body, DateTimeOffset.Now);

    private static LspMessage InitializeResponse(string[] legend) => Receive(new JObject
    {
        ["jsonrpc"] = "2.0",
        ["id"] = 2,
        ["result"] = new JObject
        {
            ["capabilities"] = new JObject
            {
                ["semanticTokensProvider"] = new JObject
                {
                    ["legend"] = new JObject
                    {
                        ["tokenTypes"] = new JArray(legend),
                        ["tokenModifiers"] = new JArray(),
                    },
                },
            },
        },
    });

    private static LspMessage PushNotification(string uri, int[] data) => Receive(new JObject
    {
        ["jsonrpc"] = "2.0",
        ["method"] = "reqnroll/semanticTokens",
        ["params"] = new JObject
        {
            ["uri"] = uri,
            ["version"] = 1,
            ["data"] = JArray.FromObject(data),
        },
    });

    // ── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Push_notification_decodes_tokens_into_the_store_using_the_legend()
    {
        const string uri = "file:///c:/w/A.feature";
        var (store, sut) = Create();

        await sut.InterceptAsync(InitializeResponse(Legend), CancellationToken.None);
        // Two tokens: (line0,char0,len8 → reqnroll.keyword) and (line2,char0,len5 → reqnroll.comment).
        await sut.InterceptAsync(PushNotification(uri, new[] { 0, 0, 8, 0, 0, 2, 0, 5, 3, 0 }), CancellationToken.None);

        var key = SemanticTokenClassificationStore.NormalizeKey(uri)!;
        store.TryGetTokens(key, out var tokens).Should().BeTrue();
        tokens.Should().HaveCount(2);

        tokens[0].Line.Should().Be(0);
        tokens[0].StartChar.Should().Be(0);
        tokens[0].Length.Should().Be(8);
        tokens[0].TokenType.Should().Be("reqnroll.keyword");

        tokens[1].Line.Should().Be(2);          // delta-line 2
        tokens[1].StartChar.Should().Be(0);
        tokens[1].Length.Should().Be(5);
        tokens[1].TokenType.Should().Be("reqnroll.comment"); // legend index 3
    }

    [Fact]
    public async Task Push_notification_is_passed_through_untouched()
    {
        var (_, sut) = Create();
        await sut.InterceptAsync(InitializeResponse(Legend), CancellationToken.None);

        var result = await sut.InterceptAsync(
            PushNotification("file:///c:/w/A.feature", new[] { 0, 0, 8, 0, 0 }), CancellationToken.None);

        result.Should().Be(LspInterceptorResult.PassThrough, "the interceptor only observes; VS still ignores the notification");
    }

    [Fact]
    public async Task Unrelated_message_passes_through_and_stores_nothing()
    {
        var (store, sut) = Create();

        var result = await sut.InterceptAsync(
            Receive(new JObject { ["jsonrpc"] = "2.0", ["method"] = "textDocument/didOpen" }),
            CancellationToken.None);

        result.Should().Be(LspInterceptorResult.PassThrough);
        store.TryGetTokens(SemanticTokenClassificationStore.NormalizeKey(@"c:\w\A.feature")!, out _).Should().BeFalse();
    }
}
