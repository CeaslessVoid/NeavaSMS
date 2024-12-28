using NeavaSMS.Common.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace NeavaSMS.Common.Global
{
    public class SMSItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        // Base Stats
        public ModProjectile character;

        // Levels and Trancendance
        public int currentLevel;

        public int currentTrancendance;
        
        public int maxLevel;

        public int maxLevelPerTransendance;

        // SKills

        public List<PassiveSkill> passiveSkills;



    }
}
