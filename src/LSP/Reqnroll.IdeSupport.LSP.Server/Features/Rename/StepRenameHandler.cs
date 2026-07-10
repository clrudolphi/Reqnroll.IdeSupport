#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Reqnroll.IdeSupport.Common;
using Reqnroll.IdeSupport.Common.Logging;
using Reqnroll.IdeSupport.LSP.Core.Bindings;
using Reqnroll.IdeSupport.LSP.Core.Documents;


using Reqnroll.IdeSupport.LSP.Core.Gherkin.Parsing;


using Reqnroll.IdeSupport.LSP.Core.Matching;
using Reqnroll.IdeSupport.LSP.Core.Rename;
using Reqnroll.IdeSupport.LSP.Server.Discovery;
using Reqnroll.IdeSupport.LSP.Server.Registry;
using Reqnroll.IdeSupport.LSP.Server.Protocol.Documents;
using Reqnroll.IdeSupport.LSP.Server.Hosting;
using Reqnroll.IdeSupport.LSP.Server.Protocol;
using Reqnroll.IdeSupport.LSP.Server.Features.TextSync;
using Reqnroll.IdeSupport.LSP.Server.Telemetry;
using Reqnroll.IdeSupport.LSP.Server.Workspace;

namespace Reqnroll.IdeSupport.LSP.Server.Features.Rename;

/// <summary>
/// Handles <c>textDocument/prepareRename</c>, <c>textDocument/rename</c>,
/// <c>reqnroll/renameTargets</c>, and <c>reqnroll/selectRenameTarget</c> for the
/// F16 Step Rename Refactoring feature.
/// </summary>
public sealed class StepRenameHandler
{
    private readonly IBindingMatchService          _matchService;
    private readonly ILspWorkspaceScopeManager     _scopeManager;
    private readonly IProjectBindingRegistryLookup _registryLookup;
    private readonly IIdeSupportLogger               _logger;
    private readonly IDocumentBufferService        _documentBuffer;
    private readonly RenameSessionManager          _sessionManager;
    private readonly ICSharpFileTextCache          _csharpFileTextCache;
    private readonly ICSharpBindingDiscoveryService _csharpDiscoveryService;
    private readonly ILanguageServerFacade         _languageServer;
    private readonly ClientIdeContext              _clientIdeContext;
    private readonly ILspTelemetryService?         _telemetryService;

    public StepRenameHandler(
        IBindingMatchService          matchService,
        ILspWorkspaceScopeManager     scopeManager,
        IProjectBindingRegistryLookup registryLookup,
        IIdeSupportLogger               logger,
        IDocumentBufferService        documentBuffer,
        ICSharpFileTextCache          csharpFileTextCache,
        ICSharpBindingDiscoveryService csharpDiscoveryService,
        ILanguageServerFacade         languageServer,
        ClientIdeContext              clientIdeContext,
        ILspTelemetryService?         telemetryService = null)
    {
        _matchService    = matchService;
        _scopeManager    = scopeManager;
        _registryLookup  = registryLookup;
        _logger          = logger;
        _documentBuffer  = documentBuffer;
        _sessionManager  = new RenameSessionManager();
        _csharpFileTextCache = csharpFileTextCache;
        _csharpDiscoveryService = csharpDiscoveryService;
        _languageServer  = languageServer;
        _clientIdeContext = clientIdeContext;
        _telemetryService = telemetryService;
    }

    // ── textDocument/prepareRename ──────────────────────────────────────────────

    /// <summary>
    /// Validates that the cursor is on a renameable binding. Returns the range of the
    /// renameable text (attribute string or step text) — for <c>.feature</c> files, paired
    /// with a <see cref="PlaceholderRange.Placeholder"/> carrying the binding's abstract
    /// expression (e.g. <c>"the second number is {int}"</c>) instead of the concrete step
    /// text — or <c>null</c> if rename is not available at this position.
    /// </summary>
    /// <remarks>
    /// Seeding the rename box with the abstract expression, rather than the concrete text
    /// literally in the buffer, is deliberate (issue #33 follow-up): a spec-compliant client
    /// has no buffer text to anchor a partial in-place edit against when the placeholder
    /// text doesn't appear in the document, so it can only submit the box's full edited
    /// content as <c>newName</c> — turning an inherently ambiguous "did the user edit the
    /// wording, the parameter value, or an arbitrary fragment?" problem into an unambiguous
    /// one: <c>newName</c> is always the complete new abstract expression.
    /// </remarks>
    public async Task<RangeOrPlaceholderRange?> HandlePrepareRenameAsync(
        PrepareRenameParams request,
        CancellationToken   cancellationToken)
    {
        var uri  = request.TextDocument.Uri;
        var path = uri.GetFileSystemPath();

        if (string.IsNullOrEmpty(path))
            return null;

        // Rule 1: validate cursor position (file type)
        var posError = StepRenameValidator.ValidateCursorPosition((Uri)uri);
        if (posError != null)
        {
            _logger.LogVerbose($"StepRenameHandler: prepareRename — {posError.Message}");
            return null;
        }

        // Rule 7: validate project state via the registry lookup
        var registry = _registryLookup.GetRegistryForUri(uri);
        bool isInitialized = registry != ProjectBindingRegistry.Invalid;
        bool hasFeatureFiles = false;

        if (isInitialized)
        {
            var project = _scopeManager.GetProjectForUri(uri);
            hasFeatureFiles = project != null &&
                (_scopeManager.GetIndexedFeatureFiles(project).Count > 0
                 || _scopeManager.ResolveOwners(uri).Count > 0);
        }

        var projError = StepRenameValidator.ValidateProjectState(isInitialized, hasFeatureFiles);
        if (projError != null)
        {
            _logger.LogVerbose($"StepRenameHandler: prepareRename — {projError.Message}");
            return null;
        }

        // For .cs files: check if the cursor resolves to a single binding
        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            var line   = request.Position.Line + 1;
            var column = request.Position.Character + 1;
            var bindingLocation = new SourceLocation(path, line, column);

            if (registry == ProjectBindingRegistry.Invalid)
                return null;

            var binding = registry.FindBindingAtLocation(bindingLocation);
            if (binding == null)
                return null;

            // Rule 2: validate expression is a string literal
            var exprError = StepRenameValidator.ValidateExpressionIsStringLiteral(binding.Expression);
            if (exprError != null)
            {
                _logger.LogVerbose($"StepRenameHandler: prepareRename — {exprError.Message}");
                return null;
            }

            // Return the range of the string literal's INNER text only, excluding the
            // surrounding quote characters. Returning the whole line/token (quotes included)
            // seeds the client's rename box with the quotes; if the user leaves them untouched
            // (a natural interaction — they look like part of the placeholder), `newName`
            // arrives already quoted, and BuildCSharpEdit's unconditional `"` + text + `"`
            // wrapping then doubles them, producing a stray trailing quote (issue #55).
            var literal = await FindAttributeLiteralAsync(uri, binding);
            if (literal == null)
            {
                _logger.LogVerbose("StepRenameHandler: prepareRename — could not resolve attribute literal for binding");
                return null;
            }

            return GetLiteralInnerRange(literal);
        }

        // For .feature files: only offer rename when the cursor is on a step that is
        // actually defined in the match cache.  Returning null here tells VS Code
        // "rename not available at this position" — same as prepareRename for a C# cursor
        // not on a binding attribute — which suppresses the rename dialog cleanly.
        // Without this check, prepareRename would succeed for undefined steps, and the
        // subsequent textDocument/rename would fail with "Internal Error".
        if (path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
        {
            var featureBindings = FindBindingsAtFeatureStep(uri, path, request.Position, out var stepRange);
            if (featureBindings.Count == 0)
            {
                _logger.LogVerbose("StepRenameHandler: prepareRename — no defined binding at feature step position");
                return null;
            }

            if (stepRange == null)
            {
                // Should not happen alongside a non-empty featureBindings, but refuse rather
                // than fall back to a whole-line range: that used to seed the dialog with the
                // keyword/indentation, which then got duplicated when the resulting edit was
                // applied at the step-text-only range HandleRenameAsync actually replaces.
                _logger.LogVerbose("StepRenameHandler: prepareRename — matched a binding but could not resolve the step's text range");
                return null;
            }

            // When ambiguous (2+ candidate bindings), a plain F2 rename would fall back to the
            // first candidate anyway (see HandleRenameAsync's position-based fallback) — pick
            // the same one here so the placeholder shown matches what would actually be renamed.
            var matchedBinding = featureBindings[0];
            var sourceLiteral  = await FindAttributeLiteralAsync(uri, matchedBinding);
            var sourceExpression = sourceLiteral?.Token.ValueText ?? matchedBinding.Expression ?? string.Empty;

            // Known cosmetic quirk (confirmed live in VS and VS Code, issue #33 follow-up): when
            // a user pre-selects a sub-span of the concrete step text before invoking F2 (e.g.
            // "added" in "the two numbers are added"), the client computes that selection's
            // offset relative to Range.Start and reapplies the same numeric offset into
            // Placeholder to decide what to pre-highlight in the rename box. Placeholder is a
            // different string than the concrete text whenever a parameter's rendered width
            // differs from its abstract token (here "are" → "{Verb}", +3 chars), so everything
            // after the parameter shifts and the pre-highlighted substring lands a few characters
            // off from what the user actually selected (e.g. "b} summed" instead of "summed").
            // This is inherent to the offset math being reused across two different-length
            // strings — the LSP PrepareRenameResult protocol has no field to specify which
            // sub-span of Placeholder to highlight independently of Range — and is harmless: the
            // box's full content is still correct, and whatever the user submits becomes newName
            // in full regardless of what was pre-highlighted.
            return new PlaceholderRange
            {
                Range       = stepRange,
                Placeholder = sourceExpression
            };
        }

        return null;
    }

    /// <summary>
    /// Computes the LSP range of a string literal's inner text, i.e. its <see cref="LiteralExpressionSyntax.Token"/>
    /// span with the surrounding quote characters excluded (2 leading characters for a verbatim
    /// string's <c>@"</c>, 1 otherwise; 1 trailing character for the closing <c>"</c>).
    /// </summary>
    private static LspRange GetLiteralInnerRange(LiteralExpressionSyntax literal)
    {
        var tokenText = literal.Token.Text;
        var leadingQuoteLength = tokenText.StartsWith("@\"", StringComparison.Ordinal) ? 2 : 1;
        const int trailingQuoteLength = 1;

        var fullSpan = literal.Token.Span;
        var innerSpan = new Microsoft.CodeAnalysis.Text.TextSpan(
            fullSpan.Start + leadingQuoteLength,
            fullSpan.Length - leadingQuoteLength - trailingQuoteLength);

        var lineSpan = literal.SyntaxTree!.GetLineSpan(innerSpan);
        var startPos = lineSpan.StartLinePosition;
        var endPos   = lineSpan.EndLinePosition;

        return new LspRange
        {
            Start = new Position(startPos.Line, startPos.Character),
            End   = new Position(endPos.Line, endPos.Character)
        };
    }

    // ── textDocument/rename ────────────────────────────────────────────────────

    /// <summary>
    /// Executes the rename. Validates the new name, resolves all feature step locations,
    /// resolves the C# attribute string range, and returns a WorkspaceEdit covering all files.
    /// </summary>
    public async Task<WorkspaceEdit?> HandleRenameAsync(
        RenameParams       request,
        CancellationToken   cancellationToken)
    {
        var uri  = request.TextDocument.Uri;
        var path = uri.GetFileSystemPath();
        var newName = request.NewName;

        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(newName))
            return null;

        _logger.LogVerbose($"StepRenameHandler: rename at {path}, newName='{newName}'");

        // ── 1. Resolve binding ─────────────────────────────────────────────────

        var line   = request.Position.Line + 1;
        var column = request.Position.Character + 1;

        var registry = _registryLookup.GetRegistryForUri(uri);
        if (registry == ProjectBindingRegistry.Invalid)
        {
            _logger.LogVerbose("StepRenameHandler: registry is invalid");
            return null;
        }

        // Check for a pending rename session (set by reqnroll/selectRenameTarget).
        // This handles the multi-attribute case where the cursor is not on a specific
        // attribute string — the picker pre-selected which binding to rename.
        ProjectStepDefinitionBinding? binding = null;
        int? pendingAttributeIndex = null;
        List<ProjectStepDefinitionBinding> bindingsAtLocation = new();

        // Use version from request or fallback to 0
        var documentVersion = 0;
        if (_sessionManager.TryConsume(uri.ToString(), documentVersion, out var sessionAttrIndex))
        {
            pendingAttributeIndex = sessionAttrIndex;
            _logger.LogVerbose($"StepRenameHandler: consumed pending session, attributeIndex={sessionAttrIndex}");
        }

        if (pendingAttributeIndex.HasValue)
        {
            if (path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
            {
                // For feature files, resolve bindings from the match cache
                bindingsAtLocation = FindBindingsAtFeatureStep(uri, path, position: request.Position);
            }
            else
            {
                // For C# files, find bindings at the method location in the registry
                bindingsAtLocation = registry.StepDefinitions
                    .Where(b => b.Implementation.SourceLocation != null &&
                                string.Equals(b.Implementation.SourceLocation.SourceFile, path, StringComparison.OrdinalIgnoreCase) &&
                                Math.Abs(b.Implementation.SourceLocation.SourceFileLine - line) <= 5)
                    .ToList();
            }

            if (pendingAttributeIndex.Value >= 0 && pendingAttributeIndex.Value < bindingsAtLocation.Count)
            {
                binding = bindingsAtLocation[pendingAttributeIndex.Value];
                _logger.LogVerbose($"StepRenameHandler: resolved binding via session: '{binding?.Expression}'");
            }
        }

        // Fall back to position-based resolution (single-binding case)
        if (binding == null && path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
        {
            var featureBindings = FindBindingsAtFeatureStep(uri, path, position: request.Position);
            binding = featureBindings.FirstOrDefault();
            if (binding != null)
                _logger.LogVerbose($"StepRenameHandler: resolved binding via feature match cache: '{binding.Expression}'");
        }
        binding ??= registry.FindBindingAtLocation(new SourceLocation(path, line, column));
        if (binding == null)
        {
            _logger.LogVerbose("StepRenameHandler: no binding at cursor position");
            return null;
        }

        SourceLocation bindingLocation;

        // Use the binding's C# source location for FindUsages so we can find
        // feature steps that reference this binding. For .feature-originated
        // renames, the request path is the .feature file, not the .cs file.
        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
            binding.Implementation?.SourceLocation?.SourceFile != null)
        {
            bindingLocation = new SourceLocation(
                binding.Implementation.SourceLocation.SourceFile,
                binding.Implementation.SourceLocation.SourceFileLine,
                binding.Implementation.SourceLocation.SourceFileColumn);
            _logger.LogVerbose($"StepRenameHandler: using binding source location for FindUsages: {bindingLocation}");
        }
        else
        {
            bindingLocation = new SourceLocation(path, line, column);
        }

        var expression = binding.Expression ?? string.Empty;

        // ── 2. Resolve feature step locations ──────────────────────────────────
        var owners = _scopeManager.ResolveOwners(uri);
        var projectFilter = owners.Count > 0
            ? owners.Select(p => new ProjectOwner(p.ProjectFullName, p.TargetFrameworkMoniker)).ToArray()
            : null;

        var usages = _matchService.FindUsages(bindingLocation, projectFilter);

        // Resolve the live source expression once (preserves the original parameter syntax).
        // For a .cs-invoked rename this is the attribute string literal; otherwise it falls back
        // to the registry expression. It anchors both the feature edits (static-segment
        // substitution) and the C# attribute edit.
        var sourceLiteral = await FindAttributeLiteralAsync(uri, binding);
        var sourceExpression = sourceLiteral?.Token.ValueText ?? expression;

        // A .feature-triggered rename can arrive in two shapes, both via the same
        // textDocument/rename call, with no protocol-level way to tell them apart:
        //  - VS Code's native F2 seeds the dialog via prepareRename's whole-line range, so
        //    `newName` comes back as concrete step text (real parameter values, e.g.
        //    "I have 10 cukes" rather than "I have {int} cukes"). Comparing that straight
        //    against the abstract expression always trips ValidateNewName's parameter-count
        //    check, silently discarding every rename of a parameterized step.
        //  - VS's custom "Rename Step" command builds its own prompt seeded with the binding's
        //    abstract expression (RenameStepCommand.cs), so `newName` already carries the
        //    correct placeholder syntax and needs no reconciliation — attempting it anyway would
        //    fail to find any parameter "value" to locate in already-abstract text and wrongly
        //    reject a rename that never needed fixing up.
        // Try the abstract form first (matching parameter-slot count against the live source
        // expression); only when that count differs do we attempt to derive the abstract
        // expression by diffing the edited concrete text against the original.
        var effectiveNewName = newName;
        if (path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase) &&
            StepExpressionParameters.ExtractSlots(newName).Count != StepExpressionParameters.ExtractSlots(sourceExpression).Count)
        {
            var currentUsage = usages.FirstOrDefault(u =>
                string.Equals(u.FeatureDocumentId, uri.ToString(), StringComparison.OrdinalIgnoreCase) &&
                u.Range != null &&
                request.Position.Line >= u.Range.ToLspRange().Start.Line &&
                request.Position.Line <= u.Range.ToLspRange().End.Line);

            var oldStepText = currentUsage?.Range != null
                ? ReadStepText(uri, currentUsage.Range.ToLspRange())
                : null;

            if (oldStepText == null)
            {
                // Can't read the pre-edit step text (buffer and disk both unavailable) — fall
                // back to treating newName as-is, same as before this reconciliation existed.
                _logger.LogVerbose("StepRenameHandler: could not read original step text for the edited position — using newName as-is");
            }
            else
            {
                var derived = FeatureStepTextBuilder.DeriveExpressionFromEditedText(sourceExpression, oldStepText, newName);
                if (derived == null)
                {
                    _logger.LogVerbose("StepRenameHandler: could not reconcile edited step text with the binding's parameter positions — the parameter values, not just the wording, appear to have changed");
                    return null;
                }

                effectiveNewName = derived;
                _logger.LogVerbose($"StepRenameHandler: derived abstract expression '{effectiveNewName}' from edited step text '{newName}'");
            }
        }

        // ── 3. Validate new name ───────────────────────────────────────────────
        var nameError = StepRenameValidator.ValidateNewName(expression, effectiveNewName);
        if (nameError != null)
        {
            _logger.LogVerbose($"StepRenameHandler: validation failed — {nameError.Message}");
            return null;
        }

        var changes = new Dictionary<DocumentUri, List<TextEdit>>();

        // ── 4. Build .feature file edits ───────────────────────────────────────
        foreach (var usage in usages)
        {
            var featureUri = DocumentUri.Parse(usage.FeatureDocumentId);
            if (!changes.TryGetValue(featureUri, out var list))
            {
                list = new List<TextEdit>();
                changes[featureUri] = list;
            }

            // Read the feature step text to preserve parameter values / placeholders
            string? stepText = null;
            if (usage.Range != null)
            {
                var stepRange = usage.Range.ToLspRange();
                stepText = ReadStepText(featureUri, stepRange);
            }

            var featureNewText = FeatureStepTextBuilder.Build(effectiveNewName, sourceExpression, binding.Regex, stepText);
            list.Add(new TextEdit
            {
                Range = usage.Range!.ToLspRange(),
                NewText = featureNewText
            });
        }

        // ── 5. Build .cs file edit ────────────────────────────────────────────
        DocumentUri? csFileUri = null;
        string? newCsText = null;
        if (sourceLiteral != null)
        {
            var csEdit = BuildCSharpEdit(sourceLiteral, effectiveNewName);
            if (csEdit != null)
            {
                csFileUri = path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    ? uri
                    : DocumentUri.FromFileSystemPath(binding.Implementation!.SourceLocation!.SourceFile);
                if (!changes.TryGetValue(csFileUri, out var list))
                {
                    list = new List<TextEdit>();
                    changes[csFileUri] = list;
                }
                list.Add(csEdit);

                // Computed directly from the same Roslyn span BuildCSharpEdit used, so this is
                // exact regardless of whether the .cs file is open or closed in the editor.
                var sourceText = await sourceLiteral.SyntaxTree!.GetTextAsync(cancellationToken);
                newCsText = sourceText
                    .WithChanges(new Microsoft.CodeAnalysis.Text.TextChange(sourceLiteral.Token.Span, csEdit.NewText))
                    .ToString();
            }
        }

        if (changes.Count == 0)
            return null;

        var workspaceEdit = new WorkspaceEdit
        {
            Changes = changes.ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<TextEdit>)kvp.Value)
        };

        // VS's Rename Step command sends textDocument/rename over a custom interception pipe
        // that swallows this method's return value before VS's built-in LSP client ever sees it
        // (see #82) — so VS needs the edit pushed via a genuine workspace/applyEdit request
        // instead, the same mechanism already proven for F13 (CommentToggleHandler). Other
        // clients (e.g. VS Code) apply the returned WorkspaceEdit natively and must NOT also
        // receive this push, or the edit would be applied twice.
        //
        // The push is awaited and its Applied flag checked *before* touching any server-side
        // cache below: if VS rejects or fails to apply the edit (e.g. a locked/read-only file,
        // or the user having closed the document with unsaved conflicting changes), the actual
        // buffer/file content never changed, so self-refreshing the registry or invalidating the
        // match cache here would desync server state from reality — the registry would claim the
        // rename succeeded while the source still has the old text.
        if (_clientIdeContext.IsVisualStudio)
        {
            var pushParams = new ApplyWorkspaceEditParams
            {
                Edit = new WorkspaceEdit
                {
                    DocumentChanges = new Container<WorkspaceEditDocumentChange>(
                        changes.Select(kvp => new WorkspaceEditDocumentChange(new TextDocumentEdit
                        {
                            TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = kvp.Key, Version = null },
                            Edits = new TextEditContainer(kvp.Value)
                        })))
                }
            };

            var response = await _languageServer.SendRequest(LspMethodNames.WorkspaceApplyEdit, pushParams)
                .Returning<ApplyWorkspaceEditResponse>(cancellationToken);

            if (response is not { Applied: true })
            {
                _logger.LogVerbose($"StepRenameHandler: VS rejected workspace/applyEdit (reason: '{response?.FailureReason}') — not refreshing server caches");
                return null;
            }

            _logger.LogVerbose("StepRenameHandler: VS applied workspace/applyEdit");
        }

        // Invalidate the match cache for CLOSED feature files that were modified by the rename.
        // When a feature file is closed at rename time, no didChange notification fires, so the
        // server's in-memory match cache would otherwise retain the old step text until the file
        // is re-opened and re-parsed. For OPEN files, applying the edit (via workspace/applyEdit
        // for VS, or natively for other clients) already triggers a real textDocument/didChange,
        // which reparses and correctly rebuilds the match cache through the normal sync pipeline
        // — invalidating here too would race with that rebuild. Losing that race (which happens
        // reliably, since this runs after awaiting the VS applyEdit round-trip) leaves the cache
        // empty with nothing left to repopulate it, since the file's content isn't changing
        // again: confirmed live as inlay hints silently disappearing for the whole file post-rename.
        foreach (var changedUri in changes.Keys)
        {
            var changedPath = changedUri.GetFileSystemPath();
            if (!string.IsNullOrEmpty(changedPath) && changedPath.EndsWith(".feature", StringComparison.OrdinalIgnoreCase) &&
                !_documentBuffer.TryGet(changedUri, out _))
            {
                _matchService.InvalidateAllForDocument(changedUri.ToString());
                _logger.LogVerbose($"StepRenameHandler: invalidated match cache for closed '{changedUri}'");
            }
        }

        // Self-refresh the C# binding registry for the edited .cs file directly, rather than
        // relying on a client-echoed textDocument/didChange (there is no file-system watcher for
        // .cs content changes, and a closed file may never round-trip one at all). For VS Code
        // (no confirmed-apply signal available) this is optimistic, same as the .feature
        // invalidation above; for VS it only runs once workspace/applyEdit has been confirmed
        // applied. Any redundant didChange-triggered refresh the client's own sync machinery
        // fires afterward is harmless (idempotent — same content, same result).
        if (csFileUri is not null && newCsText != null)
        {
            await _csharpDiscoveryService.UpdateFromSourceAsync(csFileUri, newCsText, isOpen: false, cancellationToken);
            _csharpFileTextCache.Update(csFileUri, newCsText);
            _logger.LogVerbose($"StepRenameHandler: self-refreshed C# binding registry for '{csFileUri}'");
        }

        // Telemetry
        _telemetryService?.SendEvent("Rename step command executed", new()
        {
            ["Erroneous"] = false,
        });

        return workspaceEdit;
    }

    // ── Custom request handlers ─────────────────────────────────────────────────

    /// <summary>
    /// Handles <c>reqnroll/renameTargets</c> — enumerates all binding attributes
    /// at the cursor position for the multi-attribute picker flow.
    /// </summary>
    public async Task<RenameTargetsResponse?> HandleRenameTargetsAsync(
        TextDocumentPositionParams request,
        CancellationToken          cancellationToken)
    {
        var uri  = request.TextDocument.Uri;
        var path = uri.GetFileSystemPath();

        if (string.IsNullOrEmpty(path))
            return null;

        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleRenameTargetsFromCSharpAsync(uri, path, request.Position, cancellationToken);
        }

        if (path.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleRenameTargetsFromFeatureAsync(uri, path, request.Position, cancellationToken);
        }

        return null;
    }

    private async Task<RenameTargetsResponse?> HandleRenameTargetsFromCSharpAsync(
        DocumentUri uri, string path, Position position, CancellationToken cancellationToken)
    {
        var line = position.Line + 1;

        var registry = _registryLookup.GetRegistryForUri(uri);
        if (registry == ProjectBindingRegistry.Invalid)
            return new RenameTargetsResponse();

        // Collect all bindings at this method location (heuristic: within 5 lines)
        var allBindings = registry.StepDefinitions
            .Where(b => b.Implementation.SourceLocation != null &&
                        string.Equals(b.Implementation.SourceLocation.SourceFile, path, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(b.Implementation.SourceLocation.SourceFileLine - line) <= 5)
            .ToList();

        if (allBindings.Count == 0)
            return new RenameTargetsResponse();

        var response = new RenameTargetsResponse();
        int idx = 0;
        foreach (var b in allBindings)
        {
            // Prefer the live source expression (preserves Cucumber parameter types)
            var sourceLiteral = await FindAttributeLiteralAsync(uri, b);
            var expression = sourceLiteral?.Token.ValueText ?? b.Expression ?? "(unknown)";

            var scopeTag = b.Scope?.Tag?.ToString();
            var scopeSuffix = !string.IsNullOrEmpty(scopeTag) ? $" [@{scopeTag}]" : "";
            response.Targets.Add(new RenameTargetItem
            {
                Label = $"{b.StepDefinitionType} {expression}{scopeSuffix}",
                Expression = expression,
                AttributeIndex = idx,
                StartLine = (b.Implementation.SourceLocation?.SourceFileLine ?? line) - 1,
                StartChar = 1,
                EndLine   = (b.Implementation.SourceLocation?.SourceFileLine ?? line) - 1,
                EndChar   = 200
            });
            idx++;
        }

        return response;
    }

    private async Task<RenameTargetsResponse?> HandleRenameTargetsFromFeatureAsync(
        DocumentUri uri, string path, Position position, CancellationToken cancellationToken)
    {
        var matchedBindings = FindBindingsAtFeatureStep(uri, path, position: position);
        if (matchedBindings.Count == 0)
            return new RenameTargetsResponse();

        var response = new RenameTargetsResponse();
        int idx = 0;
        foreach (var b in matchedBindings)
        {
            // Ambiguous bindings from the .feature side are frequently identical steps bound
            // to different methods (that's the whole reason they're ambiguous) — the expression
            // text alone doesn't distinguish them in the picker, so append the implementing
            // method to give the user something to choose by. Implementation.Method is fully
            // qualified (e.g. "MyProj.StepDefinitions.CalculatorSteps.GivenX(Int32)"); the shared
            // namespace prefix across bindings in the same project pushes the actually-different
            // part (class + method name) past the picker's visible width before two entries'
            // labels diverge, so only the last two dot-segments are kept.
            var method = ShortenMethodQualifier(b.Implementation?.Method);
            var methodSuffix = !string.IsNullOrEmpty(method) ? $" — {method}" : "";
            response.Targets.Add(new RenameTargetItem
            {
                Label = $"{b.StepDefinitionType} {b.Expression ?? "(unknown)"}{methodSuffix}",
                Expression = b.Expression ?? "",
                AttributeIndex = idx,
                StartLine = 0, StartChar = 0, EndLine = 0, EndChar = 200
            });
            idx++;
        }

        return response;
    }

    /// <summary>
    /// Finds all bindings that match the feature step at the given cursor position
    /// by querying the binding match cache for the owning projects.
    /// </summary>
    private List<ProjectStepDefinitionBinding> FindBindingsAtFeatureStep(
        DocumentUri uri, string path, Position position) =>
        FindBindingsAtFeatureStep(uri, path, position, out _);

    /// <summary>
    /// Finds all bindings that match the feature step at the given cursor position, and the
    /// matched step's own text span (excluding the keyword/indentation) via <paramref
    /// name="matchedRange"/>. Callers that only need the range for editing (prepareRename must
    /// offer exactly the text that HandleRenameAsync will later replace at <c>usage.Range</c> —
    /// otherwise the keyword/indentation the client seeds the dialog with gets duplicated when
    /// the edit is applied) should use this overload.
    /// </summary>
    private List<ProjectStepDefinitionBinding> FindBindingsAtFeatureStep(
        DocumentUri uri, string path, Position position, out LspRange? matchedRange)
    {
        matchedRange = null;

        var uriStr = uri.ToString();
        var owners = _scopeManager.ResolveOwners(uri);
        if (owners.Count == 0)
            return new List<ProjectStepDefinitionBinding>();

        var matchedBindings = new HashSet<ProjectStepDefinitionBinding>();

        foreach (var owner in owners)
        {
            var key = new MatchSetKey(uriStr, new ProjectOwner(owner.ProjectFullName, owner.TargetFrameworkMoniker));
            if (!_matchService.TryGet(key, out var matchSet))
                continue;

            foreach (var step in matchSet.Steps)
            {
                // Ambiguous steps (2+ matching bindings) are exactly what the rename-targets
                // picker exists to disambiguate — MatchResult.HasDefined is false for them
                // (their items are typed Ambiguous, not Defined), so it must not gate them out
                // here alongside genuinely undefined steps.
                if (step.Result is null || !(step.Result.HasDefined || step.Result.HasAmbiguous))
                    continue;

                // Tolerate a cursor anywhere on the step's line(s), not just within the exact
                // step-text span (e.g. on the keyword or leading indentation) — this used to
                // compare position.Character directly against step.Range's own start/end
                // character, the same narrow exact-text-span bug #101 fixed for Go to Definition,
                // just re-derived independently here rather than shared (see #82 follow-up).
                var snapshot  = step.Range.Snapshot;
                var startLine = snapshot.GetLineFromLineNumber(step.Range.StartLinePosition.Line);
                var endLine   = snapshot.GetLineFromLineNumber(step.Range.EndLinePosition.Line);
                var offset    = snapshot.ToOffset(position.Line, position.Character);
                if (offset >= startLine.Start && offset <= endLine.End)
                {
                    matchedRange ??= step.Range.ToLspRange();
                    foreach (var item in step.Result.Items)
                    {
                        if (item.MatchedStepDefinition != null)
                            matchedBindings.Add(item.MatchedStepDefinition);
                    }
                }
            }
        }

        return matchedBindings.ToList();
    }

    /// <summary>
    /// Handles <c>reqnroll/selectRenameTarget</c> — stores the selected attribute
    /// for the next <c>textDocument/rename</c> call.
    /// </summary>
    public Task HandleSelectRenameTargetAsync(
        SelectRenameTargetParams request,
        CancellationToken        cancellationToken)
    {
        _sessionManager.SetSession(request.Uri.ToString(), request.Version, request.AttributeIndex);
        return Task.CompletedTask;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private TextEdit? BuildCSharpEdit(
        LiteralExpressionSyntax? literalArgument,
        string newName)
    {
        if (literalArgument == null)
        {
            _logger.LogVerbose("StepRenameHandler: BuildCSharpEdit — no attribute literal found");
            return null;
        }

        // Preserve the parameter tokens as written in the source. The rename dialog edits
        // the non-parameter text only; the parameter slots must keep their original syntax
        // (e.g. a Cucumber '{int}' stays '{int}', a regex '(.*)' stays '(.*)') rather than
        // whatever projection the dialog happened to seed.
        var sourceExpression = literalArgument.Token.ValueText;
        var finalText = ReconcileParameterTokens(sourceExpression, newName);

        // Convert the character-offset TextSpan to line/column using the SyntaxTree
        var lineSpan = literalArgument.SyntaxTree!.GetLineSpan(literalArgument.Token.Span);
        var startPos = lineSpan.StartLinePosition;
        var endPos   = lineSpan.EndLinePosition;

        _logger.LogVerbose($"StepRenameHandler: BuildCSharpEdit — returning edit at ({startPos.Line},{startPos.Character})-({endPos.Line},{endPos.Character}): '{finalText}'");

        return new TextEdit
        {
            Range = new LspRange
            {
                Start = new Position(startPos.Line, startPos.Character),
                End   = new Position(endPos.Line, endPos.Character)
            },
            NewText = "\"" + finalText + "\""
        };
    }

    /// <summary>
    /// Resolves the string-literal attribute argument for <paramref name="binding"/> by its
    /// SOURCE LOCATION, not by matching the registry's expression text. The registry
    /// expression is a discovery-time projection (a Cucumber expression is rendered to a regex
    /// during discovery, and it reflects the last compiled build rather than the live buffer),
    /// so it cannot be relied on to equal the raw attribute string literal. Line drift from a
    /// stale build is tolerated by choosing the nearest candidate method.
    /// </summary>
    private async Task<LiteralExpressionSyntax?> FindAttributeLiteralAsync(
        DocumentUri uri,
        ProjectStepDefinitionBinding binding)
    {
        var csPath = uri.GetFileSystemPath();
        if (string.IsNullOrEmpty(csPath))
        {
            if (binding?.Implementation?.SourceLocation?.SourceFile != null)
            {
                csPath = binding.Implementation.SourceLocation.SourceFile;
                _logger.LogVerbose($"StepRenameHandler: FindAttributeLiteralAsync — using binding source file '{csPath}'");
            }
            else
            {
                _logger.LogVerbose("StepRenameHandler: FindAttributeLiteralAsync — csPath is null/empty");
                return null;
            }
        }
        else if (!csPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // When called from a .feature file, use the binding's C# source file
            if (binding?.Implementation?.SourceLocation?.SourceFile != null)
            {
                csPath = binding.Implementation.SourceLocation.SourceFile;
                _logger.LogVerbose($"StepRenameHandler: FindAttributeLiteralAsync — redirected from '{uri.GetFileSystemPath()}' to binding source '{csPath}'");
            }
            else
            {
                _logger.LogVerbose($"StepRenameHandler: FindAttributeLiteralAsync — non-cs file and no binding source: '{csPath}'");
                return null;
            }
        }

        // Get file text: the live .cs text cache (updated by every didOpen/didChange for this
        // file, from any source — not just our own rename edits, see ICSharpFileTextCache), the
        // Gherkin document buffer (never actually populated for .cs paths — kept in case that
        // ever changes), or disk as a last resort. Without the cache, a .cs edit applied via
        // workspace/applyEdit is never saved to disk, so re-invoking rename on the same step
        // before saving would silently read the pre-edit text back off disk and show a stale
        // placeholder (confirmed live).
        string? fileText = null;
        var csUri = string.Equals(uri.GetFileSystemPath(), csPath, StringComparison.OrdinalIgnoreCase)
            ? uri
            : DocumentUri.FromFileSystemPath(csPath);
        if (_csharpFileTextCache.TryGet(csUri, out var cachedText) && cachedText != null)
        {
            fileText = cachedText;
            _logger.LogVerbose($"StepRenameHandler: FindAttributeLiteralAsync — got text from live cache ({fileText.Length} chars)");
        }
        else if (_documentBuffer.TryGet(csUri, out var buffer) && buffer?.Text != null)
        {
            fileText = buffer.Text;
            _logger.LogVerbose($"StepRenameHandler: FindAttributeLiteralAsync — got text from buffer ({fileText.Length} chars)");
        }
        else if (System.IO.File.Exists(csPath))
        {
            fileText = await System.IO.File.ReadAllTextAsync(csPath);
            _logger.LogVerbose($"StepRenameHandler: FindAttributeLiteralAsync — got text from disk ({fileText.Length} chars)");
        }

        if (fileText == null)
        {
            _logger.LogVerbose("StepRenameHandler: FindAttributeLiteralAsync — no file text available");
            return null;
        }

        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(fileText);
        var rootNode = await tree.GetRootAsync();

        var stepType = binding.StepDefinitionType;
        var candidates = rootNode
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(m => (Method: m,
                          Line: tree.GetLineSpan(m.Identifier.Span).StartLinePosition.Line + 1)) // 1-based
            .Where(x => GetStepAttributeLiterals(x.Method, stepType).Any())
            .ToList();

        if (candidates.Count == 0)
            return null;

        var targetLine = binding.Implementation?.SourceLocation?.SourceFileLine;
        var chosen = targetLine.HasValue
            ? candidates.OrderBy(x => Math.Abs(x.Line - targetLine.Value)).ThenBy(x => x.Line).First()
            : candidates.First();

        // Among the chosen method's matching step attributes, pick the literal to rewrite.
        // A single matching attribute (the common case) is selected regardless of its text.
        // When a method carries several same-type attributes, prefer the one whose literal
        // equals the registry expression, falling back to the first.
        var literals = GetStepAttributeLiterals(chosen.Method, stepType).ToList();
        return literals.FirstOrDefault(e => e.Token.ValueText == binding.Expression)
               ?? literals[0];
    }

    /// <summary>
    /// Keeps only the last two dot-segments of a fully qualified method name (class + method),
    /// dropping the namespace. Two ambiguous bindings from the same project usually share the
    /// same namespace prefix, so keeping it just wastes the picker's limited width without
    /// helping the user distinguish the entries.
    /// </summary>
    private static string? ShortenMethodQualifier(string? fullyQualifiedMethod)
    {
        if (string.IsNullOrEmpty(fullyQualifiedMethod))
            return fullyQualifiedMethod;

        var parts = fullyQualifiedMethod.Split('.');
        return parts.Length <= 2 ? fullyQualifiedMethod : string.Join(".", parts[^2..]);
    }

    /// <summary>
    /// Rebuilds <paramref name="newExpression"/> so that its parameter slots carry the exact
    /// tokens from <paramref name="sourceExpression"/> (positionally). This keeps the original
    /// parameter syntax — a Cucumber <c>{int}</c> stays <c>{int}</c>, a regex <c>(.*)</c> stays
    /// <c>(.*)</c> — even when the rename dialog seeded a different projection. The user's edits
    /// to the non-parameter text are preserved. When the slot counts differ, the user's text is
    /// honoured verbatim.
    /// </summary>
    internal static string ReconcileParameterTokens(string sourceExpression, string newExpression)
    {
        var originalSlots = StepExpressionParameters.ExtractSlots(sourceExpression);
        if (originalSlots.Count == 0)
            return newExpression;

        var newSlots = StepExpressionParameters.ExtractSlots(newExpression);
        if (newSlots.Count != originalSlots.Count)
            return newExpression;

        var sb = new System.Text.StringBuilder();
        var slotIndex = 0;
        var i = 0;
        while (i < newExpression.Length)
        {
            var slotLength = StepExpressionParameters.SlotLengthAt(newExpression, i);
            if (slotLength > 0)
            {
                sb.Append(originalSlots[slotIndex]);
                slotIndex++;
                i += slotLength;
            }
            else
            {
                sb.Append(newExpression[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the first string-literal argument of every attribute on <paramref name="method"/>
    /// that is a step-definition attribute for <paramref name="stepType"/> (<c>Given</c>/<c>When</c>/
    /// <c>Then</c>, or <c>StepDefinition</c> which applies to all step kinds).
    /// </summary>
    private static IEnumerable<LiteralExpressionSyntax> GetStepAttributeLiterals(
        MethodDeclarationSyntax method, ScenarioBlock stepType)
    {
        foreach (var attr in method.AttributeLists.SelectMany(al => al.Attributes))
        {
            if (!IsStepAttributeFor(attr, stepType))
                continue;

            var literal = attr.ArgumentList?.Arguments
                .Select(a => a.Expression)
                .OfType<LiteralExpressionSyntax>()
                .FirstOrDefault(e => e.RawKind == (int)SyntaxKind.StringLiteralExpression);

            if (literal != null)
                yield return literal;
        }
    }

    private static bool IsStepAttributeFor(AttributeSyntax attr, ScenarioBlock stepType)
    {
        var name = attr.Name switch
        {
            QualifiedNameSyntax q => q.Right.Identifier.Text,
            SimpleNameSyntax    s => s.Identifier.Text,
            _                     => attr.Name.ToString()
        };

        if (name.EndsWith("Attribute", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Attribute".Length);

        // [StepDefinition("…")] registers for Given/When/Then alike.
        if (string.Equals(name, "StepDefinition", StringComparison.Ordinal))
            return true;

        return stepType switch
        {
            ScenarioBlock.Given => name == "Given",
            ScenarioBlock.When  => name == "When",
            ScenarioBlock.Then  => name == "Then",
            _                   => name is "Given" or "When" or "Then"
        };
    }

    // ── Feature step text parameter preservation ─────────────────────────────

    /// <summary>
    /// Reads the step text from a feature file at the given range, using the
    /// document buffer if available (open file) or reading from disk.
    /// </summary>
    private string? ReadStepText(DocumentUri featureUri, LspRange range)
    {
        string? fileText = null;
        if (_documentBuffer.TryGet(featureUri, out var buffer) && buffer?.Text != null)
            fileText = buffer.Text;
        else
        {
            var path = featureUri.GetFileSystemPath();
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                fileText = System.IO.File.ReadAllText(path);
        }

        if (fileText == null)
            return null;

        var lines = fileText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (range.Start.Line < 0 || range.Start.Line >= lines.Length)
            return null;

        var line = lines[range.Start.Line];
        var start = Math.Min(range.Start.Character, line.Length);
        var end   = Math.Min(range.End.Character, line.Length);
        return start < end ? line.Substring(start, end - start) : null;
    }
}
