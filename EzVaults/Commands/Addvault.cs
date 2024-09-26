using EzVaults.Enums;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace EzVaults.Commands
{
    class AddVault : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public List<string> Permissions
        {
            get
            {
                return new List<string>() {
                    "ezvaults.AddVault"
                };
            }
        }
        public string Name = "addvault";
        public string Help => "Create a new vault.";
        public string Syntax => "<name> <permission> <width> <height>";
        public List<string> Aliases => new List<string> { "createvault" };
        string IRocketCommand.Name => "addvault";
        public void Execute(IRocketPlayer caller, string[] command)
        {
            try{
            string n=command[0];string p=command[1];byte w=byte.Parse(command[2]);byte h=byte.Parse(command[3]);
            if(n==""||p==""||w==0||h==0){UnturnedChat.Say(caller,"<name> <permission> <width> <height>");return;}
            EzVaults.Instance.Configuration.Instance.Vaulter.Add(new Vaulter(n,p,w,h));
            EzVaults.Instance.Configuration.Save();
            UnturnedChat.Say(caller, n+" has been created with width "+w+" and height "+h);
            }catch(System.Exception){ UnturnedChat.Say(caller, "<name> <permission> <width> <height>"); }
        }
    }
}
