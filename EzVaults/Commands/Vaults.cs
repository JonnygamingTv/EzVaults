using EzVaults.Enums;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace EzVaults.Commands
{
    class Vaults : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public List<string> Permissions
        {
            get
            {
                return new List<string>() {
                    "ezvaults.vaults"
                };
            }
        }
        public string Name = "vaults";
        public string Help => "List your vaults.";
        public string Syntax => "";
        public List<string> Aliases => new List<string> { "lockers" };
        string IRocketCommand.Name => "vaults";
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer Player = (UnturnedPlayer)caller;
            List<string> PPerms = new List<string>();
            //for (int i = 0; i < EzVaults.Instance.Configuration.Instance.VaultPerms.Count; i++)
            //{
            //    if (Player.HasPermission(EzVaults.Instance.Configuration.Instance.VaultPerms[i])) PPerms.Add(EzVaults.Instance.Configuration.Instance.VaultNames[i]);
            //}
            for(int i=0;i<EzVaults.Instance.Configuration.Instance.Vaulter.Count;i++){ if (Player.HasPermission(EzVaults.Instance.Configuration.Instance.Vaulter[i].Permission)) PPerms.Add(EzVaults.Instance.Configuration.Instance.Vaulter[i].Name); }
            string Perms = "";
            for(int i = 0; i < PPerms.Count; i++) { Perms += (i == 0 ? "" : ", ") +PPerms[i]; }
            UnturnedChat.Say(Player, EzVaults.Instance.Translate(EResponse.VAULTS.ToString(),PPerms.Count,Perms), EzVaults.Instance.Configuration.Instance.Color, true);
        }
    }
}
