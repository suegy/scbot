using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH_sharp.sys;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.behaviours
{
    /// <summary>
    /// abstract class which is intended for inheritance of POSH Behaviour
    /// </summary>
    public abstract class AStarCraftBehaviour : Behaviour
    {

        protected static BWAPI.IStarcraftBot IBWAPI;

        public AStarCraftBehaviour(AgentBase agent)
            : this(agent, new string[] { }, new string[] { })
        {}
        public AStarCraftBehaviour(AgentBase agent, Dictionary<string, object> attributes)
            : base(agent, new string[] { }, new string[] { })
        {
            this.attributes = attributes;
        }

        public AStarCraftBehaviour(AgentBase agent, string[] actions, string [] senses)
            : base(agent, actions, senses)
        { }

        protected BODStarCraftBot Interface()
        {
            return ((BODStarCraftBot)IBWAPI);
        }

        protected UnitControl UnitManager()
        {
            return (UnitControl)agent.getBehaviour("UnitControl");
        }

        protected bool CanMorphUnit(UnitType unit)
        {
            if (unit.gasPrice() <= Interface().GasCount() &&
                unit.mineralPrice() <= Interface().MineralCount() &&
                unit.supplyRequired() <= Interface().AvailableSupply() &&
                Interface().LarvaeCount() > 0)
                return true;

            return false;
        }

    }
}
