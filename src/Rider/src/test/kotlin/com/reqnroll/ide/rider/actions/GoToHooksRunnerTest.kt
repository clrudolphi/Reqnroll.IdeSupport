package com.reqnroll.ide.rider.actions

import com.reqnroll.ide.rider.lsp.protocol.GoToHookLocation
import kotlin.test.Test
import kotlin.test.assertEquals

class GoToHooksRunnerTest {
    @Test
    fun `renderLabel includes hook type, method name, file name, and 1-based line`() {
        val item = GoToHookLocation(
            uri = "file:///repo/Hooks.cs",
            startLine = 9,
            startChar = 4,
            hookType = "BeforeScenario",
            hookOrder = 10000,
            methodName = "SetUpDatabase",
        )

        assertEquals(
            "[BeforeScenario] SetUpDatabase (Hooks.cs:10)",
            GoToHooksRunner.renderLabel(item),
        )
    }

    @Test
    fun `renderLabel falls back gracefully when uri has no path segments`() {
        val item = GoToHookLocation(
            uri = "Hooks.cs",
            startLine = 0,
            hookType = "AfterStep",
            methodName = "TearDown",
        )

        assertEquals(
            "[AfterStep] TearDown (Hooks.cs:1)",
            GoToHooksRunner.renderLabel(item),
        )
    }
}
