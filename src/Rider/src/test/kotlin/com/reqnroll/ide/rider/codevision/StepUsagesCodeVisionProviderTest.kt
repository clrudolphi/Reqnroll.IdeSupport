package com.reqnroll.ide.rider.codevision

import org.eclipse.lsp4j.CodeLens
import org.eclipse.lsp4j.Command
import org.eclipse.lsp4j.Position
import org.eclipse.lsp4j.Range
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class StepUsagesCodeVisionProviderTest {
    private fun lensAt(line: Int, command: Command? = Command("3 step usages", "reqnroll.findStepUsages")) =
        CodeLens(Range(Position(line, 0), Position(line, 0)), command, null)

    @Test
    fun `isRenderable is true for a lens with a command on an in-range line`() {
        assertTrue(StepUsagesCodeVisionProvider.isRenderable(lensAt(0), lineCount = 5))
        assertTrue(StepUsagesCodeVisionProvider.isRenderable(lensAt(4), lineCount = 5))
    }

    @Test
    fun `isRenderable is false when the lens has no command`() {
        assertFalse(StepUsagesCodeVisionProvider.isRenderable(lensAt(0, command = null), lineCount = 5))
    }

    @Test
    fun `isRenderable is false for a line outside the document`() {
        assertFalse(StepUsagesCodeVisionProvider.isRenderable(lensAt(5), lineCount = 5))
        assertFalse(StepUsagesCodeVisionProvider.isRenderable(lensAt(-1), lineCount = 5))
    }

    // Regression test for a real bug: an earlier version of buildEntry's call site passed the
    // constructor's (text, providerId, ...) arguments in the wrong order, so the lens rendered
    // this provider's id instead of the actual usage count.
    @Test
    fun `buildEntry renders the command title as text, not the provider id`() {
        val command = Command("3 step usages", "reqnroll.findStepUsages")
        val entry = StepUsagesCodeVisionProvider.buildEntry(command, providerId = "Reqnroll.StepUsagesCodeVision") {}

        assertEquals("3 step usages", entry.text)
        assertEquals("Reqnroll.StepUsagesCodeVision", entry.providerId)
    }
}
