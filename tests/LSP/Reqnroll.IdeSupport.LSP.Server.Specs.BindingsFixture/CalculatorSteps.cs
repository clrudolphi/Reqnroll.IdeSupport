using Reqnroll;

namespace ReqnrollBindingsFixture;

/// <summary>
/// Ground-truth Reqnroll bindings for the discovery specs.  Deliberately varied so the
/// out-of-process connector has several step definitions, a hook, and a Unicode regex to find.
/// </summary>
[Binding]
public class CalculatorSteps
{
    [Given("I have entered (.*) into the calculator")]
    public void GivenIHaveEnteredIntoTheCalculator(int number) { _ = number; }

    [When("I press add")]
    public void WhenIPressAdd() { }

    [Then("the result should be (.*) on the screen")]
    public void ThenTheResultShouldBe(int expected) { _ = expected; }

    [Then("there should be a step from another binding class")]
    public void ThenThereShouldBeAStepFromAnotherBindingClass() { }

    // A Unicode regex, mirroring the SampleProjectGenerator's unicode binding.
    [Given("Unicode Алло Χαίρετε Árvíztűrő tükörfúrógép (.*)")]
    public void GivenUnicodeStep(string value) { _ = value; }

    [BeforeScenario]
    public void BeforeScenarioHook() { }
}

[Binding]
public class AdditionalSteps
{
    [Then("there should be a step from an external assembly")]
    public void ThenThereShouldBeAStepFromAnExternalAssembly() { }

    [Then("there should be a step from a plugin")]
    public void ThenThereShouldBeAStepFromAPlugin() { }

    [AfterScenario]
    public void AfterScenarioHook() { }
}
