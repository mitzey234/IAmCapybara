using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Stores;
using LabApi.Features.Wrappers;
using Mirror.LiteNetLib4Mirror;
using RoundRestarting;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace IAmCapybara;

public class EventHandlers : CustomEventsHandler
{
    public override void OnPlayerHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker == IAmCapybara.targetPlayer && ev.Attacker != null)
        {
            ev.IsAllowed = false;
            var target = ev.Player;
            if (target.IsAlive)
            {
                if (target.IsSCP && !IAmCapybara.doesNotDiscriminate) return;
                IAmCapybara.Attack(target);
                ev.Attacker.SendHitMarker(2f);
            }
        }
    }

    public override void OnServerWaitingForPlayers()
    {
        Logger.Info("Pinata exists: " + (Scp956Pinata._instance != null));
    }

    public override void OnPlayerUpdatingEffect(PlayerEffectUpdatingEventArgs ev)
    {
        if (ev.Effect is Scp956Target && IAmCapybara.targetPlayer != null && ev.Intensity > 0)
        {
            ev.IsAllowed = false;
        }
    }
}