using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeavaSMS.Common.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NeavaSMS.Sprites.Summons.Ariel
{
    public class Ariel : ModProjectile
    {
        private const int FrameWidth = 766;
        private const int FrameHeight = 506;
        private const float GravityForce = 0.4f;
        private const float MaxFallSpeed = 32f;
        private const int GroundedBufferTime = 10;
        private const int TeleportDistanceThreshold = 800;
        private float StartWalkingDistance = 120f;
        private float StartWalkingDistanceEnemy = 300f;
        private const float StopWalkingDistance = 80f;
        private const int TargetUpdateInterval = 120;
        private const float JumpForce = -16f;
        private const float MovementSpeed = 6f;
        private NPC closestNPC;

        private Texture2D texture;
        private const float AttackRangeWidth = 700f;
        private const float AttackRangeHeight = 500f;
        private bool isAtking = false;

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

            Projectile.timeLeft = 2;

            HandleGroundCollision();
            HandleMovement(player, summonData);
            AnimateProjectile(summonData);
            HandleTeleportation(player);
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

            float mod = Projectile.velocity.Y > 0 ? 1.2f : 1.0f;

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

            Vector2 targetPosition = FindTarget(player);
            Vector2 moveTo = targetPosition - Projectile.Center;

            if (!isAtking)
                summonData.spriteEffects = moveTo.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (isAtking)
            {
                PerformAttack(summonData);
            }
            else if (moveTo.Length() > StartWalkingDistance)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Running;
                moveTo.Normalize();
                moveTo *= MovementSpeed;
                Projectile.velocity = (Projectile.velocity * 20f + moveTo) / 21f;

                if (IsObstacleInPath())
                {
                    Projectile.velocity.Y = JumpForce;
                }
            }
            else if (moveTo.Length() < StopWalkingDistance)
            {
                if (closestNPC != null)
                {
                    PerformAttack(summonData);
                }
                else
                {
                    summonData.CurrentAnimationState = SMSsummon.AnimationState.Idle;
                }
                Projectile.velocity.X *= 0f;
            }
        }

        private Vector2 FindTarget(Player player)
        {
            Vector2 target = new Vector2();

            NPC closestNPC = FindTargetNPC(player);

            if (closestNPC == null )
            {
                return new Vector2(Projectile.localAI[0], Projectile.Center.Y);
            }

            target = closestNPC.Center;
            target.Y = Projectile.Center.Y;

            return target;
        }

        private NPC FindTargetNPC(Player player)
        {
            if (closestNPC != null && closestNPC.active && Vector2.Distance(player.Center, closestNPC.Center) <= TeleportDistanceThreshold)
            {
                return closestNPC;
            }
            else
            {
                closestNPC = null;
            }

            float closestDistance = 9999;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.CountsAsACritter && npc.damage >= 1)
                {
                    float distance = Vector2.Distance(Projectile.Center, npc.Center);
                    float distance2 = Vector2.Distance(player.Center, npc.Center);

                    if (distance < closestDistance && distance2 <= TeleportDistanceThreshold)
                    {
                        closestDistance = distance;
                        closestNPC = npc;
                    }
                }
            }

            return closestNPC;
        }

        private void PerformAttack(SMSsummon summonData)
        {
            if (!isAtking && summonData.CurrentAnimationState != SMSsummon.AnimationState.Attacking)
            {
                Projectile.frame = 1;
            }

            if (Projectile.frame >= 1 && Projectile.frame <= 3)
            {
                Rectangle attackHitbox = GetAttackRectangle(summonData, 1);

                CheckForHostileNPCs(attackHitbox, (int)(1), DamageClass.Generic);
            }
            else if (Projectile.frame >= 7 && Projectile.frame <= 9)
            {
                Rectangle attackHitbox = GetAttackRectangle(summonData, 2);

                CheckForHostileNPCs(attackHitbox, (int)(1), DamageClass.Generic);
            }

            summonData.CurrentAnimationState = SMSsummon.AnimationState.Attacking;
        }

        private void CheckForHostileNPCs(Rectangle attackHitbox, int damage, DamageClass damageType)
        {
            NPC.HitInfo hit = new NPC.HitInfo
            {
                DamageType = damageType,
                Knockback = 0f,
                Damage = damage,
                Crit = true
            };

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && npc.Hitbox.Intersects(attackHitbox))
                {
                    npc.StrikeNPC(hit);
                }
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
            switch (summonData.CurrentAnimationState)
            {
                case SMSsummon.AnimationState.Idle:
                    texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Idle").Value;
                    break;
                case SMSsummon.AnimationState.Attacking:
                    texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Attack").Value;
                    break;
                default:
                    texture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Run").Value;
                    break;
            }

            int totalFrames = Math.Max(texture.Height / FrameHeight,1);
            Projectile.frameCounter++;

            if (Projectile.frameCounter >= 10)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % totalFrames;
            }

            if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Attacking)
            {
                if (Projectile.frame == 1 || Projectile.frame == 6)
                {
                    isAtking = false;
                }
                else
                {
                    isAtking = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SMSsummon summonData = Projectile.GetGlobalProjectile<SMSsummon>();

            Rectangle frame = new Rectangle(0, Projectile.frame * FrameHeight, FrameWidth, FrameHeight);
            Vector2 offset = summonData.spriteEffects == SpriteEffects.None
                ? new Vector2(240, -115)
                : new Vector2(-240, -115);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + offset;

            Main.EntitySpriteDraw(texture, drawPosition, frame, lightColor, Projectile.rotation,
                new Vector2(FrameWidth / 2, FrameHeight / 2), Projectile.scale, summonData.spriteEffects, 0);

            DrawDebugBox(drawPosition, summonData);

            return false;
        }

        private void DrawDebugBox(Vector2 position, SMSsummon summonData)
        {
            Texture2D boxTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
            boxTexture.SetData(new Color[] { Color.Red });

            int boxWidth = FrameWidth;
            int boxHeight = FrameHeight;

            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X - boxWidth / 2, position.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X - boxWidth / 2, position.Y + boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X - boxWidth / 2, position.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(position.X + boxWidth / 2, position.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
        }

        private Rectangle GetAttackRectangle(SMSsummon summonData, int rectangleType)
        {
            float widthScale = rectangleType == 1 ? 0.75f : 0.75f;
            float heightScale = rectangleType == 1 ? 0.5f : 0.65f;
            float verticalOffset = rectangleType == 1 ? -50f : -87f;
            float horizontalOffsetMultiplier = rectangleType == 1 ? FrameWidth / 3f : FrameWidth / 5f;

            float boxWidth = FrameWidth * widthScale;
            float boxHeight = FrameHeight * heightScale;

            float directionMultiplier = summonData.spriteEffects == SpriteEffects.None ? 1f : -1f;

            Vector2 rectangleOffset = new Vector2(horizontalOffsetMultiplier * directionMultiplier, verticalOffset);
            Vector2 rectanglePosition = Projectile.Center + rectangleOffset;

            return new Rectangle(
                (int)(rectanglePosition.X - boxWidth / 2),
                (int)(rectanglePosition.Y - boxHeight / 2),
                (int)boxWidth,
                (int)boxHeight
            );
        }
    }
}
