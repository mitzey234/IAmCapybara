using CommandSystem;
using HarmonyLib;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using RemoteAdmin;
using System;
using System.Linq;
using Interactables.Interobjects;
using Mirror;
using UnityEngine;
using ICommand = CommandSystem.ICommand;
using Object = UnityEngine.Object;

namespace IAmCapybara
{
    public class Plugin
    {
        public static Player targetPlayer;

        public static bool spin = false;

        public static float speed = 2f;
        
        public static bool perspective = false;
        
        internal static Plugin Instance { get; private set; }

        private Harmony Harmony { get; set; }

        [PluginEntryPoint("IAmCapybara", "1.0.6", "You cannot stop him", "Mitzey")]
        void LoadPlugin()
        {
            Harmony = new Harmony(PluginHandler.Get(this).PluginName);
            Harmony.PatchAll();
            
            Instance = this;
        }
        
        [PluginConfig]
        public Config Config;
        
        public static void SetScale(Player player, Player target, Vector3 scale, bool visibleToPlayer)
        {
            try
            {
                player.ReferenceHub.transform.localScale = scale;
                if (target == null)
                {
                    foreach (Player t in Player.GetPlayers())
                    {
                        if (visibleToPlayer || t.UserId != player.UserId)
                        {
                            NetworkServer.SendSpawnMessage(player.ReferenceHub.characterClassManager.netIdentity, t.Connection);
                        }
                    }
                }
                else
                {
                    NetworkServer.SendSpawnMessage(player.ReferenceHub.characterClassManager.netIdentity, target.Connection);
                }
                if (!visibleToPlayer) player.ReferenceHub.transform.localScale = Vector3.one;
            }
            catch (Exception exception)
            {
                Log.Error($"Set scale error: {exception}");
            }
        }
    }

    class CustomComponent : MonoBehaviour
    {
        float Timer = 0f;

        public static bool active = false;

        bool firstStart = false;

        private float rotation = 0f;

        private Vector3 temp = new Vector3(0f, 0.4f, 0f);

        public Scp956Pinata instance;

        public void Awake()
        {
            
        }

        public void Start()
        {

        }

        //Updates 45x a second
        public void Update()
        {
            Timer += Time.deltaTime;
            if (Timer <= 1f/45f) return;
            Timer = 0f;

            rotation += Plugin.speed;
            if (rotation >= 360f) rotation = 0;

            if (instance == null) return;

            if ((Plugin.targetPlayer == null || Plugin.targetPlayer.Equals(null)) && !active) return;

            if (active && Plugin.targetPlayer == null && Scp956Pinata.IsSpawned)
            {
                instance.Network_syncPos = new Vector3(0, 0, 0);
                instance._spawnPos = new Vector3(0, 0, 0);
                instance.Network_spawned = false;
                active = false;
                return;
            }
            else if (active && Plugin.targetPlayer == null)
            {
                active = false;
                return;
            }


            if (Plugin.targetPlayer != null)
            {
                if (Plugin.targetPlayer.Equals(null))
                {
                    Plugin.targetPlayer = null;
                    return;
                }
                if (!Plugin.targetPlayer.IsAlive)
                {
                    Plugin.targetPlayer = null;
                    return;
                }
                if (!active)
                {
                    active = true;
                    firstStart = true;
                }
            }
            else
            {
                return;
            }

            if (!active) return;

            if (firstStart && Scp956Pinata.IsSpawned)
            {
                firstStart = false;
                instance.Network_syncPos = new Vector3(0, 0, 0);
                instance._spawnPos = new Vector3(0, 0, 0);
                instance.Network_spawned = false;
                return;
            }
            else if (!Scp956Pinata.IsSpawned)
            {
                firstStart = false;
                instance.Network_carpincho = (byte)69;
                Scp956Pinata.IsSpawned = true;
            }
            Vector3 vector = Plugin.targetPlayer.Position + (Plugin.perspective ? temp : Vector3.zero);
            instance.Network_syncPos = vector;
            instance._spawnPos = vector;
            instance.Network_syncRot = Plugin.spin ? rotation : Plugin.targetPlayer.Camera.rotation.eulerAngles.y;
            instance.Network_spawned = true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class PluginCommand : ParentCommand
    {
        public override string Command => "iam";

        public override string[] Aliases => Array.Empty<string>();

        public override string Description => "Controls some funnies";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new CapybaraCommand());
            RegisterCommand(new FlyingCommand());
            RegisterCommand(new FakeAttackCommand());
            RegisterCommand(new AttackCommand());
            RegisterCommand(new SpinCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                response = "Plugin Menu:\n" +
                    "iam capybara - Makes you the capybara, or makes you not the capybara\n" +
                    "iam flying - Makes the capybara fly\n" +
                    "iam fake - Plays a fake attack noise.. Probably\n" +
                    "iam attack - attacks the closest player\n" +
                    "iam spin - Makes the capybara spin\n" +
                    "iam perspective - Shrinks your perspective to the small boi";
                return true;
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class CapybaraCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "Tis quite funny";

        string ICommand.Command { get; } = "capybara";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (Plugin.targetPlayer == target)
                {
                    Plugin.targetPlayer = null;
                    response = "You are no longer a capybara";
                    return true;
                }

                Plugin.targetPlayer = Player.Get(sender);
                Plugin.SetScale(Plugin.targetPlayer, null, new Vector3(1f, 0.53f, 1f), Plugin.perspective);
                response = "You are now a capybara";
                return true;
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class FlyingCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "";

        string ICommand.Command { get; } = "flying";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (Plugin.targetPlayer == target)
                {
                    Scp956Pinata._instance.Network_flying = !Scp956Pinata._instance.Network_flying;
                    response = Scp956Pinata._instance.Network_flying ? "You're now flying!" : "You're no longer flying";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class FakeAttackCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "";

        string ICommand.Command { get; } = "fake";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (Plugin.targetPlayer == target)
                {
                    Scp956Pinata._instance.RpcAttack();
                    response = "Spooky, musta been scary";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class AttackCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "";

        string ICommand.Command { get; } = "attack";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (Plugin.targetPlayer == target)
                {
                    Player actualTarget = null;
                    foreach (Player p in Player.GetPlayers().Where(p => p.IsAlive && !p.IsSCP && !p.IsServer && p != Plugin.targetPlayer))
                    {
                        if (actualTarget == null)
                        {
                            actualTarget = p;
                            continue;
                        }
                        if (actualTarget == p) continue;
                        if ((Plugin.targetPlayer.Position - p.Position).sqrMagnitude < (Plugin.targetPlayer.Position - actualTarget.Position).sqrMagnitude) actualTarget = p;
                    }

                    if (actualTarget == null)
                    {
                        response = "Could not find target!";
                        return false;
                    }

                    Scp956Pinata._instance.RpcAttack();
                    Vector3 normalized = (actualTarget.Position - Scp956Pinata._instance._tr.position).normalized;
                    actualTarget.ReferenceHub.playerStats.DealDamage(new Scp956DamageHandler(normalized));
                    response = "Ouch! That looked like it hurt..";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }

    [CommandHandler(typeof(PluginCommand))]
    class SpinCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "SPEEN";

        string ICommand.Command { get; } = "spin";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (Plugin.targetPlayer == target)
                {
                    if (arguments.Count >= 1)
                    {
                        float speed;
                        try
                        {
                            speed = float.Parse(arguments.First());
                        }
                        catch
                        {
                            response = "Failed to parse argument";
                            return true;
                        }

                        Plugin.speed = speed;
                        response = "Set speed to: " + speed;
                        return true;
                    }
                    Plugin.spin = !Plugin.spin;
                    response = Plugin.spin ? "SPEEEEEEEN" : "oof..";
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }
    
    [CommandHandler(typeof(PluginCommand))]
    class PerspectiveCommand : ICommand
    {
        public string[] Aliases { get; set; } = Array.Empty<string>();

        public string Description { get; set; } = "Shrinks your perspective to the small boi";

        string ICommand.Command { get; } = "perspective";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                if (!Plugin.Instance.Config.Authorized.Contains(playerSender.ReferenceHub.authManager.UserId))
                {
                    response = "You do not have permission to use this command!";
                    return false;
                }

                Player target = Player.Get(sender);

                if (target == null)
                {
                    response = "I Couldn't find your player object!";
                    return false;
                }

                if (Plugin.targetPlayer == target)
                {
                    Plugin.perspective = !Plugin.perspective;
                    response = Plugin.perspective ? "Smol" : "not so smol..";
                    if (!Plugin.perspective) Plugin.SetScale(Plugin.targetPlayer, null, new Vector3(1f, 1f, 1f), true);
                    Plugin.SetScale(Plugin.targetPlayer, null, new Vector3(1f, 0.53f, 1f), Plugin.perspective);
                    return true;
                }
                else
                {
                    response = "You're no capybara!";
                    return false;
                }
            }
            else
            {
                response = "Only players may use this command";
                return false;
            }
        }
    }
    
    [HarmonyPatch(typeof(Scp956Pinata))]
    class Patches
    {
        [HarmonyPatch(nameof(Scp956Pinata.Awake))]
        [HarmonyPostfix]
        public static void Awake(Scp956Pinata __instance)
        {
            __instance.gameObject.AddComponent<CustomComponent>().instance = __instance;
        }

        [HarmonyPatch(nameof(Scp956Pinata.UpdateAi))]
        [HarmonyPrefix]
        public static bool UpdateAi(Scp956Pinata __instance)
        {
            if (Plugin.targetPlayer != null || CustomComponent.active) return false;
            return true;
        }
    }
}