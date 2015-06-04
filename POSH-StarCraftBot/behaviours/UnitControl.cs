using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH_sharp.sys;
using POSH_sharp.sys.annotations;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.behaviours
{
    public class UnitControl : AStarCraftBehaviour
    {

        private Dictionary<Position, int> minedPatches;

        public UnitControl(AgentBase agent)
            : base(agent, 
            new string[] { "MorphDrone", "MorphZergling", "MorphOverlord", "MorphHydralisk", "DronesToMineral" },
            new string[] { "DroneCount", "ZerglingCount", "IdleDrones" })
        {
            minedPatches = new Dictionary<Position, int>();
        }

        //
        // INTERNAL
        //

        protected internal Unit GetDrone()
        {
            if (IdleDrones())
                return Interface().GetIdleDrones().ElementAt(0);
            //TODO:  here we could possibly take of the fact that we remove a busy drone from its current task which is not a good thing sometimes
            // this is especially the case if it is the last drone mining
            
            return (Interface().GetDrones(1).Count() > 0) ? Interface().GetDrones(1).ElementAt(0) : null;
        }


        //
        // SENSES
        //

        [ExecutableSense("IdleDrones")]
        public bool IdleDrones()
        {
            return (Interface().GetIdleDrones().Count() > 0) ? true : false;
        }

        [ExecutableSense("DroneCount")]
        public int DroneCount()
        {
            return Interface().DroneCount();
        }

        [ExecutableSense("ZerglingCount")]
        public int ZerglingCount()
        {
            return Interface().ZerglingCount();
        }

        [ExecutableSense("HydraliskCount")]
        public int HydraliskCount()
        {
            return Interface().HydraliskCount();
        }

        //
        // ACTIONS
        //

        [ExecutableAction("MorphDrone")]
        public bool MorphDrone()
        {
            int amount = 3;
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Drone))
                return Interface().
                    GetLarvae(amount).
                    All(larva => larva.morph(bwapi.UnitTypes_Zerg_Drone));

            return false;
        }

        [ExecutableAction("MorphZergling")]
        public bool MorphZergling()
        {
            int amount = 1;
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Zergling))
                return Interface().
                    GetLarvae(amount).
                    All(larva => larva.morph(bwapi.UnitTypes_Zerg_Zergling));

            return false;
        }

        [ExecutableAction("MorphOverlord")]
        public bool MorphOverlord()
        {
            int amount = 1;
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Zergling))
                return Interface().
                    GetLarvae(amount).
                    All(larva => larva.morph(bwapi.UnitTypes_Zerg_Overlord));

            return false;
        }

        [ExecutableAction("MorphHydralisk")]
        public bool MorphHydralisk()
        {
            int amount = 1;
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Hydralisk))
                return Interface().
                    GetLarvae(amount).
                    All(larva => larva.morph(bwapi.UnitTypes_Zerg_Hydralisk));

            return false;
        }

        [ExecutableAction("DronesToMineral")]
        public bool DronesToMineral()
        {
            IEnumerable<Unit> drones = Interface().GetIdleDrones();
            IEnumerable<Unit> mineralPatches = Interface().GetMineralPatches();

            if (drones.Count() < 1)
                return false;

            foreach (Unit drone in drones)
            {
                Position pos = mineralPatches.
                    Where(patch => patch.hasPath(drone)).
                    OrderBy(patch => patch.getDistance(drone)).ElementAt(0).getPosition();
                
                int i = 0;
                while (minedPatches.ContainsKey(pos) && minedPatches[pos] >= 4)
                {
                    //FIXME: this could potentially lead into a null pointer exception because the available patches could be less than the minded ones
                    pos = mineralPatches.
                    Where(patch => patch.hasPath(drone)).
                    OrderBy(patch => patch.getDistance(drone)).ElementAt(++i).getPosition();
                }

                drone.rightClick(pos);
                minedPatches[pos]++;
            }
            

            return true;
        }

        [ExecutableAction("DronesToGas")]
        public bool DronesToGas()
        {
            IEnumerable<Unit> drones = Interface().GetIdleDrones();
            IEnumerable<Unit> extractors = Interface().GetExtractors();

            if (drones.Count() < 1 || extractors.Count() < 1)
                return false;

            foreach (Unit drone in drones)
            {
                Position pos = extractors.
                    Where(patch => patch.hasPath(drone)).
                    OrderBy(patch => patch.getDistance(drone)).ElementAt(0).getPosition();

                int i = 0;
                while (minedPatches.ContainsKey(pos) && minedPatches[pos] >= 4)
                {
                    //FIXME: the could potentially lead into a null pointer exception because the available patches could be less than the minded ones
                    pos = extractors.
                    Where(patch => patch.hasPath(drone)).
                    OrderBy(patch => patch.getDistance(drone)).ElementAt(++i).getPosition();
                }

                drone.rightClick(pos);
                minedPatches[pos]++;
            }


            return true;
        }

        
    }
}
