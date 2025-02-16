﻿using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomRoles.Structures;
using UnityEngine;

namespace UncomplicatedCustomRoles.Manager
{
    public class SpawnManager
    {
        public static void RegisterCustomSubclass(ICustomRole Role)
        {
            if (SubclassValidator(Role) && !Plugin.CustomRoles.ContainsKey(Role.Id))
            {
                Plugin.CustomRoles.Add(Role.Id, Role);
                Plugin.RolesCount.Add(Role.Id, 0);
                Log.Info($"Successfully registered the UCR role with the ID {Role.Id} and {Role.Name} as name!");
                return;
            }
            Log.Warn($"Failed to register the UCR role with the ID {Role.Id}!");
        }
        public static bool SubclassValidator(ICustomRole Role)
        {
            if (Role == null)
            {
                return false;
            } else
            {
                if (Role.Spawn == SpawnLocationType.ZoneSpawn && Role.SpawnZones.Count() < 1)
                {
                    Log.Warn($"The UCR custom role with the ID {Role.Id} failed the check: if you select the ZoneSpawn as SpawnType the List SpawnZones can't be empty!");
                    return false;
                } else if (Role.Spawn == SpawnLocationType.RoomsSpawn && Role.SpawnRooms.Count() < 1)
                {
                    Log.Warn($"The UCR custom role with the ID {Role.Id} failed the check: if you select the RoomSpawn as SpawnType the List SpawnRooms can't be empty!");
                    return false;
                } else if (Role.Spawn == SpawnLocationType.PositionSpawn && Role.SpawnPosition == new UnityEngine.Vector3(0, 0, 0))
                {
                    Log.Warn($"The UCR custom role with the ID {Role.Id} failed the check: if you select the PositionSpawn as SpawnType the Vector3 SpawnPosition can't be empty!");
                    return false;
                }
                else if (Role.MinPlayers == 0)
                {
                    Log.Warn($"The UCR custom role with the ID {Role.Id} failed the check: the value of MinPlayers field must be greater than or equals to 1!");
                    return false;
                }
                else if (Role.MovementBoost > 255)
                {
                    Log.Warn($"The UCR custom role with the ID {Role.Id} failed the check: the value of MovementBoost field must be a number between 0 and 255.");
                    return false;
                }
                return true;
            }
        }
        public static void SummonCustomSubclass(Player Player, int Id, bool DoBypassRoleOverwrite = true)
        {
            // Does the role exists?
            if (!Plugin.CustomRoles.ContainsKey(Id))
            {
                Log.Warn($"Sorry but the role with the Id {Id} is not registered inside UncomplicatedCustomRoles!");
                return;
            }
            ICustomRole Role = Plugin.CustomRoles[Id];
            if (!DoBypassRoleOverwrite && !Role.CanReplaceRoles.Contains(Player.Role.Type))
            {
                Log.Debug($"Can't spawn the player {Player.Nickname} as UCR custom role {Role.Name} because it's role is not in the overwrittable list of custom role!\nStrange because this should be managed correctly by the plugin!");
                return;
            }
            RoleSpawnFlags SpawnFlag = RoleSpawnFlags.None;
            if (Role.Spawn == SpawnLocationType.KeepRoleSpawn)
            {
                SpawnFlag = RoleSpawnFlags.UseSpawnpoint;
            }
            // It's all OK so we can start to elaborate the spawn
            Player.Role.Set(Role.Role, SpawnFlag);
            Vector3 BasicPosition = Player.Position;
            Player.ChangeAppearance(Role.RoleAppearance);
            if (Role.Spawn == SpawnLocationType.KeepRoleSpawn)
            {
                Player.Position = BasicPosition;
            }
            if (SpawnFlag == RoleSpawnFlags.None)
            {
                switch (Role.Spawn)
                {
                    case SpawnLocationType.ZoneSpawn:
                        Player.Position = Factory.AdjustRoomPosition(Factory.RoomsInZone(Role.SpawnZones.RandomItem()).RandomItem());
                        break;
                    case SpawnLocationType.CompleteRandomSpawn:
                        Player.Position = Factory.AdjustRoomPosition(Factory.GetRoomList().RandomItem());
                        break;
                    case SpawnLocationType.RoomsSpawn:
                        Player.Position = Factory.AdjustRoomPosition(Room.Get(Role.SpawnRooms.RandomItem()));
                        if (Role.SpawnOffset != new Vector3())
                        {
                            Vector3 OldPosition = Player.Position;
                            OldPosition.x += Role.SpawnOffset.x;
                            OldPosition.y += Role.SpawnOffset.y;
                            OldPosition.z += Role.SpawnOffset.z;
                            Player.Position = OldPosition;
                        }
                        break;
                    case SpawnLocationType.PositionSpawn:
                        Player.Position = Role.SpawnPosition;
                        break;
                };
            }
            Player.ResetInventory(Role.Inventory);
            if (Role.CustomItemsInventory.Count() > 0)
            {
                foreach (uint ItemId in Role.CustomItemsInventory)
                {
                    if (!Player.IsInventoryFull)
                    {
                        CustomItem.Get(ItemId).Give(Player);
                    }
                }
            }
            // Now add all ammos
            if (Role.Ammo.GetType() == typeof(Dictionary<AmmoType, ushort>) && Role.Ammo.Count() > 0)
            {
                foreach (KeyValuePair<AmmoType, ushort> Ammo in Role.Ammo)
                {
                    Player.AddAmmo(Ammo.Key, Ammo.Value);
                }
            }
            // Player.Group.BadgeColor = Role.Badge.Color;
            // Player.Group.BadgeText = Role.Badge.Name;
            // Player.Group.Permissions = Player.Group.Permissions;
            Player.CustomInfo = Role.Name + ("\n" + Role.CustomInfo ?? string.Empty);
            Player.MaxHealth = Role.MaxHealth;
            Player.Health = Role.Health;
            Player.ArtificialHealth = Role.Ahp;
            Player.EnableEffect<MovementBoost>(Role.MovementBoost);
            if (Role.Effects.Count() > 0)
            {
                foreach (IEffect effect in Role.Effects)
                {
                    Player.EnableEffect(effect.EffectType, effect.Duration);
                    Player.ChangeEffectIntensity(effect.EffectType, effect.Intensity);
                }
            }
            if (Role.HumeShield > 0)
            {
                Player.HumeShield = Role.HumeShield;
            }
            if (Role.Scale != new Vector3(0, 0, 0))
            {
                Player.Scale = Role.Scale;
            }
            Player.Broadcast(Role.SpawnBroadcastDuration, Role.SpawnBroadcast);
            // Add the player to the player classes list
            Plugin.PlayerRegistry.Add(Player.Id, Role.Id);
        }
    }   
}