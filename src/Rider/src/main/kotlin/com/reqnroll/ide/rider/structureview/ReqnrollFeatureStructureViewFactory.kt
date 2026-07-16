package com.reqnroll.ide.rider.structureview

import com.intellij.ide.structureView.StructureViewBuilder
import com.intellij.ide.structureView.StructureViewBuilderProvider
import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile

/**
 * Wires `.feature` files into IntelliJ's Structure View (Alt+7) — issue #163.
 *
 * `com.intellij.lang.psiStructureViewFactory` (`PsiStructureViewFactory`, keyed by `Language`)
 * would be the conventional way to do this, but it's dead-on-arrival here: `.feature` files have
 * a `Language` (`ReqnrollFeatureLanguage`) but no `ParserDefinition` — confirmed repeatedly
 * elsewhere in this plugin (see [com.reqnroll.ide.rider.inlayhints.ReqnrollFeatureInlayHintsController]'s
 * doc comment) — so IntelliJ falls back to a `PsiPlainTextFile` and anything keyed by the
 * declared language never fires.
 *
 * `com.intellij.structureViewBuilder` (this EP, `StructureViewBuilderProvider`) is keyed by
 * [FileType] instead, confirmed via `javap` on the actual bundled Rider 2024.3.5 class — so it
 * sidesteps the PSI-language problem entirely, the same "bypass the declarative EP, hand-build
 * the platform adapter" pattern already used for folding/inlay hints/CodeVision, just one layer
 * up (the toolwindow itself, not just rendering). Registered in plugin.xml with
 * `key="Reqnroll Feature"` matching [com.reqnroll.ide.rider.ReqnrollFeatureFileType]'s name.
 */
class ReqnrollFeatureStructureViewFactory : StructureViewBuilderProvider {
    override fun getStructureViewBuilder(fileType: FileType, file: VirtualFile, project: Project): StructureViewBuilder? {
        if (!file.extension.equals("feature", ignoreCase = true)) return null
        return ReqnrollFeatureStructureViewBuilder(project, file)
    }
}
