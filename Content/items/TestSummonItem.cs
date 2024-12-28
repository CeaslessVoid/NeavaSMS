using NeavaSMS.Sprites.Summons.Ariel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NeavaSMS.Content.items
{
    public class TestSummonItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Ariel>()] < 1)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, player.velocity,
                    ModContent.ProjectileType<Ariel>(), 10, 0f, player.whoAmI);
            }
        }
    }
}
