using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.behaviours
{
    /// <summary>
    /// abstract class which is intended for inheritance of POSH Behaviour
    /// </summary>
    public abstract class AStarCraftBehaviour : Behaviour
    {

        protected static BWAPI.IStarcraftBot IBWAPI;

        public enum Races { Unknown = 0, Zerg = 1, Protoss = 2, Terran = 3 }

        protected const double DELTADISTANCE = 100L;

        protected const double DELTATIME = 100L;

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

        protected internal bool move(Position target, Unit unit, int timeout = 10)
        {
            bool executed = false;
            if (unit.getDistance(target) < DELTADISTANCE)
                return false;
            while (!unit.getTargetPosition().opEquals(target) && timeout-- > 0)
            {
                executed = unit.move(target, false);
                if (_debug_)
                    Console.Out.WriteLine("unit "+unit.getID()+" to target: " + executed);
                System.Threading.Thread.Sleep(50);
            }
            return executed;
        }

        protected UnitControl UnitManager()
        {
            return (UnitControl)agent.getBehaviour("UnitControl");
        }

        protected bool CanMorphUnit(UnitType unit)
        {
            if (unit.gasPrice() <= Interface().GasCount() &&
                unit.mineralPrice() <= Interface().MineralCount() &&
                unit.supplyRequired() <= Interface().AvailableSupply()
                )
                if (unit.isBuilding())
                    return true;
                else if (Interface().LarvaeCount() > 0 )
                    return true;

            return false;
        }

    }
}
