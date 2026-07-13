package com.reqnroll.ide.rider.lsp

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.ProjectWideLspServerDescriptor

class ReqnrollLspServerDescriptor(project: Project) :
    ProjectWideLspServerDescriptor(project, "Reqnroll") {

    override fun isSupportedFile(file: VirtualFile): Boolean =
        file.extension == "feature" || file.extension == "cs"

    override fun createCommandLine(): GeneralCommandLine =
        GeneralCommandLine(ReqnrollServerPathResolver.resolve().toString())
            .withParameters("--ide", "rider", "--log-level", "Warning")
}
