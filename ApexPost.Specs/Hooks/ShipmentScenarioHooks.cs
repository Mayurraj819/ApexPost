using ApexPost.Specs.Infrastructure;
using Reqnroll;

namespace ApexPost.Specs.Hooks;

// Copilot-assisted hook draft; cleanup and scope corrected during review.
[Binding]
public sealed class ShipmentScenarioHooks(ShipmentScenarioState state)
{
    [BeforeScenario("sql")]
    public Task BeforeScenarioAsync() => state.StartAsync();

    [AfterScenario("sql")]
    public Task AfterScenarioAsync() => state.StopAsync();
}
