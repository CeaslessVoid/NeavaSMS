using Microsoft.Xna.Framework.Graphics;
using NeavaSMS.Common.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace NeavaSMS.Common.Global
{
    public class SMSsummon : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // Animation
        public enum AnimationState
        {
            Idle,
            Running,
            Attacking,
            Ultimate
        }

        public AnimationState CurrentAnimationState = AnimationState.Idle;

        public SpriteEffects spriteEffects;

        // SKill
        public ActiveSkill activeSkill;

    }
}
