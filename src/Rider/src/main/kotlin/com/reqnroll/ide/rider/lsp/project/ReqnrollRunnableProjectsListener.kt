package com.reqnroll.ide.rider.lsp.project

import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.openapi.rd.util.RdCoroutineHost
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rider.model.RunnableProject
import com.jetbrains.rider.model.runnableProjectsModel
import com.jetbrains.rider.projectView.solution
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.ReqnrollNotificationSender
import com.reqnroll.ide.rider.lsp.protocol.PackageReferenceInfo
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollProjectLoadedParams
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollProjectUnloadedParams
import kotlinx.coroutines.withContext
import java.io.File

/**
 * Feeds `reqnroll/projectLoaded`/`reqnroll/projectUnloaded` from Rider's own runnable-projects
 * model — [docs/Rider-Project-Document-Sync-Implementation-Plan.md] Phase 2. Confirmed via
 * decompiling Rider 2024.3.5's actual bundled classes (product.jar):
 * `project.solution.runnableProjectsModel.projects` is an `IOptProperty<List<RunnableProject>>`
 * (RD reactive property) already exposing TFM + output assembly path via
 * `RunnableProject.projectOutputs` — no RD protocol submodule needed (Phase 0 finding).
 *
 * One reactive subscription covers all three of the plan's originally-separate event sources:
 * `advise` fires immediately with the current value on subscribe (→ initial flush on project
 * open, no separate "SendInitialProjectsAsync"-equivalent needed) and again on every subsequent
 * change, including whatever Rider's backend does on rebuild (→ no separate build-completion
 * listener needed either, assuming the backend actually re-pushes this model post-build —
 * simpler than the plan's original guess of needing `RunnableProjectListener`, which turned out
 * to be Rider's own internal gutter-icon-refresh listener, not a public extension point).
 *
 * `advise` only ever hands us the *current full list* — there's no per-project add/remove
 * callback — so project removal is detected by diffing against the previous snapshot.
 */
class ReqnrollRunnableProjectsListener : ProjectActivity {
    override suspend fun execute(project: Project) {
        val lifetime = project.createLifetime()
        val knownProjectFiles = mutableSetOf<String>()

        // RD reactive properties assert they're advised from the UI thread or a scheduler the RD
        // dispatcher itself recognizes — a plain background ProjectActivity coroutine is neither,
        // and .advise() throws IllegalStateException("Wrong thread ...") without this. Confirmed
        // by decompiling Rider 2024.3.5's actual RdCoroutineHost class that .uiDispatcher is the
        // correct RD-aware context for exactly this.
        withContext(RdCoroutineHost.instance.uiDispatcher) {
            project.solution.runnableProjectsModel.projects.advise(lifetime) { runnableProjects ->
                val current = runnableProjects.orEmpty()
                val currentFiles = current.map { it.projectFilePath }.toSet()

                (knownProjectFiles - currentFiles).forEach { removedFile ->
                    ReqnrollDebugLogger.info("projectUnloaded: $removedFile")
                    ReqnrollNotificationSender.sendProjectUnloaded(project, ReqnrollProjectUnloadedParams(removedFile))
                }

                current.forEach { runnableProject ->
                    ReqnrollDebugLogger.info("projectLoaded: ${runnableProject.projectFilePath}")
                    ReqnrollNotificationSender.sendProjectLoaded(project, buildProjectLoadedParams(project, runnableProject))
                }

                knownProjectFiles.clear()
                knownProjectFiles.addAll(currentFiles)
            }
        }
    }

    private fun buildProjectLoadedParams(project: Project, runnableProject: RunnableProject): ReqnrollProjectLoadedParams {
        // Reqnroll test projects normally have a single output per TFM; multi-TFM projects are
        // a known follow-up (see ReqnrollProjectFilesParams.TargetFrameworkMoniker's own
        // "Phase 1 ignores TFM" note on the server side).
        val output = runnableProject.projectOutputs.firstOrNull()
        val projectFolder = File(runnableProject.projectFilePath).parent ?: ""

        return ReqnrollProjectLoadedParams(
            workspaceFolder = project.basePath ?: "",
            projectFile = runnableProject.projectFilePath,
            projectFolder = projectFolder,
            outputAssemblyPath = output?.exePath ?: "",
            // RdTargetFrameworkId has no classic MSBuild moniker field (".NETCoreApp,Version=v8.0")
            // — shortName ("net8.0") is the closest available. Revisit if the server's reflection
            // discovery needs the exact classic format rather than the short one.
            targetFrameworkMoniker = output?.tfm?.shortName ?: "",
            // Not available from RunnableProject; server uses this only to derive namespaces for
            // scaffolded files, which isn't reachable from Rider yet anyway (no scaffolding UI).
            defaultNamespace = "",
            // No dedicated Rider model class for resolved NuGet package references (Phase 0
            // finding) — reading obj/project.assets.json from disk is the planned follow-up
            // (see the plan doc's R5), not yet implemented.
            packageReferences = emptyList<PackageReferenceInfo>(),
        )
    }
}
