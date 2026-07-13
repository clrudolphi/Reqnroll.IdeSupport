package com.reqnroll.ide.rider.lsp

import com.intellij.ide.plugins.PluginManagerCore
import com.intellij.openapi.extensions.PluginId
import java.nio.file.Path

/**
 * Resolves the bundled Reqnroll.IdeSupport.LSP.Server executable for the current OS/arch,
 * mirroring the `server/<rid>/<binary>` layout used by the VS Code extension
 * (see src/VSCode/src/extension.ts resolveServerPath).
 */
object ReqnrollServerPathResolver {
    private val PLUGIN_ID = PluginId.getId("com.reqnroll.ide.rider")

    fun resolve(): Path {
        val plugin = PluginManagerCore.getPlugin(PLUGIN_ID)
            ?: error("Reqnroll plugin descriptor '$PLUGIN_ID' not found")

        val rid = currentRid()
        val binaryName = if (isWindows()) "Reqnroll.IdeSupport.LSP.Server.exe" else "Reqnroll.IdeSupport.LSP.Server"
        val candidate = plugin.pluginPath.resolve("server").resolve(rid).resolve(binaryName)

        if (candidate.toFile().exists()) {
            return candidate
        }

        error(
            "Reqnroll LSP server not found at $candidate. " +
                "Ensure the server is published and bundled under server/$rid/ for this plugin."
        )
    }

    private fun isWindows() = System.getProperty("os.name").lowercase().contains("win")

    private fun currentRid(): String {
        val os = System.getProperty("os.name").lowercase()
        val arch = System.getProperty("os.arch").lowercase()
        return when {
            os.contains("win") -> "win-x64"
            os.contains("mac") -> if (arch.contains("aarch64") || arch.contains("arm")) "osx-arm64" else "osx-x64"
            else -> "linux-x64"
        }
    }
}
