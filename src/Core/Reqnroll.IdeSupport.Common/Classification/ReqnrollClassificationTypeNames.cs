using System.Collections.Generic;

namespace Reqnroll.IdeSupport.Common.Classification;

/// <summary>
/// The canonical set of custom Reqnroll classification / semantic-token type names.
/// </summary>
/// <remarks>
/// <para>
/// These names are the single source of truth shared by:
/// </para>
/// <list type="bullet">
///   <item>the LSP server's semantic-token legend (advertised in the <c>initialize</c> response), and</item>
///   <item>the Visual Studio extension's MEF <c>ClassificationTypeDefinition</c> exports
///         (<c>DeveroomClassifications</c>), which map each name to a concrete editor colour.</item>
/// </list>
/// <para>
/// Rather than emitting the generic LSP standard token types (<c>keyword</c>, <c>string</c>,
/// <c>parameter</c>, …), the server declares these Reqnroll-specific names so that each IDE
/// client can map a Reqnroll concept to a Reqnroll-specific colour instead of overloading a
/// host theme's generic scopes.  The values intentionally match the custom classification
/// names already used by the existing <c>Reqnroll.VisualStudio</c> extension, giving existing
/// users pixel-for-pixel continuity.
/// </para>
/// <para>
/// <b>Legend stability:</b> the order in <see cref="Ordered"/> is the contract between the
/// server and every client (the index emitted in each semantic-token 5-tuple is an index into
/// this list).  Append new entries only — never reorder or remove existing ones — so that older
/// client index assumptions remain valid across versions.
/// </para>
/// </remarks>
public static class ReqnrollClassificationTypeNames
{
    /// <summary>Gets or sets the keyword.</summary>
    public const string Keyword = "reqnroll.keyword";
    /// <summary>Gets or sets the tag.</summary>
    public const string Tag = "reqnroll.tag";
    /// <summary>Gets or sets the description.</summary>
    public const string Description = "reqnroll.description";
    /// <summary>Gets or sets the comment.</summary>
    public const string Comment = "reqnroll.comment";
    /// <summary>Gets or sets the doc string.</summary>
    public const string DocString = "reqnroll.doc_string";
    /// <summary>Gets or sets the data table.</summary>
    public const string DataTable = "reqnroll.data_table";
    /// <summary>Gets or sets the data table header.</summary>
    public const string DataTableHeader = "reqnroll.data_table_header";
    /// <summary>Gets or sets the step parameter.</summary>
    public const string StepParameter = "reqnroll.step_parameter";
    /// <summary>Gets or sets the scenario outline placeholder.</summary>
    public const string ScenarioOutlinePlaceholder = "reqnroll.scenario_outline_placeholder";

    /// <summary>
    /// Step text of a step with no matching binding.  Only carries meaning once Roslyn/C#
    /// source-level binding discovery is available; reserved in the legend from Phase 1 so the legend never
    /// changes across phases.
    /// </summary>
    public const string UndefinedStep = "reqnroll.undefined_step";

    /// <summary>
    /// Step text of a step that matches more than one step-definition binding (ambiguous).
    /// The test would fail at runtime with "Ambiguous step definitions"; highlighted to make
    /// the conflict visible in the editor.
    /// </summary>
    public const string AmbiguousStep = "reqnroll.ambiguous_step";

    /// <summary>
    /// The legend order.  Indices into this array are the <c>tokenTypeIndex</c> emitted in the
    /// LSP semantic-token 5-tuples.  Append-only (see "Legend stability" in the type remarks).
    /// </summary>
    public static readonly IReadOnlyList<string> Ordered = new[]
    {
        Keyword,                     // 0
        Tag,                         // 1
        Description,                 // 2
        Comment,                     // 3
        DocString,                   // 4
        DataTable,                   // 5
        DataTableHeader,             // 6
        StepParameter,               // 7
        ScenarioOutlinePlaceholder,  // 8
        UndefinedStep,               // 9
        AmbiguousStep,               // 10
    };
}
