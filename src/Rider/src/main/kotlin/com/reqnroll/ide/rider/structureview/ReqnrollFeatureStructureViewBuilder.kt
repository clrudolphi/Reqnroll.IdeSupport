package com.reqnroll.ide.rider.structureview

import com.intellij.ide.structureView.StructureViewModel
import com.intellij.ide.structureView.TreeBasedStructureViewBuilder
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.PsiManager

/**
 * Builds the Structure View model for one `.feature` [virtualFile] — see
 * [ReqnrollFeatureStructureViewFactory] for why this EP (rather than the PSI-based
 * `psiStructureViewFactory`) is the viable path.
 *
 * `isRootNodeShown() = false`: the model's root wraps the whole file (needed to satisfy
 * `StructureViewModelBase`'s constructor), but the file itself isn't a useful outline entry —
 * hiding it shows Feature/Rule/Scenario/Step directly as top-level tree nodes, matching every
 * other language's Structure View convention.
 */
class ReqnrollFeatureStructureViewBuilder(
    private val project: Project,
    private val virtualFile: VirtualFile,
) : TreeBasedStructureViewBuilder() {
    override fun createStructureViewModel(editor: Editor?): StructureViewModel {
        // Every VirtualFile with a registered FileType resolves to a real (if structurally
        // trivial, parser-less) PsiFile via PsiManager — confirmed via ReqnrollFeatureStructureViewFactory's
        // reasoning — so this should not actually be null for a `.feature` file in practice.
        // StructureViewModelBase's constructor requires a non-null PsiFile regardless, so there's
        // no graceful degradation available here if it somehow is.
        val psiFile = PsiManager.getInstance(project).findFile(virtualFile)!!
        return ReqnrollFeatureStructureViewModel(project, virtualFile, psiFile, editor)
    }

    override fun isRootNodeShown(): Boolean = false
}
