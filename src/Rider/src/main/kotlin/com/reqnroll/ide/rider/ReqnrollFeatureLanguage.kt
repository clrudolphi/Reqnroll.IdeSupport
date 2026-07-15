package com.reqnroll.ide.rider

import com.intellij.lang.Language

/**
 * The Gherkin/`.feature` file language, identified purely by ID ("Reqnroll Feature") — there's no
 * grammar/`ParserDefinition` registered; all language-aware behavior (coloring, diagnostics,
 * navigation, etc.) comes from the LSP server via [com.reqnroll.ide.rider.lsp.ReqnrollLspServerDescriptor].
 */
object ReqnrollFeatureLanguage : Language("Reqnroll Feature")
