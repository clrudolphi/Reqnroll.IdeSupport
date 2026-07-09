using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reqnroll.IdeSupport.Common.Diagnostics;
using Reqnroll.IdeSupport.LSP.Core.Bindings;
using Reqnroll.IdeSupport.LSP.Core.Documents;



using Reqnroll.IdeSupport.LSP.Core.Gherkin.Parsing;


using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Server.Discovery;
using Reqnroll.IdeSupport.LSP.Server.Documents;
using Reqnroll.IdeSupport.LSP.Server.Features.Rename;
using Reqnroll.IdeSupport.LSP.Server.Protocol;
using Reqnroll.IdeSupport.LSP.Server.Features.TextSync;
using Reqnroll.IdeSupport.LSP.Server.Telemetry;
using Reqnroll.IdeSupport.LSP.Server.Workspace;

namespace Reqnroll.IdeSupport.LSP.Server.Tests.Features.Rename;

public class StepRenameHandlerTests
{
    private readonly IBindingMatchService          _matchService   = Substitute.For<IBindingMatchService>();
    private readonly ILspWorkspaceScopeManager     _scopeManager   = Substitute.For<ILspWorkspaceScopeManager>();
    private readonly IProjectBindingRegistryLookup _registryLookup = Substitute.For<IProjectBindingRegistryLookup>();
    private readonly IIdeSupportLogger               _logger         = Substitute.For<IIdeSupportLogger>();
    private readonly IDocumentBufferService         _documentBuffer = Substitute.For<IDocumentBufferService>();

    private static readonly DocumentUri CsUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");
    private static string CsPath => CsUri.GetFileSystemPath();

    public StepRenameHandlerTests()
    {
        // No project owners → FindUsages is called with a null filter and there are
        // no feature-side edits; the tests focus on the generated C# attribute edit.
        _scopeManager.ResolveOwners(Arg.Any<DocumentUri>())
                     .Returns(Array.Empty<LspReqnrollProject>());
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(System.Array.Empty<StepBindingMatch>());
    }

    private StepRenameHandler CreateSut() =>
        new(_matchService, _scopeManager, _registryLookup, _logger, _documentBuffer);

    private StepRenameHandler CreateSutWithTelemetry(ILspTelemetryService telemetry) =>
        new(_matchService, _scopeManager, _registryLookup, _logger, _documentBuffer, telemetry);

    private void SetupBuffer(string csText)
    {
        _documentBuffer
            .TryGet(Arg.Any<DocumentUri>(), out Arg.Any<DocumentBuffer?>())
            .Returns(ci =>
            {
                ci[1] = new DocumentBuffer(CsUri, 1, csText);
                return true;
            });
    }

    private void SetupBuffers(params (DocumentUri Uri, string Text)[] buffers)
    {
        var map = buffers.ToDictionary(b => b.Uri.ToString(), b => b.Text);
        _documentBuffer
            .TryGet(Arg.Any<DocumentUri>(), out Arg.Any<DocumentBuffer?>())
            .Returns(ci =>
            {
                var u = (DocumentUri)ci[0];
                if (map.TryGetValue(u.ToString(), out var text))
                {
                    ci[1] = new DocumentBuffer(u, 1, text);
                    return true;
                }
                ci[1] = null;
                return false;
            });
    }

    private static ProjectStepDefinitionBinding MakeBinding(
        ScenarioBlock type,
        Regex         regex,
        string        specifiedExpression,
        int           line,
        int           column = 9,
        ProjectBindingImplementation? sharedImpl = null,
        string        method = "Steps.M()")
    {
        var impl = sharedImpl ?? new ProjectBindingImplementation(
            method, null, new SourceLocation(CsPath, line, column));
        return new ProjectStepDefinitionBinding(type, regex, null, impl, specifiedExpression);
    }

    private static RenameParams RenameAt(int line, int character, string newName) =>
        new()
        {
            TextDocument = new TextDocumentIdentifier { Uri = CsUri },
            Position     = new Position(line, character),
            NewName      = newName
        };

    // ── The F16 defect: registry expression is a discovery projection (regex), not the
    //    source literal (Cucumber expression). The attribute must still be located and
    //    edited even though `binding.Expression` does not equal the source string. ──────

    [Fact]
    public async Task Rename_edits_source_literal_even_when_registry_expression_is_a_regex_projection()
    {
        // Source uses a Cucumber expression …
        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"the first number is {int}\")]\n" +          // 0-based line 6
            "        public void GivenTheFirstNumberIs(int number) { }\n" + // 0-based line 7
            "    }\n" +
            "}\n";
        SetupBuffer(csText);

        // … but the registry carries the regex projection produced by discovery.
        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the first number is (.*)$"),
            specifiedExpression: "the first number is (.*)",
            line: 8, column: 9);                                  // 1-based method line
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        // Position-based resolution: request maps to the binding's (line 8, col 9).
        var result = await CreateSut().HandleRenameAsync(
            RenameAt(line: 7, character: 8, newName: "the renamed number is {int}"),
            CancellationToken.None);

        result.Should().NotBeNull();
        var edits = result!.Changes![CsUri].ToList();
        edits.Should().ContainSingle();
        edits[0].NewText.Should().Be("\"the renamed number is {int}\"");
        edits[0].Range.Start.Line.Should().Be(6, "the attribute literal lives on 0-based line 6");
    }

    // ── Multiple same-type attributes on one method are disambiguated by the resolved
    //    binding's expression, not by source position. ─────────────────────────────────

    [Fact]
    public async Task Rename_multi_attribute_method_edits_only_the_selected_attribute_literal()
    {
        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"alpha {int}\")]\n" +    // 0-based line 6
            "        [Given(\"beta {int}\")]\n" +     // 0-based line 7
            "        public void M(int x) { }\n" +    // 0-based line 8 → 1-based 9
            "    }\n" +
            "}\n";
        SetupBuffer(csText);

        var impl = new ProjectBindingImplementation("Steps.M()", null, new SourceLocation(CsPath, 9, 9));
        var alpha = new ProjectStepDefinitionBinding(
            ScenarioBlock.Given, new Regex("^alpha (.*)$"), null, impl, "alpha {int}");
        var beta = new ProjectStepDefinitionBinding(
            ScenarioBlock.Given, new Regex("^beta (.*)$"), null, impl, "beta {int}");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { alpha, beta }));

        var sut = CreateSut();

        // Pick the second attribute (beta) on this method.
        await sut.HandleSelectRenameTargetAsync(
            new SelectRenameTargetParams { Uri = CsUri.ToString(), Version = 0, AttributeIndex = 1 },
            CancellationToken.None);

        var result = await sut.HandleRenameAsync(
            RenameAt(line: 8, character: 8, newName: "gamma {int}"),
            CancellationToken.None);

        result.Should().NotBeNull();
        var edits = result!.Changes![CsUri].ToList();
        edits.Should().ContainSingle();
        edits[0].NewText.Should().Be("\"gamma {int}\"");
        edits[0].Range.Start.Line.Should().Be(7, "the selected (beta) attribute literal is on 0-based line 7");
    }

    // ── Cucumber parameter types must survive the write-back, even when the dialog seeded
    //    (and the user edited) the regex projection of the expression. ────────────────────

    [Fact]
    public async Task Rename_retains_original_cucumber_parameter_type_when_new_name_uses_regex_form()
    {
        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"the first number is {int}\")]\n" +          // 0-based line 6
            "        public void GivenTheFirstNumberIs(int number) { }\n" +
            "    }\n" +
            "}\n";
        SetupBuffer(csText);

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the first number is (.*)$"),
            specifiedExpression: "the first number is (.*)",
            line: 8, column: 9);
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        // The user edited the regex-form seed (number → no), keeping the (.*) slot.
        var result = await CreateSut().HandleRenameAsync(
            RenameAt(line: 7, character: 8, newName: "the first no is (.*)"),
            CancellationToken.None);

        result.Should().NotBeNull();
        var edits = result!.Changes![CsUri].ToList();
        edits.Should().ContainSingle();
        edits[0].NewText.Should().Be("\"the first no is {int}\"",
            "the original Cucumber {int} parameter type is preserved, not the regex (.*) the user typed");
    }

    // ── Regression (issue #55): prepareRename for a .cs binding must return the string
    //    literal's INNER text range, excluding the surrounding quote characters. A whole-line
    //    (or whole-token, quotes-included) range used to seed the rename dialog with the quotes
    //    still visible; if the user left them untouched, `newName` arrived already quoted, and
    //    BuildCSharpEdit's unconditional `"` + text + `"` wrapping doubled them, producing a
    //    stray trailing quote in the attribute, e.g. [Given("foo bar"")]. ─────────────────────

    [Fact]
    public async Task PrepareRename_from_cs_returns_literal_inner_range_excluding_quotes()
    {
        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"the first number is {int}\")]\n" +          // 0-based line 6
            "        public void GivenTheFirstNumberIs(int number) { }\n" +
            "    }\n" +
            "}\n";
        SetupBuffer(csText);

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the first number is (.*)$"),
            specifiedExpression: "the first number is {int}",
            line: 8, column: 9);
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.GetProjectForUri(CsUri).Returns(project);
        _scopeManager.GetIndexedFeatureFiles(project).Returns(new List<string> { "/workspace/test.feature" });

        var result = await CreateSut().HandlePrepareRenameAsync(
            new PrepareRenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = CsUri },
                Position     = new Position(7, 8)
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsRange.Should().BeTrue(
            "a .cs-triggered prepareRename returns a bare Range — the buffer text at that range " +
            "already is the abstract expression, so no separate Placeholder is needed");
        var range = result.Range!;
        range.Start.Line.Should().Be(6, "the attribute literal lives on 0-based line 6");
        range.End.Line.Should().Be(6);

        var line = csText.Replace("\r\n", "\n").Split('\n')[6];
        line[range.Start.Character - 1].Should().Be('"',
            "the range must start right after the opening quote, not include it");
        line[range.End.Character].Should().Be('"',
            "the range must end right before the closing quote, not include it");
        line.Substring(range.Start.Character, range.End.Character - range.Start.Character)
            .Should().Be("the first number is {int}");
    }

    // ── renameTargets surfaces the live source expression so the dialog seeds the
    //    Cucumber form rather than the regex projection. ───────────────────────────────────

    [Fact]
    public async Task RenameTargets_reports_live_source_expression_not_the_regex_projection()
    {
        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"the first number is {int}\")]\n" +
            "        public void GivenTheFirstNumberIs(int number) { }\n" +
            "    }\n" +
            "}\n";
        SetupBuffer(csText);

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the first number is (.*)$"),
            specifiedExpression: "the first number is (.*)",
            line: 8, column: 9);
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var response = await CreateSut().HandleRenameTargetsAsync(
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = CsUri },
                Position     = new Position(7, 8)
            },
            CancellationToken.None);

        response.Should().NotBeNull();
        var target = response!.Targets.Should().ContainSingle().Subject;
        target.Expression.Should().Be("the first number is {int}");
        target.Label.Should().Be("Given the first number is {int}");
    }

    // ── ReconcileParameterTokens unit behaviour ─────────────────────────────────────────

    [Theory]
    // regex-form edit over a Cucumber source → Cucumber type retained
    [InlineData("the first number is {int}", "the first no is (.*)", "the first no is {int}")]
    // Cucumber-form edit over a Cucumber source → verbatim
    [InlineData("the first number is {int}", "the first no is {int}", "the first no is {int}")]
    // regex source stays regex
    [InlineData("the first number is (.*)", "the first no is (.*)", "the first no is (.*)")]
    // multiple params, mixed forms → each slot takes the source token positionally
    [InlineData("a {int} b {string}", "x (.*) y (.*)", "x {int} y {string}")]
    // no parameters → verbatim
    [InlineData("just text", "renamed text", "renamed text")]
    // slot-count mismatch → user text honoured verbatim
    [InlineData("a {int}", "a {int} {word}", "a {int} {word}")]
    public void ReconcileParameterTokens_preserves_original_slot_tokens(
        string source, string newName, string expected)
    {
        StepRenameHandler.ReconcileParameterTokens(source, newName).Should().Be(expected);
    }

    // ── End-to-end: a Scenario Outline placeholder usage must be preserved in the feature edit,
    //    not replaced by the binding's {int} placeholder. ─────────────────────────────────────

    [Fact]
    public async Task Rename_preserves_scenario_outline_placeholder_in_feature_edit()
    {
        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"the second number is {int}\")]\n" +     // 0-based line 6
            "        public void GivenTheSecondNumberIs(int number) { }\n" +
            "    }\n" +
            "}\n";

        // The feature step uses an Examples placeholder, which does not match the numeric regex.
        const string featureText =
            "Feature: F\n" +
            "Scenario Outline: x\n" +
            "\tGiven the second number is <secondNumber>\n";   // step text at line 2, chars 7..42
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/x.feature");

        SetupBuffers((CsUri, csText), (featureUri, featureText));

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the second number is (-?\\d+)$"),
            specifiedExpression: "the second number is {int}",
            line: 8, column: 9);
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var snapshot = new LspTextSnapshot(featureUri.ToString(), 1, featureText);
        var match = new StepBindingMatch(
            featureUri.ToString(),
            GherkinRange.FromPoint(snapshot, startOffset: 38, length: 35),
            MatchResult.NoMatch);
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { match });

        var result = await CreateSut().HandleRenameAsync(
            RenameAt(line: 7, character: 8, newName: "the second no is {int}"),
            CancellationToken.None);

        result.Should().NotBeNull();
        var featureEdits = result!.Changes![featureUri].ToList();
        featureEdits.Should().ContainSingle();
        featureEdits[0].NewText.Should().Be("the second no is <secondNumber>",
            "the outline placeholder is preserved; the binding's {int} token must not leak into the feature");
    }

    // ── Feature-file renaming tests ─────────────────────────────────────────────
    // These cover the three new code paths: HandleRenameTargetsFromFeatureAsync,
    // FindBindingsAtFeatureStep, and the .feature branch of HandleRenameAsync.

    private static FeatureBindingMatchSet MakeFeatureMatchSet(
        string featureUri, ProjectStepDefinitionBinding binding,
        string scenarioBlock, string stepText, int stepLine, int stepChar)
    {
        var text = $"Feature: F\nScenario: S\n\t{scenarioBlock} {stepText}\n";
        var snapshot = new LspTextSnapshot(featureUri, 1, text);
        var stepPrefix = $"\t{scenarioBlock} ";
        var startOffset = text.IndexOf(stepPrefix + stepText) + stepPrefix.Length;
        var range = GherkinRange.FromPoint(snapshot, startOffset: startOffset, length: stepText.Length);
        var match = new StepBindingMatch(
            featureUri,
            range,
            MatchResult.CreateMultiMatch(new[]
            {
                MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch)
            }));

        return new FeatureBindingMatchSet(
            featureUri,
            new ProjectOwner("/workspace/MyProject.csproj", ".NETCoreApp,Version=v8.0"),
            1, 1,
            new[] { match });
    }

    // Builds a match set where the step is genuinely ambiguous (2+ matching bindings), mirroring
    // how ProjectBindingRegistry.cs:123 turns multiple Defined items into Ambiguous ones via
    // CloneToAmbiguousItem() — MatchResultItem.CreateMatch alone always produces Type.Defined.
    private static FeatureBindingMatchSet MakeAmbiguousFeatureMatchSet(
        string featureUri, ProjectStepDefinitionBinding[] bindings,
        string scenarioBlock, string stepText, int stepLine, int stepChar)
    {
        var text = $"Feature: F\nScenario: S\n\t{scenarioBlock} {stepText}\n";
        var snapshot = new LspTextSnapshot(featureUri, 1, text);
        var stepPrefix = $"\t{scenarioBlock} ";
        var startOffset = text.IndexOf(stepPrefix + stepText) + stepPrefix.Length;
        var range = GherkinRange.FromPoint(snapshot, startOffset: startOffset, length: stepText.Length);
        var items = bindings
            .Select(b => MatchResultItem.CreateMatch(b, ParameterMatch.NotMatch).CloneToAmbiguousItem())
            .ToArray();
        var match = new StepBindingMatch(featureUri, range, MatchResult.CreateMultiMatch(items));

        return new FeatureBindingMatchSet(
            featureUri,
            new ProjectOwner("/workspace/MyProject.csproj", ".NETCoreApp,Version=v8.0"),
            1, 1,
            new[] { match });
    }

    private static LspReqnrollProject MakeTestProject() =>
        new(
            new ReqnrollProjectLoadedParams
            {
                ProjectFile = "/workspace/MyProject.csproj",
                ProjectFolder = "/workspace",
                TargetFrameworkMoniker = ".NETCoreApp,Version=v8.0"
            },
            Substitute.For<Reqnroll.IdeSupport.Common.IIdeScope>());

    // ── Regression: prepareRename for a .feature step must return the step-text-only range
    //    (excluding the keyword and leading indentation), matching the range HandleRenameAsync
    //    later applies the edit at (usage.Range). A whole-line range used to seed the rename
    //    dialog with "\tThen the result should be 120"; submitting an edited version of that back
    //    duplicated the keyword when the edit was applied only at the step-text span, producing
    //    "\tThen \tThen the result should be 120" in the feature file. ──────────────────────────

    [Fact]
    public async Task PrepareRename_from_feature_returns_step_text_range_excluding_keyword_and_indentation()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var binding = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^to be or not to be$"),
            specifiedExpression: "to be or not to be",
            line: 8, column: 9,
            method: "Steps.ThenToBeOrNotToBe()");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });
        _scopeManager.GetProjectForUri(featureUri).Returns(project);

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "Then", "to be or not to be", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var result = await CreateSut().HandlePrepareRenameAsync(
            new PrepareRenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 10)
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsPlaceholderRange.Should().BeTrue(
            "a .feature-triggered prepareRename must seed the client with the abstract expression " +
            "as Placeholder, not the concrete step text, so newName always comes back unambiguous");
        result.PlaceholderRange!.Placeholder.Should().Be("to be or not to be",
            "the placeholder is the binding's abstract expression, not whatever is literally in the buffer");

        // MakeFeatureMatchSet builds the line as "\tThen to be or not to be" — the step text
        // starts right after the tab + "Then " (6 chars), not at column 0.
        var range = result.PlaceholderRange.Range!;
        range.Start.Line.Should().Be(2);
        range.Start.Character.Should().Be(6,
            "the range must start at the step text, excluding the keyword and indentation");
        range.End.Character.Should().NotBe(200,
            "a synthetic whole-line range was the bug this regression guards against");
    }

    // ── Regression (issue #47): prepareRename at a position with no renameable binding must
    //    return null quietly rather than throwing. The textDocument/prepareRename JSON-RPC
    //    handler in LanguageServerOptionsExtensions passes this return value straight through
    //    (LspRange?), so vscode-languageclient can treat it as "rename not supported here" and
    //    stay silent instead of surfacing a raw exception popup. ─────────────────────────────

    [Fact]
    public async Task PrepareRename_from_feature_returns_null_when_no_binding_matches_the_step()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(Array.Empty<ProjectStepDefinitionBinding>()));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });
        _scopeManager.GetProjectForUri(featureUri).Returns(project);
        _scopeManager.GetIndexedFeatureFiles(project).Returns(new List<string> { featureUri.GetFileSystemPath()! });

        // No match set registered for this key → FindBindingsAtFeatureStep finds nothing.
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(false);

        var result = await CreateSut().HandlePrepareRenameAsync(
            new PrepareRenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 10)
            },
            CancellationToken.None);

        result.Should().BeNull("rename is not available at this position and must be a silent no-op, not an exception");
    }

    [Fact]
    public async Task RenameTargets_from_feature_returns_matched_binding()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var binding = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^to be or not to be$"),
            specifiedExpression: "to be or not to be",
            line: 8, column: 9,
            method: "Steps.ThenToBeOrNotToBe()");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "Then", "to be or not to be", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var response = await CreateSut().HandleRenameTargetsAsync(
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14)
            },
            CancellationToken.None);

        response.Should().NotBeNull();
        var target = response!.Targets.Should().ContainSingle().Subject;
        target.Expression.Should().Be("to be or not to be");
        target.Label.Should().Be("Then to be or not to be — Steps.ThenToBeOrNotToBe()");
    }

    [Fact]
    public async Task RenameTargets_from_feature_no_match_returns_empty_response()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        _scopeManager.ResolveOwners(featureUri).Returns(Array.Empty<LspReqnrollProject>());

        var response = await CreateSut().HandleRenameTargetsAsync(
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14)
            },
            CancellationToken.None);

        response.Should().NotBeNull();
        response!.Targets.Should().BeEmpty();
    }

    // ── Regression: an ambiguous feature step (2+ matching bindings) must still surface
    //    every candidate through the rename-targets picker, and prepareRename must not report
    //    "no defined binding" for it. MatchResult.HasDefined is false for ambiguous steps (their
    //    items are typed Ambiguous, not Defined) — FindBindingsAtFeatureStep used to gate on
    //    HasDefined alone, silently excluding exactly the steps this feature exists to handle. ──

    [Fact]
    public async Task RenameTargets_from_feature_returns_all_targets_for_ambiguous_step()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var bindingInt = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^the result should be (-?\\d+)$"),
            specifiedExpression: "the result should be {int}",
            line: 8, column: 9,
            method: "Steps.ThenResultInt(Int32)");
        var bindingAny = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^the result should be (.*)$"),
            specifiedExpression: "the result should be (.*)",
            line: 10, column: 9,
            method: "Steps.ThenResultAny(String)");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { bindingInt, bindingAny }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeAmbiguousFeatureMatchSet(
            featureUri.ToString(), new[] { bindingInt, bindingAny },
            "Then", "the result should be 120", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var response = await CreateSut().HandleRenameTargetsAsync(
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14)
            },
            CancellationToken.None);

        response.Should().NotBeNull();
        response!.Targets.Should().HaveCount(2,
            "both ambiguous bindings must be offered, not silently dropped because neither is individually 'Defined'");
        response.Targets.Select(t => t.Expression).Should()
            .BeEquivalentTo(new[] { "the result should be {int}", "the result should be (.*)" });
        response.Targets.Select(t => t.Label).Should().OnlyHaveUniqueItems(
            "identical-looking picker entries leave the user unable to tell which binding they're choosing — " +
            "the implementing method must be part of the label");
        response.Targets.Should().Contain(t => t.Label.EndsWith("Steps.ThenResultInt(Int32)"));
        response.Targets.Should().Contain(t => t.Label.EndsWith("Steps.ThenResultAny(String)"));
    }

    [Fact]
    public async Task PrepareRename_from_feature_succeeds_for_ambiguous_step()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var bindingInt = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^the result should be (-?\\d+)$"),
            specifiedExpression: "the result should be {int}",
            line: 8, column: 9,
            method: "Steps.ThenResultInt(Int32)");
        var bindingAny = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^the result should be (.*)$"),
            specifiedExpression: "the result should be (.*)",
            line: 10, column: 9,
            method: "Steps.ThenResultAny(String)");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { bindingInt, bindingAny }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });
        _scopeManager.GetProjectForUri(featureUri).Returns(project);

        var matchSet = MakeAmbiguousFeatureMatchSet(
            featureUri.ToString(), new[] { bindingInt, bindingAny },
            "Then", "the result should be 120", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var result = await CreateSut().HandlePrepareRenameAsync(
            new PrepareRenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 10)
            },
            CancellationToken.None);

        result.Should().NotBeNull(
            "an ambiguous-but-defined step must still offer rename — this used to return null " +
            "and the server's null->throw wiring surfaced that as a visible client error popup");
    }

    [Fact]
    public async Task SelectRenameTarget_then_Rename_from_feature_edits_only_selected_ambiguous_binding()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var csUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");

        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Then(\"the result should be {int}\")]\n" +   // 0-based line 6
            "        public void ThenResultInt(int result) { }\n" + // 0-based line 7
            "        [Then(\"the result should be (.*)\")]\n" +     // 0-based line 8
            "        public void ThenResultAny(string result) { }\n" + // 0-based line 9
            "    }\n" +
            "}\n";
        SetupBuffers((csUri, csText));

        var bindingInt = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^the result should be (-?\\d+)$"),
            specifiedExpression: "the result should be {int}",
            line: 8, column: 9,
            method: "Steps.ThenResultInt(Int32)");
        var bindingAny = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^the result should be (.*)$"),
            specifiedExpression: "the result should be (.*)",
            line: 10, column: 9,
            method: "Steps.ThenResultAny(String)");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { bindingInt, bindingAny }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeAmbiguousFeatureMatchSet(
            featureUri.ToString(), new[] { bindingInt, bindingAny },
            "Then", "the result should be 120", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var sut = CreateSut();

        // Discover the target index for the "(.*)" binding rather than assuming ordering —
        // FindBindingsAtFeatureStep collects candidates via a HashSet, so index isn't contractual.
        var targets = await sut.HandleRenameTargetsAsync(
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14)
            },
            CancellationToken.None);
        var anyTarget = targets!.Targets.Should()
            .ContainSingle(t => t.Expression == "the result should be (.*)").Subject;

        await sut.HandleSelectRenameTargetAsync(
            new SelectRenameTargetParams { Uri = featureUri.ToString(), Version = 0, AttributeIndex = anyTarget.AttributeIndex },
            CancellationToken.None);

        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14),
                NewName = "the total should be (.*)"
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Changes!.Should().ContainKey(csUri);
        var csEdit = result.Changes[csUri].ToList();
        csEdit.Should().ContainSingle();
        csEdit[0].NewText.Should().Be("\"the total should be (.*)\"",
            "only the selected (ThenResultAny) attribute should be edited");
        csEdit[0].Range.Start.Line.Should().Be(8,
            "the edit must land on the ThenResultAny attribute line, not ThenResultInt's");
    }

    // ── Regression: a namespace-qualified method name (as real discovery actually produces,
    //    e.g. "MyProj.StepDefinitions.CalculatorSteps.GivenX(Int32)") must not dominate the
    //    picker label — two ambiguous bindings in the same project share that namespace prefix,
    //    so keeping it pushes the only actually-distinguishing part (class + method) past the
    //    picker's visible width before the two labels diverge. ─────────────────────────────────

    [Fact]
    public async Task RenameTargets_from_feature_label_omits_namespace_from_method_qualifier()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var bindingInt = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the first number is (-?\\d+)$"),
            specifiedExpression: "the first number is {int}",
            line: 8, column: 9,
            method: "Minimal.StepDefinitions.CalculatorStepDefinitions.GivenTheFirstNumberIs(Int32)");
        var bindingAny = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^the first number is (.*)$"),
            specifiedExpression: "the first number is {int}",
            line: 10, column: 9,
            method: "Minimal.StepDefinitions.OtherStepDefinitions.GivenTheFirstNumberIs(Int32)");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { bindingInt, bindingAny }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeAmbiguousFeatureMatchSet(
            featureUri.ToString(), new[] { bindingInt, bindingAny },
            "Given", "the first number is 50", stepLine: 2, stepChar: 6);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var response = await CreateSut().HandleRenameTargetsAsync(
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14)
            },
            CancellationToken.None);

        response.Should().NotBeNull();
        response!.Targets.Should().HaveCount(2);
        response.Targets.Should().OnlyContain(t => !t.Label.Contains("Minimal.StepDefinitions"),
            "the shared namespace prefix must be dropped — it doesn't help distinguish ambiguous bindings and crowds out the part that does");
        response.Targets.Should().Contain(t => t.Label.EndsWith("CalculatorStepDefinitions.GivenTheFirstNumberIs(Int32)"));
        response.Targets.Should().Contain(t => t.Label.EndsWith("OtherStepDefinitions.GivenTheFirstNumberIs(Int32)"));
    }

    [Fact]
    public async Task Rename_from_feature_edits_both_feature_text_and_csharp_attribute()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var csUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");

        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Then(\"to be or not to be\")]\n" +
            "        public void ThenToBeOrNotToBe() { }\n" +
            "    }\n" +
            "}\n";

        SetupBuffers((csUri, csText));

        var binding = MakeBinding(
            ScenarioBlock.Then,
            new Regex("^to be or not to be$"),
            specifiedExpression: "to be or not to be",
            line: 8, column: 9,
            method: "Steps.ThenToBeOrNotToBe()");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "Then", "to be or not to be", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        const string featureText = "Feature: F\nScenario: S\n\tThen to be or not to be\n";
        var snapshot = new LspTextSnapshot(featureUri.ToString(), 1, featureText);
        const string stepText = "to be or not to be";
        var stepOffset = featureText.IndexOf("\tThen " + stepText) + "\tThen ".Length;
        var usageMatch = new StepBindingMatch(
            featureUri.ToString(),
            GherkinRange.FromPoint(snapshot, startOffset: stepOffset, length: stepText.Length),
            MatchResult.CreateMultiMatch(new[]
            {
                MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch)
            }));
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { usageMatch });

        var sut = CreateSut();
        await sut.HandleSelectRenameTargetAsync(
            new SelectRenameTargetParams { Uri = featureUri.ToString(), Version = 0, AttributeIndex = 0 },
            CancellationToken.None);

        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 14),
                NewName = "to be and not to be"
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Changes!.Should().ContainKey(featureUri);
        result.Changes.Should().ContainKey(csUri);

        var featureEdit = result.Changes[featureUri].ToList();
        featureEdit.Should().ContainSingle();
        featureEdit[0].NewText.Should().Be("to be and not to be");

        var csEdit = result.Changes[csUri].ToList();
        csEdit.Should().ContainSingle();
        csEdit[0].NewText.Should().Be("\"to be and not to be\"");
    }

    [Fact]
    public async Task Rename_from_feature_without_session_resolves_binding_via_match_cache()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var csUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");

        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"I press add\")]\n" +
            "        public void GivenIPressAdd() { }\n" +
            "    }\n" +
            "}\n";

        SetupBuffers((csUri, csText));

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^I press add$"),
            specifiedExpression: "I press add",
            line: 8, column: 9,
            method: "Steps.GivenIPressAdd()");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "Given", "I press add", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        const string featureText = "Feature: F\nScenario: S\n\tGiven I press add\n";
        var snapshot = new LspTextSnapshot(featureUri.ToString(), 1, featureText);
        const string stepText = "I press add";
        var stepOffset = featureText.IndexOf("\tGiven " + stepText) + "\tGiven ".Length;
        var usageMatch = new StepBindingMatch(
            featureUri.ToString(),
            GherkinRange.FromPoint(snapshot, startOffset: stepOffset, length: stepText.Length),
            MatchResult.CreateMultiMatch(new[]
            {
                MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch)
            }));
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { usageMatch });

        var sut = CreateSut();

        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 10),
                NewName = "I choose add"
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Changes!.Should().ContainKey(featureUri);
        result.Changes.Should().ContainKey(csUri);
    }

    // ── Regression: VS Code seeds the .feature rename dialog with the step's concrete text
    //    (real parameter values), not the abstract expression, since prepareRename returns a
    //    whole-line range. A rename submitted from the feature file must therefore be
    //    reconciled back to an abstract expression before validation/propagation — otherwise
    //    the parameter-count check always fails and the rename silently no-ops. ──────────────

    [Fact]
    public async Task Rename_from_feature_with_concrete_parameter_value_updates_feature_and_csharp()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var csUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");

        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"I have {int} cukes\")]\n" +
            "        public void GivenIHaveCukes(int count) { }\n" +
            "    }\n" +
            "}\n";

        const string featureText = "Feature: F\nScenario: S\n\tGiven I have 5 cukes\n";
        SetupBuffers((csUri, csText), (featureUri, featureText));

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^I have (-?\\d+) cukes$"),
            specifiedExpression: "I have {int} cukes",
            line: 8, column: 9,
            method: "Steps.GivenIHaveCukes()");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "Given", "I have 5 cukes", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var snapshot = new LspTextSnapshot(featureUri.ToString(), 1, featureText);
        const string stepText = "I have 5 cukes";
        var stepOffset = featureText.IndexOf("\tGiven " + stepText) + "\tGiven ".Length;
        var usageMatch = new StepBindingMatch(
            featureUri.ToString(),
            GherkinRange.FromPoint(snapshot, startOffset: stepOffset, length: stepText.Length),
            MatchResult.CreateMultiMatch(new[]
            {
                MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch)
            }));
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { usageMatch });

        var sut = CreateSut();

        // Simulates F2 on "cukes": VS Code seeds the dialog with the whole concrete line and the
        // user edits only the static wording, keeping the parameter value (5) untouched.
        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 15),
                NewName = "I have 5 pickles"
            },
            CancellationToken.None);

        result.Should().NotBeNull("a parameterized step rename from the .feature file must not silently no-op");
        result!.Changes!.Should().ContainKey(featureUri);
        result.Changes.Should().ContainKey(csUri);

        var featureEdit = result.Changes[featureUri].ToList();
        featureEdit.Should().ContainSingle();
        featureEdit[0].NewText.Should().Be("I have 5 pickles",
            "the concrete parameter value must be preserved in the feature file");

        var csEdit = result.Changes[csUri].ToList();
        csEdit.Should().ContainSingle();
        csEdit[0].NewText.Should().Be("\"I have {int} pickles\"",
            "the {int} parameter type must be preserved in the binding attribute, not the concrete value 5");
    }

    [Fact]
    public async Task Rename_from_feature_with_already_abstract_new_name_is_used_as_is()
    {
        // VS's custom "Rename Step" command (RenameStepCommand.cs) seeds its own prompt with the
        // binding's abstract expression (placeholders intact) regardless of whether the cursor was
        // in the .cs or .feature file, then submits that abstract text verbatim as `newName`. This
        // must not be mistaken for VS Code's concrete-text submission and rejected/mangled by the
        // parameter-value reconciliation added for that case.
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var csUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");

        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [Given(\"I have {int} cukes\")]\n" +
            "        public void GivenIHaveCukes(int count) { }\n" +
            "    }\n" +
            "}\n";

        const string featureText = "Feature: F\nScenario: S\n\tGiven I have 5 cukes\n";
        SetupBuffers((csUri, csText), (featureUri, featureText));

        var binding = MakeBinding(
            ScenarioBlock.Given,
            new Regex("^I have (-?\\d+) cukes$"),
            specifiedExpression: "I have {int} cukes",
            line: 8, column: 9,
            method: "Steps.GivenIHaveCukes()");
        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "Given", "I have 5 cukes", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        var snapshot = new LspTextSnapshot(featureUri.ToString(), 1, featureText);
        const string stepText = "I have 5 cukes";
        var stepOffset = featureText.IndexOf("\tGiven " + stepText) + "\tGiven ".Length;
        var usageMatch = new StepBindingMatch(
            featureUri.ToString(),
            GherkinRange.FromPoint(snapshot, startOffset: stepOffset, length: stepText.Length),
            MatchResult.CreateMultiMatch(new[]
            {
                MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch)
            }));
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { usageMatch });

        var sut = CreateSut();

        // Simulates VS's custom command: the prompt was seeded with, and the user edited, the
        // abstract expression "I have {int} cukes" directly — not the concrete feature line.
        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 15),
                NewName = "I have {int} pickles"
            },
            CancellationToken.None);

        result.Should().NotBeNull("an already-abstract newName must not be rejected as an unreconcilable parameter-value change");
        result!.Changes!.Should().ContainKey(featureUri);
        result.Changes.Should().ContainKey(csUri);

        var featureEdit = result.Changes[featureUri].ToList();
        featureEdit.Should().ContainSingle();
        featureEdit[0].NewText.Should().Be("I have 5 pickles",
            "the concrete parameter value must still be preserved in the feature file");

        var csEdit = result.Changes[csUri].ToList();
        csEdit.Should().ContainSingle();
        csEdit[0].NewText.Should().Be("\"I have {int} pickles\"");
    }

    [Fact]
    public async Task FindAttributeLiteralAsync_redirects_from_feature_to_csharp_source()
    {
        var featureUri = DocumentUri.FromFileSystemPath("/workspace/test.feature");
        var csUri = DocumentUri.FromFileSystemPath("/workspace/Steps.cs");
        var csPath = csUri.GetFileSystemPath();

        const string csText =
            "using Reqnroll;\n" +
            "namespace N\n" +
            "{\n" +
            "    [Binding]\n" +
            "    public class Steps\n" +
            "    {\n" +
            "        [When(\"something happens\")]\n" +
            "        public void WhenSomethingHappens() { }\n" +
            "    }\n" +
            "}\n";

        SetupBuffers((csUri, csText));

        var binding = MakeBinding(
            ScenarioBlock.When,
            new Regex("^something happens$"),
            specifiedExpression: "something happens",
            line: 8, column: 9,
            method: "Steps.WhenSomethingHappens()");

        binding = new ProjectStepDefinitionBinding(
            binding.StepDefinitionType,
            binding.Regex,
            binding.Scope,
            new ProjectBindingImplementation(
                "Steps.WhenSomethingHappens()",
                null,
                new SourceLocation(csPath, 8, 9)),
            binding.Expression);

        _registryLookup.GetRegistryForUri(Arg.Any<DocumentUri>())
                       .Returns(ProjectBindingRegistry.FromBindings(new[] { binding }));

        var project = MakeTestProject();
        _scopeManager.ResolveOwners(featureUri).Returns(new[] { project });

        var matchSet = MakeFeatureMatchSet(
            featureUri.ToString(), binding,
            "When", "something happens", stepLine: 2, stepChar: 5);
        _matchService.TryGet(Arg.Any<MatchSetKey>(), out Arg.Any<FeatureBindingMatchSet>())
            .Returns(ci =>
            {
                ci[1] = matchSet;
                return true;
            });

        const string whenFeatureText = "Feature: F\nScenario: S\n\tWhen something happens\n";
        const string whenStepText = "something happens";
        var whenStepOffset = whenFeatureText.IndexOf("\tWhen " + whenStepText) + "\tWhen ".Length;
        var usageMatch = new StepBindingMatch(
            featureUri.ToString(),
            GherkinRange.FromPoint(
                new LspTextSnapshot(featureUri.ToString(), 1, whenFeatureText),
                startOffset: whenStepOffset, length: whenStepText.Length),
            MatchResult.CreateMultiMatch(new[] { MatchResultItem.CreateMatch(binding, ParameterMatch.NotMatch) }));
        _matchService.FindUsages(Arg.Any<SourceLocation>(), Arg.Any<IReadOnlyCollection<ProjectOwner>>())
                     .Returns(new[] { usageMatch });

        var sut = CreateSut();
        await sut.HandleSelectRenameTargetAsync(
            new SelectRenameTargetParams { Uri = featureUri.ToString(), Version = 0, AttributeIndex = 0 },
            CancellationToken.None);

        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = featureUri },
                Position = new Position(2, 10),
                NewName = "something changed"
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Changes!.Should().ContainKey(csUri);

        var csEdit = result.Changes[csUri].ToList();
        csEdit.Should().ContainSingle();
        csEdit[0].NewText.Should().Be("\"something changed\"");
        csEdit[0].Range.Start.Line.Should().Be(6, "the [When] attribute literal is on 0-based line 6");
    }

    // ── Telemetry ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleRenameAsync_emits_rename_telemetry_on_success()
    {
        // Arrange — set up a minimal rename that produces an edit
        const string csText = """
            using Reqnroll;
            namespace S;
            class C
            {
                [When("something")]
                void M() { }
            }
            """;
        var binding = new ProjectStepDefinitionBinding(
            ScenarioBlock.When,
            new Regex("^something$"),
            null,
            new ProjectBindingImplementation("C.M", null, new SourceLocation(CsPath, 5, 1)),
            "something");
        var registry = ProjectBindingRegistry.FromBindings(new[] { binding });
        _registryLookup.GetRegistryForUri(CsUri).Returns(registry);
        SetupBuffer(csText);

        var telemetry = Substitute.For<ILspTelemetryService>();
        var sut = CreateSutWithTelemetry(telemetry);

        var result = await sut.HandleRenameAsync(
            new RenameParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = CsUri },
                Position = new Position(4, 0),
                NewName = "something changed"
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        telemetry.Received(1).SendEvent(
            "Rename step command executed",
            Arg.Is<Dictionary<string, object?>>(d => false.Equals(d["Erroneous"])));
    }
}
