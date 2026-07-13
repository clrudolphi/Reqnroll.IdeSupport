import org.gradle.internal.os.OperatingSystem

plugins {
    kotlin("jvm") version "2.0.21"
    id("org.jetbrains.intellij.platform") version "2.2.1"
}

group = providers.gradleProperty("pluginGroup").get()
version = providers.gradleProperty("pluginVersion").get()

repositories {
    mavenCentral()
    intellijPlatform {
        defaultRepositories()
    }
}

dependencies {
    intellijPlatform {
        rider(providers.gradleProperty("platformVersion"))
        instrumentationTools()
    }
}

kotlin {
    jvmToolchain(21)
}

intellijPlatform {
    pluginConfiguration {
        id = providers.gradleProperty("pluginGroup")
        name = "Reqnroll"
        version = providers.gradleProperty("pluginVersion")

        ideaVersion {
            sinceBuild = providers.gradleProperty("pluginSinceBuild")
            untilBuild = providers.gradleProperty("pluginUntilBuild")
        }
    }
}

// ── Bundle the Reqnroll.IdeSupport LSP server ───────────────────────────────
//
// ReqnrollServerPathResolver (src/main/kotlin/.../lsp/ReqnrollServerPathResolver.kt)
// expects server/<rid>/Reqnroll.IdeSupport.LSP.Server[.exe] under the plugin's own
// install directory, for whichever RID matches the OS Rider is actually running on
// — so a distributable build needs every supported RID bundled at once, mirroring
// the layout src/VSCode's build produces (a single, OS-detecting .vsix/.zip).
//
// Two ways to populate server/<rid>/ here, matching the `UseExternalLspServerBuild`
// / `LspServerBuildDir` MSBuild properties the VS extension build already uses for
// the same problem (see build-vs-extension.yml):
//
//  - Local dev (no -PlspServerBuildDir): publishServer runs `dotnet publish` for the
//    host RID only — fast, and sufficient since a local `runIde` only ever needs the
//    server for the OS it's running on.
//  - CI (-PlspServerBuildDir=<dir>): skips publishServer entirely and instead copies
//    whichever server-<rid> subdirectories already exist under <dir> — CI populates
//    those from the already-built-and-tested artifacts test-lsp.yml publishes, so
//    Gradle never needs `dotnet` on the CI runner at all.

val repoRoot = layout.projectDirectory.dir("../..").asFile.canonicalFile
val allServerRids = listOf("win-x64", "linux-x64", "osx-x64", "osx-arm64")

fun defaultServerRid(): String {
    val os = OperatingSystem.current()
    val arch = System.getProperty("os.arch").lowercase()
    return when {
        os.isWindows -> "win-x64"
        os.isMacOsX -> if (arch.contains("aarch64") || arch.contains("arm")) "osx-arm64" else "osx-x64"
        else -> "linux-x64"
    }
}

// Override with e.g. `./gradlew runIde -PserverRid=linux-x64` to publish/bundle a
// different single RID for local dev. Ignored once -PlspServerBuildDir is set.
val serverRid = (findProperty("serverRid") as String?) ?: defaultServerRid()
val serverOutputDir = layout.projectDirectory.dir("server/$serverRid")
val serverProject = File(repoRoot, "src/LSP/Reqnroll.IdeSupport.LSP.Server/Reqnroll.IdeSupport.LSP.Server.csproj")
val connectorProject = File(repoRoot, "src/LSP/Reqnroll.IdeSupport.LSP.Connector/Connector/Connector.csproj")

// Directory containing one server-<rid>-shaped subdirectory per RID, pre-published
// and tested by CI (see .github/workflows/build-rider-plugin.yml). When set,
// publishServer is skipped and prepareSandbox bundles every RID found here instead
// of just the host's.
val externalServerBuildDir = (findProperty("lspServerBuildDir") as String?)?.let { File(it) }

val publishServer by tasks.registering(Exec::class) {
    group = "reqnroll"
    description = "Publishes Reqnroll.IdeSupport.LSP.Server (RID=$serverRid) into server/$serverRid. " +
        "Skipped when -PlspServerBuildDir is set."
    onlyIf { externalServerBuildDir == null }

    inputs.file(serverProject)
    outputs.dir(serverOutputDir)

    doFirst {
        // Restore the Connector project for this RID first — it's multi-TFM and doesn't
        // resolve correctly as part of the Server's own restore. Same requirement as
        // src/VSCode/scripts/publish-server.sh.
        // `project.exec` (not bare `exec`) — this task is itself an `Exec` task, which has its
        // own no-arg `exec(): Unit` member that shadows the `Project.exec(Action)` extension;
        // the unqualified name resolves to that member and fails to compile ("too many
        // arguments") under some Gradle/Kotlin-DSL combinations.
        project.exec {
            commandLine("dotnet", "restore", connectorProject.toString(), "--runtime", serverRid)
        }
    }

    commandLine(
        "dotnet", "publish", serverProject.toString(),
        "--configuration", "Release",
        "--runtime", serverRid,
        "--self-contained", "true",
        "--nologo",
        "--output", serverOutputDir.asFile.absolutePath,
    )
}

tasks.named<Sync>("prepareSandbox") {
    val externalDir = externalServerBuildDir
    if (externalDir == null) {
        dependsOn(publishServer)
        from(serverOutputDir) {
            into("${project.name}/server/$serverRid")
        }
    } else {
        allServerRids.forEach { rid ->
            val ridDir = File(externalDir, rid)
            if (ridDir.exists()) {
                from(ridDir) {
                    into("${project.name}/server/$rid")
                }
            }
        }
    }
}

tasks {
    wrapper {
        gradleVersion = "8.10"
    }
}
