using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NeavaSMS.Common.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NeavaSMS.Sprites.Summons.Ariel
{
    public class Ariel : ModProjectile
    {

        // Constnats


        //Sprites
        private const int   FrameWidth = 766;
        private const int   FrameHeight = 506;

        private readonly Dictionary<string, (float widthScale, float heightScale, float verticalOffset, float horizontalOffsetMultiplier)> rectangleSettings =
            new Dictionary<string, (float, float, float, float)>
            {
                { "attack1", (0.75f, 0.5f, -50f, FrameWidth / 3f) },
                { "attack2", (0.75f, 0.65f, -87f, FrameWidth / 5f) },
                { "ultimate", (2f, 5f, 0f, FrameWidth / 2f) }
            };

        //Physics
        private const float GravityForce = 0.4f;
        private const float MaxFallSpeed = 60f;
        private const float JumpForce = -20f;
        private const float MovementSpeed = 6f;
        private const int   GroundedBufferTime = 10;


        // AI
        private const float StartWalkingDistance = 100f;
        private const float StopWalkingDistance = 30f;
        private const int   TargetUpdateInterval = 120;
        private const int   TeleportDistanceThreshold = 900;

        private const float AttackRangeWidth = 700f;
        private const float AttackRangeHeight = 500f;

        private const int   UltimateCooldownMax = 1400;

        // Non Constants

        //AI
        private bool    attackHitRegistered = false;
        private bool    isAtking = false;
        private bool    isOnGround = false;
        private NPC     closestNPC;
        private int     groundedBuffer;
        private int     ultimateCooldown = 0;

        //Sprites
        private SMSsummon.AnimationState? previousAnimationState;
        private Texture2D texture;

        private static Texture2D idleTexture;
        private static Texture2D attackTexture;
        private static Texture2D runTexture;
        private static Texture2D ultTexture;

        private bool framelock = false;

        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.knockBack = 1f;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.damage = 20;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            SMSsummon summonData = Projectile.GetGlobalProjectile<SMSsummon>();

            Projectile.timeLeft = 2;
            
            Lighting.AddLight(Projectile.Center, 0.1f, 0.5f, 1.0f);

            HandleGravity(summonData);
            HandleMovement(player, summonData);
            AnimateProjectile(summonData);
            HandleTeleportation(player);
        }

        private void HandleGravity(SMSsummon summonData)
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Bottom.Y / 16f);

            Tile tileBelow = Framing.GetTileSafely(tileX, tileY + 1);
            Tile tileBelow2 = Framing.GetTileSafely(tileX + 1, tileY + 1);

            isOnGround = (tileBelow.HasTile && Main.tileSolid[tileBelow.TileType] && !Main.tileSolidTop[tileBelow.TileType]) || 
                (tileBelow2.HasTile && Main.tileSolid[tileBelow2.TileType] && !Main.tileSolidTop[tileBelow2.TileType]);

            groundedBuffer = (int)Projectile.localAI[1];

            if (isOnGround)
            {
                groundedBuffer = GroundedBufferTime;
            }
            else if (groundedBuffer >= -30)
            {
                groundedBuffer--;
            }

            isOnGround = groundedBuffer > 0;
            Projectile.localAI[1] = groundedBuffer;

            if (!isOnGround)
            {
                if (groundedBuffer < 0)
                {
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + (GravityForce * (int)(groundedBuffer * -0.08 + 1)), MaxFallSpeed);
                }
                else
                {
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + GravityForce, MaxFallSpeed);
                }

                if (summonData.CurrentAnimationState != SMSsummon.AnimationState.Attacking && summonData.CurrentAnimationState != SMSsummon.AnimationState.Ultimate)
                {
                    Projectile.frame = 1;
                    framelock = true;
                }
            }
            else
            {
                Projectile.velocity.Y = 0;
                framelock = false;
            }
        }


        private void HandleTeleportation(Player player)
        {
            float distanceToPlayer = Vector2.Distance(Projectile.Center, player.Center);
            if (distanceToPlayer > TeleportDistanceThreshold && !isAtking)
            {
                groundedBuffer = GroundedBufferTime * 5;
                Projectile.velocity.Y = 0;

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


            Rectangle attackHitbox = summonData.CurrentAnimationState == SMSsummon.AnimationState.Attacking && Projectile.frame >= 6 ? GetRectangle(summonData, "attack2") : GetRectangle(summonData, "attack1");

            bool target = false;
            CheckForHostileNPCs(attackHitbox, 0, DamageClass.Generic, true, out target);

            if (!isAtking)
                summonData.spriteEffects = moveTo.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (isAtking || target)
            {
                PerformAttack(summonData);
            }
            else if (moveTo.Length() > StartWalkingDistance)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Running;
                moveTo.Normalize();
                moveTo *= MovementSpeed;
                Projectile.velocity = (Projectile.velocity * 20f + moveTo) / 21f;

                int obstacleHeight = 1;
                if (IsObstacleInPath(out obstacleHeight) && groundedBuffer >= -30)
                {
                    float jumpStrength = MathHelper.Clamp(obstacleHeight * 2f, 8, 40f);
                    if (Projectile.velocity.Y == 0)
                    {
                        Projectile.velocity.Y -= (jumpStrength);
                    }
                }
            }
            else if (moveTo.Length() < StopWalkingDistance)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Idle;
                Projectile.velocity.X *= 0f;
            }
        }

        private Vector2 FindTarget(Player player)
        {
            Vector2 target = new Vector2();

            closestNPC = FindTargetNPC(player);

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
            bool temp;

            Projectile.velocity.X = 0f;

            framelock = false;

            if (!isAtking && (summonData.CurrentAnimationState != SMSsummon.AnimationState.Attacking && summonData.CurrentAnimationState != SMSsummon.AnimationState.Ultimate))
            {
                Projectile.frame = 1;
                attackHitRegistered = false;
            }

            if (ultimateCooldown == 0)
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Ultimate;

                if (Projectile.frame == 14)
                {
                    Vector2 explosionOffset = summonData.spriteEffects == SpriteEffects.None ? new Vector2(580, -200) : new Vector2(-240, -200); ;
                    Vector2 explosionPosition = Projectile.Center + explosionOffset;
                    SoundEngine.PlaySound(SoundID.Item74, explosionPosition);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        explosionPosition,
                        Vector2.Zero,
                        ModContent.ProjectileType<Ariel_Ultimate>(),
                        0,
                        0,
                        Projectile.owner
                    );

                    Rectangle attackHitbox = GetRectangle(summonData, "ultimate");

                    if (!attackHitRegistered)
                    {
                        CheckForHostileNPCs(attackHitbox, (int)(1000), DamageClass.Generic, false, out temp);
                        attackHitRegistered = true;
                    }
                }
                else
                {
                    attackHitRegistered = false;
                }

            }
            else
            {
                summonData.CurrentAnimationState = SMSsummon.AnimationState.Attacking;

                if (Projectile.frame >= 2 && Projectile.frame <= 4)
                {
                    Rectangle attackHitbox = GetRectangle(summonData, "attack1");
                    SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);

                    if (!attackHitRegistered)
                    {
                        CheckForHostileNPCs(attackHitbox, (int)(50), DamageClass.Generic, false, out temp);
                        attackHitRegistered = true;
                    }
                }
                else if (Projectile.frame >= 7 && Projectile.frame <= 9)
                {
                    Rectangle attackHitbox = GetRectangle(summonData, "attack2");
                    SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);

                    if (!attackHitRegistered)
                    {
                        CheckForHostileNPCs(attackHitbox, (int)(100), DamageClass.Generic, false, out temp);
                        attackHitRegistered = true;
                    }
                }
                else
                {
                    attackHitRegistered = false;
                }
            }
            
        }

        private void CheckForHostileNPCs(Rectangle attackHitbox, int damage, DamageClass damageType, bool check, out bool checking)
        {
            checking = false;

            NPC.HitInfo hit = new NPC.HitInfo
            {
                DamageType = damageType,
                Knockback = 2f,
                Damage = damage,
                Crit = true
            };

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && npc.Hitbox.Intersects(attackHitbox))
                {
                    checking = true;

                    if (!check)
                        npc.StrikeNPC(hit);
                }
            }
        }
        private bool IsObstacleInPath(out int obstacleHeight)
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Bottom.Y / 16f);
            int direction = Math.Sign(Projectile.velocity.X);

            obstacleHeight = 0;

            for (int offset = 1; offset <= 8; offset++)
            {
                int baseTileX = tileX + (direction * offset);

                for (int i = 0; i < 3; i++)
                {
                    Tile tileInPath = Framing.GetTileSafely(baseTileX, tileY - i);

                    if (Main.netMode != NetmodeID.Server)
                    {
                        Vector2 tileWorldPosition = new Vector2(baseTileX * 16, (tileY - i) * 16);
                        Dust.NewDustPerfect(tileWorldPosition, DustID.Electric, Vector2.Zero, 0, Color.Red, 1f).noGravity = true;
                    }

                    if (tileInPath.HasTile && Main.tileSolid[tileInPath.TileType])
                    {
                        int currentObstacleHeight = 1;

                        for (int heightCheck = 1; heightCheck < 10; heightCheck++)
                        {
                            Tile tileAbove = Framing.GetTileSafely(baseTileX, tileY - i - heightCheck);
                            if (tileAbove.HasTile && Main.tileSolid[tileAbove.TileType])
                            {
                                currentObstacleHeight++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        int baseTileXBehind = baseTileX - direction;
                        for (int heightCheck = 0; heightCheck < 10; heightCheck++)
                        {
                            Tile tileBehind = Framing.GetTileSafely(baseTileXBehind, tileY - i - heightCheck);
                            if (tileBehind.HasTile && Main.tileSolid[tileBehind.TileType])
                            {
                                currentObstacleHeight = Math.Max(currentObstacleHeight, heightCheck + 1);
                            }
                            else
                            {
                                break;
                            }
                        }

                        obstacleHeight = Math.Max(obstacleHeight, currentObstacleHeight);
                        return true;
                    }
                }
            }

            return false;
        }


        private void AnimateProjectile(SMSsummon summonData)
        {

            if (idleTexture != null)
            {
                Load();
            }

            if (summonData.CurrentAnimationState != previousAnimationState)
            {
                Projectile.frame = 1;
                Projectile.frameCounter = 0;
                previousAnimationState = summonData.CurrentAnimationState;
            }

            switch (summonData.CurrentAnimationState)
            {
                case SMSsummon.AnimationState.Idle:
                    texture = idleTexture;
                    break;
                case SMSsummon.AnimationState.Attacking:
                    texture = attackTexture;
                    break;
                case SMSsummon.AnimationState.Running:
                    texture = runTexture;
                    break;
                case SMSsummon.AnimationState.Ultimate:
                    texture = ultTexture;
                    break;
            }

            int totalFrames = Math.Max(texture.Height / FrameHeight,1);
            Projectile.frameCounter++;

            if (ultimateCooldown > 0)
                ultimateCooldown--;

            if (Projectile.frameCounter >= frameCooldown(summonData) && !framelock)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % totalFrames;
            }

            if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Attacking)
            {
                if (Projectile.frame == 5 || Projectile.frame == 6 || Projectile.frame == 11 || Projectile.frame == 12)
                {
                    isAtking = false;
                }
                else
                {
                    isAtking = true;
                    groundedBuffer = GroundedBufferTime * 5;
                    Projectile.velocity.Y = 0;
                }
            }
            else if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Ultimate)
            {
                if (Projectile.frame == 20 || Projectile.frame == 21)
                {
                    ultimateCooldown = UltimateCooldownMax;
                    isAtking = false;
                }
                else
                {
                    isAtking = true;
                    groundedBuffer = GroundedBufferTime * 5;
                    Projectile.velocity.Y = 0;
                }
            }
        }

        public int frameCooldown(SMSsummon summonData)
        {

            if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Idle)
                return 9;
            else if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Running)
                return 8;
            else if (summonData.CurrentAnimationState == SMSsummon.AnimationState.Attacking)
                return 6;

            return 6;
        }

        public override void Load()
        {
            idleTexture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Idle").Value;
            attackTexture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Attack").Value;
            runTexture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Run").Value;
            ultTexture = ModContent.Request<Texture2D>("NeavaSMS/Sprites/Summons/Ariel/Ariel_Ult").Value;
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

            DrawATKRectangle(lightColor, summonData);
            DrawATKRectangle2(lightColor, summonData);

            return false;
        }
        private Rectangle GetRectangle(SMSsummon summonData, string mode)
        {
            rectangleSettings.TryGetValue(mode, out var settings);

            float directionMultiplier = summonData.spriteEffects == SpriteEffects.None ? 1f : -1f;

            float boxWidth = FrameWidth * settings.widthScale;
            float boxHeight = FrameHeight * settings.heightScale;

            Vector2 rectangleOffset = new Vector2(settings.horizontalOffsetMultiplier * directionMultiplier, settings.verticalOffset);
            Vector2 rectanglePosition = Projectile.Center + rectangleOffset;

            return new Rectangle(
                (int)(rectanglePosition.X - boxWidth / 2),
                (int)(rectanglePosition.Y - boxHeight / 2),
                (int)boxWidth,
                (int)boxHeight
            );
        }

        private void DrawATKRectangle(Color lightColor, SMSsummon summonData)
        {
            Texture2D boxTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
            boxTexture.SetData(new Color[] { Color.Red });

            float widthScale = 0.75f;
            float heightScale = 0.5f;

            float boxWidth = FrameWidth * widthScale;
            float boxHeight = FrameHeight * heightScale;

            float directionMultiplier = summonData.spriteEffects == SpriteEffects.None ? 1f : -1f;
            Vector2 rectangleOffset = new Vector2(FrameWidth / 3f * directionMultiplier, -50f);

            Vector2 rectanglePosition = Projectile.Center + rectangleOffset - Main.screenPosition;

            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X - boxWidth / 2, rectanglePosition.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X - boxWidth / 2, rectanglePosition.Y + boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X - boxWidth / 2, rectanglePosition.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X + boxWidth / 2, rectanglePosition.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
        }

        private void DrawATKRectangle2(Color lightColor, SMSsummon summonData)
        {
            Texture2D boxTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
            boxTexture.SetData(new Color[] { Color.Red });

            float widthScale = 0.75f;
            float heightScale = 0.65f;

            float boxWidth = FrameWidth * widthScale;
            float boxHeight = FrameHeight * heightScale;

            float directionMultiplier = summonData.spriteEffects == SpriteEffects.None ? 1f : -1f;
            Vector2 rectangleOffset = new Vector2(FrameWidth / 5f * directionMultiplier, -87f);

            Vector2 rectanglePosition = Projectile.Center + rectangleOffset - Main.screenPosition;

            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X - boxWidth / 2, rectanglePosition.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X - boxWidth / 2, rectanglePosition.Y + boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(boxWidth, 1), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X - boxWidth / 2, rectanglePosition.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(boxTexture, new Vector2(rectanglePosition.X + boxWidth / 2, rectanglePosition.Y - boxHeight / 2),
                null, Color.Red * 0.5f, 0f, Vector2.Zero, new Vector2(1, boxHeight), SpriteEffects.None, 0);
        }
    }

    public class Ariel_Ultimate : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 766;
            Projectile.height = 506;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            if (++Projectile.frameCounter >= 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.Kill();
                    for (int i = 0; i < 20; i++)
                    {
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Rain, Scale: 1.5f);
                    }
                }
            }

            Lighting.AddLight(Projectile.Center, 0.1f, 0.5f, 1.0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float scale = 1.5f;
            Main.EntitySpriteDraw(
                TextureAssets.Projectile[Projectile.type].Value,
                Projectile.Center - Main.screenPosition,
                new Rectangle(0, Projectile.frame * Projectile.height, Projectile.width, Projectile.height),
                lightColor,
                Projectile.rotation,
                new Vector2(Projectile.width / 2, Projectile.height / 2),
                scale,
                SpriteEffects.None,
                0
            );
            return false;
        }
    }
}
