using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomPhoneCalls
{
    public class CustomCall
    {
        public CustomCall() { }
        public string npc;
        public string[] dialogue;

        public void Receive()
        {
            ModEntry.receivedCalls.Add(ModEntry.currentCall);
            if (npc is null || dialogue is null || !dialogue.Any()) return;
            if (npc == "")
                Game1.multipleDialogues(dialogue);
            else
            {
                var c = Game1.getCharacterFromName(npc);
                if (c is null) c = new NPC(Game1.player.sprite, Vector2.Zero, "", 0, npc, false, null, Game1.temporaryContent.Load<Texture2D>("Portraits\\" + npc));
                Game1.drawDialogue(c, String.Join("#$b#", dialogue));
            }
        }
    }
}
