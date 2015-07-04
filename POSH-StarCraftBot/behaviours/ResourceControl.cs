using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using System.Threading;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.behaviours
{
    class ResourceControl : AStarCraftBehaviour
    {
        public ResourceControl(AgentBase agent)
            : base(agent, new string[] {}, new string[] {})
        {

        }
        //
        // INTERNAL
        //

        //
        // ACTIONS
        //
        [ExecutableAction("HydraSpeedUpgrade")]
        public bool HydraSpeedUpgrade()
        {
            return Interface().GetHydraDens().Where(den => !den.isUpgrading() && den.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Muscular_Augments);
        }

        [ExecutableAction("HydraRangeUpgrade")]
        public bool HydraRangeUpgrade()
        {
            return Interface().GetHydraDens().Where(den => !den.isUpgrading() && den.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Grooved_Spines);
        }

        //
        // SENSES
        //

        [ExecutableSense("TotalSupply")]
        public int TotalSupply()
        {
            return Interface().TotalSupply();
        }

        [ExecutableSense("Supply")]
        public int SupplyCount()
        {
            return Interface().SupplyCount();
        }

        [ExecutableSense("AvailableSupply")]
        public int AvailableSupply()
        {
            return Interface().AvailableSupply();
        }

        [ExecutableSense("Gas")]
        public int Gas()
        {
            return Interface().GasCount();
        }

        [ExecutableSense("Minerals")]
        public int Minerals()
        {
            return Interface().MineralCount();
        }

        [ExecutableSense("NaturalFound")]
        public bool NaturalFound()
        {
            return (Interface().baseLocations[(int)BuildSite.Natural] is TilePosition);
        }

        [ExecutableSense("HaveHydraSpeed")]
        public bool HaveHydraSpeed()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Muscular_Augments) > 0);
        }

        [ExecutableSense("HaveHydraRange")]
        public bool HaveHydraRange()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Grooved_Spines) > 0);
        }

        
    }
}
