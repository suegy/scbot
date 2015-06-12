using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using System.Threading;

namespace POSH_StarCraftBot.behaviours
{
    class StrategyControl : AStarCraftBehaviour
    {
        public StrategyControl(AgentBase agent)
            : base(agent, new string[] {}, new string[] {})
        {

        }
        //
        // INTERNAL
        //

        //
        // ACTIONS
        //
        [ExecutableAction("Idle")]
        public bool Idle()
        {
            try
            {
                Thread.Sleep(50);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //
        // SENSES
        //

        [ExecutableSense("GameRunning")]
        public bool GameRunning()
        {
            return Interface().GameRunning();
        }

        [ExecutableSense("TotalSupply")]
        public int SupplyCount()
        {
            return Interface().TotalSupply();
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

        [ExecutableSense("BuildArmy")]
        public bool BuildArmy()
        {
            return false;
        }
    }
}
