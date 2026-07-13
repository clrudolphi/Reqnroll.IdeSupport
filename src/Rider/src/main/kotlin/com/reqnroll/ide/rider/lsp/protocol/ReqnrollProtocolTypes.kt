package com.reqnroll.ide.rider.lsp.protocol

/**
 * Wire DTOs for the reqnroll-prefixed client-to-server notifications, mirrored field-for-field
 * (camelCase, matching the server's CamelCasePropertyNamesContractResolver) against:
 *   - src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/ReqnrollProjectLoadedParams.cs
 *   - src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/ReqnrollProjectFilesParams.cs
 *   - src/LSP/Reqnroll.IdeSupport.LSP.Server/Protocol/ReqnrollProjectUnloadedParams.cs
 *   - src/LSP/Reqnroll.IdeSupport.LSP.Server/Features/DocumentActivated/DocumentActivatedParams.cs
 *
 * `kind`/`role` are plain Int (not Kotlin enums) matching the C# side's default Newtonsoft
 * integer enum representation — LSP4J's Gson-based serializer defaults to enum-name-as-string,
 * which would not match the wire format VS/VS Code already send (see VsProjectEventMonitor.cs's
 * `kind = 1` / `role = 1` literal int usage). ProjectFilesKind/ProjectFileRole below are just
 * named int constants for callers to use instead of magic numbers.
 */

/** Payload for `reqnroll/projectLoaded`. */
data class ReqnrollProjectLoadedParams(
    val workspaceFolder: String,
    val projectFile: String,
    val projectFolder: String,
    val outputAssemblyPath: String,
    val targetFrameworkMoniker: String,
    val defaultNamespace: String,
    val packageReferences: List<PackageReferenceInfo> = emptyList(),
)

/** One resolved NuGet package reference. */
data class PackageReferenceInfo(
    val packageId: String,
    val version: String,
    val installPath: String,
)

/** Payload for `reqnroll/projectUnloaded`. */
data class ReqnrollProjectUnloadedParams(
    val projectFile: String,
)

/** Payload for `reqnroll/projectFiles`. `kind`/`files[].role` — see [ProjectFilesKind]/[ProjectFileRole]. */
data class ReqnrollProjectFilesParams(
    val projectFile: String,
    val targetFrameworkMoniker: String,
    val kind: Int,
    val files: List<ProjectFileEntry> = emptyList(),
)

/** One file attributed to a project in a [ReqnrollProjectFilesParams] payload. */
data class ProjectFileEntry(
    val path: String,
    val role: Int,
    val added: Boolean = true,
)

/** Matches `ProjectFilesKind` in ReqnrollProjectFilesParams.cs. */
object ProjectFilesKind {
    const val BASELINE = 0
    const val DELTA = 1
}

/** Matches `ProjectFileRole` in ReqnrollProjectFilesParams.cs. */
object ProjectFileRole {
    const val FEATURE = 0
    const val BINDING = 1

    /** Mirrors VsProjectEventMonitor.ClassifyRole — null for extensions the index doesn't track. */
    fun classify(path: String): Int? = when {
        path.endsWith(".feature", ignoreCase = true) -> FEATURE
        path.endsWith(".cs", ignoreCase = true) -> BINDING
        else -> null
    }
}

/** Payload for `reqnroll/documentActivated` (issue #85). */
data class DocumentActivatedParams(
    val uri: String,
)
