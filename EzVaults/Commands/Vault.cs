using EzVaults.Enums;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using System.IO;

namespace EzVaults.Commands
{
    public class Vault : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public List<string> Permissions
        {
            get
            {
                return new List<string>() {
                    "ezvaults.vault"
                };
            }
        }
        public string Name = "vault";
        public string Help => "Edit your vault.";
        public string Syntax => "";
        public List<string> Aliases => new List<string> { "locker" };
        string IRocketCommand.Name => "vault";
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer Player = (UnturnedPlayer)caller;
            if (Player.IsInVehicle &&EzVaults.Instance.Configuration.Instance.AllowVehicle==0)
            {
                UnturnedChat.Say(Player, EzVaults.Instance.Translate(EResponse.IN_VEHICLE.ToString()), EzVaults.Instance.Configuration.Instance.Color, true);
                return;
            }
            if(EzVaults.Instance.Configuration.Instance.CacheTime == 0 && EzVaults.vaultCurrent.ContainsKey(Player))
            {
                UnturnedChat.Say(Player, EzVaults.Instance.Translate(EResponse.DUPE.ToString()), EzVaults.Instance.Configuration.Instance.Color, true);
                return;
            }
            List<int> PPerms = new List<int>();
            //for (int i = 0; i < EzVaults.Instance.Configuration.Instance.VaultPerms.Count; i++)
            //{
            //    if (Player.HasPermission(EzVaults.Instance.Configuration.Instance.VaultPerms[i])) PPerms.Add(i);
            //}
            for (int i = 0; i < EzVaults.Instance.Configuration.Instance.Vaulter.Count; i++) { if (Player.HasPermission(EzVaults.Instance.Configuration.Instance.Vaulter[i].Permission)) PPerms.Add(i); }
            int SVault = -1;
            if (command.Length != 0)
            {
                int x = EzVaults.Instance.Configuration.Instance.Vaulter.FindIndex(k=>k.Name==command[0]||(EzVaults.Instance.Configuration.Instance.ignoreCase&& k.Name.ToLower()==command[0].ToLower()));
                if (x == -1)
                {
                    UnturnedChat.Say(Player, EzVaults.Instance.Translate(EResponse.VAULT_NOT_FOUND.ToString()), EzVaults.Instance.Configuration.Instance.Color, true);
                    return;
                }
                else
                if (PPerms.Contains(x))
                {
                    SVault = x;
                }
                else
                {
                    UnturnedChat.Say(Player, EzVaults.Instance.Translate(EResponse.VAULT_NO_PERMISSION.ToString(), SVault), EzVaults.Instance.Configuration.Instance.Color, true);
                    return;
                }
            }
            else if(PPerms.Count>0)
            {
                SVault = PPerms[0];
            }
            if (SVault != -1) {
                if (Player.Player.equipment.IsEquipAnimationFinished || Player.Player.equipment.HasValidUseable)Player.Player.equipment.dequip();
                await System.Threading.Tasks.Task.Run(() =>
                {
                    EzVaults.LoadVault(Player, SVault, this);
                });
            } else {
                UnturnedChat.Say(Player, EzVaults.Instance.Translate(EResponse.NO_PERMISSION.ToString()), EzVaults.Instance.Configuration.Instance.Color, true);
            }
        }
    }
}
