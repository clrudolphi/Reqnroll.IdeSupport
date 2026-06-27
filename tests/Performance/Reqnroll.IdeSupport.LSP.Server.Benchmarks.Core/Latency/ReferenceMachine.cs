#nullable enable

using System;

namespace Reqnroll.IdeSupport.LSP.Server.Benchmarks.Latency;

/// <summary>
/// Encapsulates the §9 rule that <b>absolute</b> latency thresholds are asserted only on a
/// designated reference machine — shared CI runners are too noisy for absolute pass/fail. Everywhere
/// else the suite reports numbers but always exits success.
/// </summary>
/// <remarks>
/// "Designated" is signalled by the <c>REQNROLL_PERF_REFERENCE_MACHINE</c> environment variable
/// (truthy values: <c>1</c>/<c>true</c>/<c>yes</c>) or by the <c>--assert</c> CLI flag.
/// </remarks>
public sealed class ReferenceMachine
{
    public const string ReferenceMachineEnvVar = "REQNROLL_PERF_REFERENCE_MACHINE";

    public bool AssertThresholds { get; }

    public ReferenceMachine(bool assertThresholds) => AssertThresholds = assertThresholds;

    /// <summary>
    /// Resolves the gate from the environment and CLI args: assertions are on if the reference-machine
    /// env var is truthy OR <c>--assert</c> is present.
    /// </summary>
    public static ReferenceMachine FromEnvironment(string[]? args = null)
    {
        var fromEnv = IsTruthy(Environment.GetEnvironmentVariable(ReferenceMachineEnvVar));
        var fromFlag = args is not null && Array.Exists(args, a =>
            string.Equals(a, "--assert", StringComparison.OrdinalIgnoreCase));
        return new ReferenceMachine(fromEnv || fromFlag);
    }

    private static bool IsTruthy(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
