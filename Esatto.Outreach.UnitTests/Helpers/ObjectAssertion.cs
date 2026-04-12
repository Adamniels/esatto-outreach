using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Esatto.Outreach.UnitTests.Helpers;

/// <summary>
/// FluentAssertions 8+ gives <c>Should(this object)</c> lower overload priority than enum <c>Should&lt;TEnum&gt;</c>,
/// which breaks reference-type subjects on modern C#. Building <see cref="ObjectAssertions"/> directly matches core behavior.
/// </summary>
public static class ObjectAssertion
{
    public static ObjectAssertions Should(object? subject) =>
        new(subject!, AssertionChain.GetOrCreate());
}
