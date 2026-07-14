package com.reqnroll.ide.rider.lsp.project

import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.openapi.rd.util.RdCoroutineHost
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rider.model.runnableProjectsModel
import com.jetbrains.rider.projectView.solution
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger
import com.reqnroll.ide.rider.lsp.ReqnrollNotificationSender
import com.reqnroll.ide.rider.lsp.protocol.ReqnrollProjectUnloadedParams
import kotlinx.coroutines.withContext

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
                    ReqnrollNotificationSender.sendProjectLoaded(
                        project, ReqnrollProjectBaseline.buildProjectLoadedParams(project, runnableProject))
                }

                knownProjectFiles.clear()
                knownProjectFiles.addAll(currentFiles)
            }
        }
    }
}
