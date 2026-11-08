// <copyright file="ActorJsonTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.TestActors;

using MUnique.OpenMU.GameLogic.TestActors;
using NUnit.Framework;

/// <summary>
/// Tests for the TestActors JSON wire representation.
/// </summary>
[TestFixture]
public class ActorJsonTests
{
    /// <summary>
    /// A payload field named <c>type</c> cannot replace the event-kind metadata.
    /// </summary>
    [Test]
    public void EventTypeMetadataIsNotOverwrittenByFieldNamedType()
    {
        var actorEvent = new ActorEvent(
            42,
            new DateTime(2026, 11, 8, 10, 44, 1, DateTimeKind.Utc),
            "mini_game_state",
            [new ActorEventField("type", "DevilSquare"), new ActorEventField("state", "Closed")]);

        var result = ActorJson.ToDictionary(actorEvent);

        Assert.That(result["type"], Is.EqualTo("mini_game_state"));
        Assert.That(result["field_type"], Is.EqualTo("DevilSquare"));
        Assert.That(result["state"], Is.EqualTo("Closed"));
    }
}
