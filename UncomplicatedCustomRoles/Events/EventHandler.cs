﻿using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomRoles.Manager;
using UncomplicatedCustomRoles.Structures;
using MEC;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using Utf8Json.Formatters;
using CustomPlayerEffects;
using Exiled.CustomRoles.API.Features;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace UncomplicatedCustomRoles.Events
{
    public class EventHandler
    {
        public void OnRoundStarted()
        {
            // Check for all subclasses and all spawn percentage
            Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage = Factory.RoleIstance();
            foreach (KeyValuePair<int, ICustomRole> Role in Plugin.CustomRoles)
            {
                if (!Role.Value.IgnoreSpawnSystem && Role.Value.SpawnCondition == SpawnCondition.RoundStart && Player.List.Count() >= Role.Value.MinPlayers)
                {
                    foreach (RoleTypeId RoleType in Role.Value.CanReplaceRoles)
                    {
                        for (int a = 0; a < Role.Value.SpawnChance; a++)
                        {
                            RolePercentage[RoleType].Add(Role.Value);
                        }
                    }
                }
            }

            // Now check all the player list and assign a custom subclasses for every role
            foreach (Player Player in Player.List)
            {
                if (RolePercentage.ContainsKey(Player.Role.Type))
                {
                    // We can proceed with the chance
                    int Chance = new Random().Next(0, 100);
                    if (Chance < RolePercentage[Player.Role.Type].Count())
                    {
                        // The role exists, good, let's give the player a role
                        int RoleId = RolePercentage[Player.Role.Type].RandomItem().Id;
                        if (Plugin.RolesCount[RoleId] < Plugin.CustomRoles[RoleId].MaxPlayers)
                        {
                            Timing.RunCoroutine(DoSpawnPlayer(Player, RoleId, false));
                            Plugin.RolesCount[RoleId]++;
                            Log.Debug($"Player {Player.Nickname} spawned as CustomRole {RoleId}");
                        }
                        else
                        {
                            Log.Debug($"Player {Player.Nickname} won't be spawned as CustomRole {RoleId} because it has reached the maximus number");
                        }
                    }
                }
            }
        }
        public void OnRespawningTeam(RespawningTeamEventArgs Respawn)
        {
            Log.Debug("Respawning event, reset the queue");
            Plugin.RoleSpawnQueue.Clear();

            foreach (Player Player in Respawn.Players.ToList())
            {
                Plugin.RoleSpawnQueue.Add(Player.Id);
                Log.Debug($"Player {Player.Nickname} queued for spawning as CustomRole, will be defined when spawned");
            }
        }
        public void OnPlayerSpawned(SpawnedEventArgs Spawned)
        {
            Log.Debug($"Player {Spawned.Player.Nickname} spawned, going to assign a role if needed!");
            Timing.CallDelayed(0.1f, () =>
            {
                if (Plugin.RoleSpawnQueue.Contains(Spawned.Player.Id))
                {
                    Log.Debug($"Assigning a role to {Spawned.Player.Nickname}");
                    Plugin.RoleSpawnQueue.Remove(Spawned.Player.Id);
                    Timing.RunCoroutine(DoElaborateSpawnPlayerFromWave(Spawned.Player, false));
                    Log.Debug($"Player {Spawned.Player.Nickname} successfully spawned as CustomRole {Plugin.RoleSpawnQueue[Spawned.Player.Id]}");
                }
            });
        }
        public void OnDied(DiedEventArgs Died)
        {
            if (Plugin.PlayerRegistry.ContainsKey(Died.Player.Id))
            {
                Plugin.RolesCount[Plugin.PlayerRegistry[Died.Player.Id]]--;
                Plugin.PlayerRegistry.Remove(Died.Player.Id);
                Died.Player.CustomInfo = "";
                // Died.Player.Group = new UserGroup();
            }
        }
        public void OnSpawning(SpawningEventArgs Spawning)
        {
            if (Plugin.PlayerRegistry.ContainsKey(Spawning.Player.Id))
            {
                Plugin.PlayerRegistry.Remove(Spawning.Player.Id);
                Spawning.Player.CustomInfo = "";
                // Spawning.Player.Group = new UserGroup();
            }
        }
        public void OnEscaping(EscapingEventArgs Escaping)
        {
            if (Plugin.PlayerRegistry.ContainsKey(Escaping.Player.Id))
            {
                int RoleId = Plugin.PlayerRegistry[Escaping.Player.Id];
                ICustomRole Role = Plugin.CustomRoles[RoleId];
                if (!Role.CanEscape)
                {
                    Escaping.IsAllowed = false;
                }
            }
        }
        public static IEnumerator<float> DoSpawnPlayer(Player Player, int Id, bool DoBypassRoleOverwrite = true)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnManager.SummonCustomSubclass(Player, Id, DoBypassRoleOverwrite);
        }
        public static IEnumerator<float> DoElaborateSpawnPlayerFromWave(Player Player, bool DoBypassRoleOverwrite = true)
        {
            yield return Timing.WaitForSeconds(0.1f);
            Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage = Factory.RoleIstance();
            foreach (KeyValuePair<int, ICustomRole> Role in Plugin.CustomRoles)
            {
                if (!Role.Value.IgnoreSpawnSystem && Role.Value.CanReplaceRoles.Contains(Player.Role.Type) && Role.Value.MaxPlayers > Plugin.RolesCount[Role.Value.Id] && Role.Value.MinPlayers >= Player.List.Count())
                {
                    foreach (RoleTypeId RoleType in Role.Value.CanReplaceRoles)
                    {
                        for (int a = 0; a < Role.Value.SpawnChance; a++)
                        {
                            RolePercentage[RoleType].Add(Role.Value);
                        }
                    }
                }
            }
            int Chance = new Random().Next(0, 100);
            if (Chance <= RolePercentage.Count())
            {
                yield break;
            }
            if (RolePercentage[Player.Role.Type].Count() > 0)
            {
                int RoleId = RolePercentage[Player.Role.Type].RandomItem().Id;
                Plugin.RolesCount[RoleId]++;
                Timing.RunCoroutine(DoSpawnPlayer(Player, RoleId, false));
                yield break;
            }
            yield break;
        }
    }
}