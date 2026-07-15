package com.reqnroll.ide.rider.actions

import com.reqnroll.ide.rider.lsp.protocol.UnusedStepDefinitionItem
import kotlin.test.Test
import kotlin.test.assertEquals

class FindUnusedStepDefinitionsActionTest {
    @Test
    fun `renderLabel includes class, method, expression, and project when all present`() {
        val item = UnusedStepDefinitionItem(
            projectName = "Calculator",
            className = "CalculatorSteps",
            methodName = "GivenIHaveEnteredNumber",
            bindingExpression = "I have entered {int}",
            sourceFile = "/repo/CalculatorSteps.cs",
            sourceLine = 12,
            sourceChar = 4,
        )

        assertEquals(
            "CalculatorSteps.GivenIHaveEnteredNumber — I have entered {int} [Calculator]",
            FindUnusedStepDefinitionsAction.renderLabel(item),
        )
    }

    @Test
    fun `renderLabel omits optional segments that are null`() {
        val item = UnusedStepDefinitionItem(
            projectName = null,
            className = "CalculatorSteps",
            methodName = "GivenIHaveEnteredNumber",
            bindingExpression = null,
            sourceFile = "/repo/CalculatorSteps.cs",
            sourceLine = 12,
            sourceChar = 4,
        )

        assertEquals(
            "CalculatorSteps.GivenIHaveEnteredNumber",
            FindUnusedStepDefinitionsAction.renderLabel(item),
        )
    }

    @Test
    fun `renderLabel falls back to just the method name when className is null`() {
        val item = UnusedStepDefinitionItem(methodName = "GivenIHaveEnteredNumber")

        assertEquals(
            "GivenIHaveEnteredNumber",
            FindUnusedStepDefinitionsAction.renderLabel(item),
        )
    }
}
