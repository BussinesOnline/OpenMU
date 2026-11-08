// <copyright file="EnterMiniGameCommand.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TestActors;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// Enters a configured mini game using the native entry action. This command exists only in the
/// isolated TestActors control adapter; it does not implement or bypass event rules.
/// </summary>
/// <param name="Type">The configured mini game type.</param>
/// <param name="Level">The mini game bracket.</param>
/// <param name="TicketSlot">The zero-based inventory slot of the ticket, or 255 to find the configured ticket.</param>
public sealed record EnterMiniGameCommand(MiniGameType Type, int Level, byte TicketSlot) : ActorCommand("enter_minigame");
