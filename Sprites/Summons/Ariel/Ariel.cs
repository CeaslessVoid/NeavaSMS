using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeavaSMS.Common.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace NeavaSMS.Sprites.Summons.Ariel
{
    public class Ariel : ModProjectile
    {

        private const int FrameWidth = 766;
        private const int FrameHeight = 506;
        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.knockBack = 0f;
            Projectile.DamageType = DamageClass.Generic;

            Projectile.damage = 10;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            SMSsummon summonData = Projectile.GetGlobalProjectile<SMSsummon>();

            Projectile.timeLeft = 2;

            const int targetUpdateInterval = 120;
            if (Projectile.ai[0] % targetUpdateInterval == 0 || Projectile.ai[1] == 0)
            {
                Projectile.localAI[0] = player.Center.X;
                Projectile.localAI[1] = player.Center.Y;
            }
            Projectile.ai[0]++;

            Vector2 targetPosition = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
            float speed = 6f;

            Vector2 moveTo = targetPosition - Projectile.Center;
            if (moveTo.Length() > 50)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Running;
                moveTo.Normalize();
                moveTo *= speed;
                Projectile.velocity = (Projectile.velocity * 20f + moveTo) / 21f;

                if (Projectile.velocity.X > 0)
                    summonData.spriteEffects = SpriteEffects.None;
                else if (Projectile.velocity.X < 0)
                    summonData.spriteEffects = SpriteEffects.FlipHorizontally;
                else
                {
                    summonData.CurrentAnimationState = SMSsummon.AnimationState.Idle;
                    Projectile.velocity *= 0f;
                }
            }
            else
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Idle;
                Projectile.velocity *= 0f;
            }

            AnimateProjectile(summonData);
        }

        private void AnimateProjectile(SMSsummon summonData)
        {
            Texture2D texture;

            if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Idle)
            {
                texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Idle").Value;
            }
            else
            {
                texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Run").Value;
            }

            int totalFrames = texture.Height / FrameHeight;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 10)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= totalFrames)
                {
                    Projectile.frame = 0;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SMSsummon summonData = Projectile.GetGlobalProjectile<SMSsummon>();
            Texture2D texture;

            if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Idle)
            {
                texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Idle").Value;
            }
            else
            {
                texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Run").Value;
            }


            int totalFrames = texture.Height / FrameHeight;
            Rectangle frame = new Rectangle(0, Projectile.frame * FrameHeight, FrameWidth, FrameHeight);

            Vector2 offset = summonData.spriteEffects == SpriteEffects.None
                ? new Vector2(240, -130)
                : new Vector2(-240, -130);

            Vector2 drawOrigin = new Vector2(FrameWidth / 2, FrameHeight / 2);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + offset;

            Main.EntitySpriteDraw(texture, drawPosition, frame, lightColor, Projectile.rotation,
                drawOrigin, Projectile.scale, summonData.spriteEffects, 0);

            DrawDebugBox(drawPosition, lightColor);

            return false;
        }

        private void DrawDebugBox(Vector2 position, Color lightColor)
        {
            Texture2D boxTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
            boxTexture.SetData(new Color[] { Color.Red });

            int boxWidth = FrameWidth;
            int boxHeight = FrameHeight;

            // Draw the box rectangle
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X - boxWidth / 2, position.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X - boxWidth / 2, position.Y + boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X - boxWidth / 2, position.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X + boxWidth / 2, position.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
        }
    }
}
