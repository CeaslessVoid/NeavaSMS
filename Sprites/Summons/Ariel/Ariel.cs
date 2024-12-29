using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeavaSMS.Common.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NeavaSMS.Sprites.Summons.Ariel
{
    public class Ariel : ModProjectile
    {
        private const int FrameWidth = 766;
        private const int FrameHeight = 506;
        private const float GravityForce = 0.4f;
        private const float MaxFallSpeed = 12f;
        private const int GroundedBufferTime = 10;
        private const int TeleportDistanceThreshold = 800;
        private const float StartWalkingDistance = 120f;
        private const float StopWalkingDistance = 80f;
        private const int TargetUpdateInterval = 120;
        private const float JumpForce = -16f;
        private const float MovementSpeed = 6f;

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
            Projectile.damage = 0;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            SMSsummon summonData = Projectile.GetGlobalProjectile<SMSsummon>();

            // Reset timeLeft to ensure the projectile remains active
            Projectile.timeLeft = 2;

            HandleGroundCollision();
            HandleTeleportation(player);
            HandleMovement(player, summonData);
            AnimateProjectile(summonData);
        }

        private void HandleGroundCollision()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Bottom.Y / 16f);
            Tile tileBelow = Framing.GetTileSafely(tileX, tileY + 1);

            bool isOnGround = tileBelow.HasTile && Main.tileSolid[tileBelow.TileType];
            int groundedBuffer = (int)Projectile.localAI[1];

            if (isOnGround)
                groundedBuffer = GroundedBufferTime;
            else if (groundedBuffer > 0)
                groundedBuffer--;

            isOnGround = groundedBuffer > 0;
            Projectile.localAI[1] = groundedBuffer;

            if (!isOnGround)
            {
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + GravityForce, MaxFallSpeed);
            }
            else
            {
                Projectile.velocity.Y = 0;
            }
        }

        private void HandleTeleportation(Player player)
        {
            float distanceToPlayer = Vector2.Distance(Projectile.Center, player.Center);
            if (distanceToPlayer > TeleportDistanceThreshold)
            {
                Projectile.Center = player.Center;
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.MagicMirror);
                }
            }
        }

        private void HandleMovement(Player player, SMSsummon summonData)
        {
            if (Projectile.ai[0] % TargetUpdateInterval == 0 || Projectile.ai[1] == 0)
            {
                Projectile.localAI[0] = player.Center.X;
            }
            Projectile.ai[0]++;

            Vector2 targetPosition = new Vector2(Projectile.localAI[0], Projectile.Center.Y);
            Vector2 moveTo = targetPosition - Projectile.Center;

            if (moveTo.Length() > StartWalkingDistance)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Running;
                moveTo.Normalize();
                moveTo *= MovementSpeed;
                Projectile.velocity = (Projectile.velocity * 20f + moveTo) / 21f;

                summonData.spriteEffects = Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                if (IsObstacleInPath())
                {
                    Projectile.velocity.Y = JumpForce;
                }
            }
            else if (moveTo.Length() < StopWalkingDistance)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Idle;
                Projectile.velocity.X *= 0.1f;
            }
        }

        private bool IsObstacleInPath()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Bottom.Y / 16f);
            int direction = Math.Sign(Projectile.velocity.X);

            if (Projectile.velocity.X == 0) return false;

            for (int i = 0; i < 3; i++)
            {
                for (int offset = 2; offset <= 4; offset++)
                {
                    Tile tileInPath = Framing.GetTileSafely(tileX + (direction * offset), tileY - i);
                    if (tileInPath.HasTile && Main.tileSolid[tileInPath.TileType])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AnimateProjectile(SMSsummon summonData)
        {
            Texture2D texture = summonData.CurrentAnimationState == SMSsummon.AnimationState.Idle
                ? ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Idle").Value
                : ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Run").Value;

            int totalFrames = texture.Height / FrameHeight;
            Projectile.frameCounter++;

            if (Projectile.frameCounter >= 10)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % totalFrames;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SMSsummon summonData = Projectile.GetGlobalProjectile<SMSsummon>();
            Texture2D texture = summonData.CurrentAnimationState == SMSsummon.AnimationState.Idle
                ? ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Idle").Value
                : ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Run").Value;

            Rectangle frame = new Rectangle(0, Projectile.frame * FrameHeight, FrameWidth, FrameHeight);
            Vector2 offset = summonData.spriteEffects == SpriteEffects.None
                ? new Vector2(240, -115)
                : new Vector2(-240, -115);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + offset;

            Main.EntitySpriteDraw(texture, drawPosition, frame, lightColor, Projectile.rotation,
                new Vector2(FrameWidth / 2, FrameHeight / 2), Projectile.scale, summonData.spriteEffects, 0);

            return false;
        }
    }

}
