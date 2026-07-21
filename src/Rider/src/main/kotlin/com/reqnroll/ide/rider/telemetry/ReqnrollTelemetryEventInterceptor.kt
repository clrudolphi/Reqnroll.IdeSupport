package com.reqnroll.ide.rider.telemetry

import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import com.reqnroll.ide.rider.logging.ReqnrollDebugLogger

/**
 * Delegates every [LspServerNotificationsHandler] callback straight through to Rider's own
 * platform-provided [handler], except [telemetryEvent] — there it also forwards the event to
 * [RiderTelemetryTransmitter], mirroring what VS's `TelemetryEventInterceptor.cs` and VS Code's
 * `telemetry.ts` already do for the standard `telemetry/event` notification (see
 * `LspTelemetryService.cs`) — Rider previously had no consumer for it at all (issue #255).
 *
 * `telemetryEvent` is confirmed (by fetching the pinned platform version, 2024.3.5 / branch `243`,
 * from JetBrains/intellij-community's public mirror) to be part of `LspServerNotificationsHandler`
 * and *not* `final` on `Lsp4jClient` — same override-via-delegation shape already used by
 * [com.reqnroll.ide.rider.lsp.ReqnrollCodeLensRefreshInterceptor] and
 * [com.reqnroll.ide.rider.lsp.ReqnrollInlayHintRefreshInterceptor]. Should still be exercised live
 * via `./gradlew runIde` before release, same as those two.
 *
 * The server sends `params` shaped as `{ eventName, properties }` (see `LspTelemetryService.cs`).
 * lsp4j/Gson deserializes the untyped `Any` parameter to a `Map`-like value (Gson's
 * `LinkedTreeMap`) for a JSON object, so this reads it structurally via `Map<*, *>` rather than a
 * Gson-specific type, so it isn't coupled to Gson's internal representation.
 */
class ReqnrollTelemetryEventInterceptor(
    private val handler: LspServerNotificationsHandler,
) : LspServerNotificationsHandler by handler {
    override fun telemetryEvent(`object`: Any) {
        try {
            val params = `object` as? Map<*, *>
            val eventName = params?.get("eventName") as? String
            if (eventName.isNullOrEmpty()) {
                ReqnrollDebugLogger.warn("ReqnrollTelemetryEventInterceptor: telemetry/event without eventName; dropping.")
            } else {
                val properties = (params["properties"] as? Map<*, *>)
                    ?.entries
                    ?.mapNotNull { (key, value) -> (key as? String)?.let { it to value } }
                    ?.toMap()
                    ?: emptyMap()
                RiderTelemetryTransmitter.transmit(eventName, properties)
            }
        } catch (ex: Exception) {
            ReqnrollDebugLogger.warn("ReqnrollTelemetryEventInterceptor: error forwarding telemetry/event.", ex)
        }

        handler.telemetryEvent(`object`)
    }
}
