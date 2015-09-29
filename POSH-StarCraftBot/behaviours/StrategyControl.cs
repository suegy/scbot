using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using System.Threading;
using SWIG.BWAPI;
using SWIG.BWTA;
using POSH_StarCraftBot.logic;

namespace POSH_StarCraftBot.behaviours
{

    public enum Strategy { ThreeHatchHydra = 0, TwoHatchMuta = 1, Zergling = 2 }

    public class StrategyControl : AStarCraftBehaviour
    {
        private Unit scout;
        private Position lureCentroid;
        private Unit droneScout;
        /// <summary>
        /// The scout counter contains the number of bases we already discovered moving from Startlocation towards the most distant ones.
        /// bases are retrieved using bwta.getBaselocations
        /// </summary>
        private int scoutCounter = 1;
        private bool reachedBaseLocation = false;
        private Strategy currentStrategy;
        private bool startStrategy = true;
        private float alarm = 0.0f;
        private GamePhase phase;


        public StrategyControl(AgentBase agent)
            : base(agent, new string[] { }, new string[] { })
        {

        }
        //
        // INTERNAL
        //
        private bool SwitchBuildToBase(int location)
        {
            if (Interface().baseLocations.ContainsKey(location) && Interface().baseLocations[location] is TilePosition)
            {
                Interface().currentBuildSite = (BuildSite)location;
                return true;
            }

            return false;
        }

        //
        // ACTIONS
        //
        [ExecutableAction("SelectNatural")]
        public bool SelectNatural()
        {
            return SwitchBuildToBase((int)BuildSite.Natural);
        }

        [ExecutableAction("SelectStartBase")]
        public bool SelectStartBase()
        {
            return SwitchBuildToBase((int)BuildSite.StartingLocation);
        }

        [ExecutableAction("SelectExtension")]
        public bool SelectExtension()
        {
            return SwitchBuildToBase((int)BuildSite.Extension);
        }

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

        [ExecutableAction("RespondToLure")]
        public bool RespondToLure()
        {
            IEnumerable<Unit> drones = Interface().GetDrones().Where(drone => drone.isUnderAttack());

            if (drones.Count() > 1)
                lureCentroid = CombatControl.CalculateCentroidPosition(drones);
            foreach (Unit drone in drones)
            {
                if (drone.isCarryingMinerals())
                {
                    drone.gather(Interface().GetExtractors().OrderBy(extr => extr.getDistance(drone)).First());
                }
                else if (drone.isCarryingGas())
                {
                    drone.gather(Interface().GetMineralPatches().OrderBy(extr => extr.getDistance(drone)).First());
                }
                else
                {
                    drone.move(new Position(Interface().baseLocations[(int)BuildSite.StartingLocation]));
                }
                alarm += 0.1f;

            }

            return alarm > 0 ? true : false;
        }


        [ExecutableAction("SelectLuredBase")]
        public bool SelectLuredBase()
        {
            int location = Interface().baseLocations.OrderBy(loc => new Position(loc.Value).getApproxDistance(lureCentroid)).First().Key;
            ForceLocations target;
            switch (location)
            {
                case (int)ForceLocations.OwnStart:
                    target = ForceLocations.OwnStart;
                    break;
                case (int)ForceLocations.Natural:
                    target = ForceLocations.Natural;
                    break;
                case (int)ForceLocations.Extension:
                    target = ForceLocations.Extension;
                    break;
                default:
                    target = ForceLocations.Natural;
                    break;

            }
            Interface().currentForcePoint = target;
            return true;


        }

        [ExecutableAction("CounterWithForce")]
        public bool CounterWithForce()
        {
            //TODO: this needs to be implemented
         return false;
        }

        /// <summary>
        /// Find Path to Natural Extension using Overlord 
        /// </summary>
        /// <returns></returns>
        [ExecutableAction("OverLordToNatural")]
        public bool OverLordToNatural()
        {
            if ( Interface().baseLocations.ContainsKey((int)BuildSite.Natural)  || (scout is Unit && scout.getHitPoints() > 0 && scout.isMoving()))
                return false;
            TilePosition startLoc = Interface().baseLocations[(int)BuildSite.StartingLocation];

            if (scout == null || scout.getHitPoints() == 0)
                scout = Interface().GetOverlord().Where(ol => !ol.isMoving()).OrderByDescending(ol => ol.getTilePosition().getDistance(startLoc)).First();
            BaseLocation[] pos = bwta.getBaseLocations().Where(baseLoc => bwta.getGroundDistance(startLoc, baseLoc.getTilePosition()) > 0).OrderBy(baseLoc => bwta.getGroundDistance(startLoc, baseLoc.getTilePosition())).ToArray();

            for (int i = 0; i < pos.Length; i++)
                Console.Out.WriteLine("loc" + i + " " + bwta.getGroundDistance(pos[i].getTilePosition(), startLoc) + " " + pos[i].getTilePosition().getDistance(startLoc));
            if (pos.Length < 1)
                return false; 
            scout.rightClick(pos[0].getPosition());

            return true;
        }

        [ExecutableAction("SelectDroneScout")]
        public bool SelectDroneScout()
        {
            Unit scout = null;
            IEnumerable<Unit> units = Interface().GetDrones().Where(drone =>
                drone.getHitPoints() > 0 &&
                !drone.isMorphing());

            foreach (Unit unit in units)
            {
                if (!unit.isCarryingGas() && !unit.isCarryingMinerals())
                {
                    scout = unit;
                    break;
                }
            }
            if (scout == null && units.Count() > 0)
            {
                scout = units.First();
            }
            droneScout = scout;

            return (droneScout is Unit && droneScout.getHitPoints() > 0) ? true : false;
        }

        [ExecutableAction("DroneScouting")]
        public bool DroneScouting()
        {
            if (droneScout == null || droneScout.getHitPoints() <= 0)
                return false;

            if (scoutCounter == bwta.getBaseLocations().Count() - 1)
                scoutCounter = 0;

            if (droneScout.isUnderAttack())
            {
                if (Interface().baseLocations.ContainsKey((int)BuildSite.Natural))
                    droneScout.move(new Position(Interface().baseLocations[(int)BuildSite.Natural]));
                else
                    droneScout.move(new Position(Interface().baseLocations[(int)BuildSite.StartingLocation]));
                return false;
            }
            if (droneScout.getPosition().getDistance(
                bwta.getBaseLocations().OrderBy(loc =>
                    loc.getTilePosition().getDistance(Interface().baseLocations[(int)BuildSite.StartingLocation]))
                    .ElementAt(scoutCounter)
                    .getPosition()
                    ) < DELTADISTANCE)
            {
                // close to another base location 
                Interface().baseLocations[scoutCounter] = new TilePosition(droneScout.getTargetPosition());
                scoutCounter++;
            }
            else
            {
                droneScout.move(bwta.getBaseLocations().OrderBy(loc =>
                    loc.getTilePosition().getDistance(Interface().baseLocations[(int)BuildSite.StartingLocation]))
                    .ElementAt(scoutCounter).getPosition());
            }

            return true;
        }

        [ExecutableAction("PursueThreeHatchHydra")]
        public bool PursueThreeHatchHydra()
        {
            currentStrategy = Strategy.ThreeHatchHydra;
            return (currentStrategy == Strategy.ThreeHatchHydra) ? true : false;
        }

        [ExecutableAction("SelectChoke")]
        public bool SelectChoke()
        {
            // get the distance between start and natural 
            BuildSite site = Interface().currentBuildSite;
            TilePosition start = Interface().baseLocations[(int)BuildSite.StartingLocation];
            TilePosition targetChoke = null;
            Chokepoint chokepoint = null;

            if (site != BuildSite.StartingLocation && Interface().baseLocations.ContainsKey((int)site))
            {
                targetChoke = Interface().baseLocations[(int)site];
                double distance = start.getDistance(targetChoke);

                // find some kind of measure to determine if the the closest choke to natural is not the once between choke and start but after the natural
                IEnumerable<Chokepoint> chokes = bwta.getChokepoints().Where(ck => bwta.getGroundDistance(new TilePosition(ck.getCenter()), start) > 0).OrderBy(choke => choke.getCenter().getDistance(new Position(targetChoke)));


                foreach (Chokepoint ck in chokes)
                {

                    if (bwta.getGroundDistance(new TilePosition(ck.getCenter()), targetChoke) < bwta.getGroundDistance(new TilePosition(ck.getCenter()), start))
                    {
                        chokepoint = ck;
                        break;
                    }
                }
            }
            else
            {
                targetChoke = start;
                chokepoint = bwta.getChokepoints().Where(ck => bwta.getGroundDistance(new TilePosition(ck.getCenter()), start) > 0).OrderBy(choke => choke.getCenter().getDistance(new Position(start))).First();
            }



            if (chokepoint == null)
                return false;

            //picking the right side of the choke to position forces
            Interface().forcePoints[ForceLocations.NaturalChoke] = (targetChoke.getDistance(new TilePosition(chokepoint.getSides().first)) < targetChoke.getDistance(new TilePosition(chokepoint.getSides().second))) ? new TilePosition(chokepoint.getSides().first) : new TilePosition(chokepoint.getSides().second);
            Interface().currentForcePoint = ForceLocations.NaturalChoke;

            return true;

        }

        //
        // SENSES
        //

        [ExecutableSense("CanCreateUnits")]
        public bool CanCreateUnits()
        {
            if (Interface().GetLarvae().Count() == 0)
                return false;
            switch (currentStrategy){
                case Strategy.ThreeHatchHydra:
                    return (Interface().GetHydraDens().Count() > 0) ? true : false;
                case Strategy.TwoHatchMuta:
                    return (Interface().GetLairs().Count() > 0 && Interface().GetSpire().Count() > 0) ? true : false;
                case Strategy.Zergling:
                    return (Interface().GetHatcheries().Count() > 0 || Interface().GetLairs().Count() > 0 ) ? true : false;
                default:
                    break;
            }
            return false;
        }

        [ExecutableSense("DoneExploring")]
        public bool DoneExploring()
        {
            if (Interface().baseLocations.Count() == bwta.getBaseLocations().Count())
                return true;

            return false;
        }

        [ExecutableSense("NeedOverlordAtNatural")]
        public bool NeedOverlordAtNatural()
        {
            if (scout == null || scout.getHitPoints() == 0 || ( !scout.isMoving() && !reachedBaseLocation) || !Interface().baseLocations.ContainsKey((int)BuildSite.Natural))
                return true;
            
            TilePosition startLoc = Interface().baseLocations[(int)BuildSite.StartingLocation];
            BaseLocation[] pos = bwta.getBaseLocations().Where(
                baseLoc => bwta.getGroundDistance(startLoc, baseLoc.getTilePosition()) > 0 &&
                bwta.getGroundDistance(startLoc, baseLoc.getTilePosition()) > 0 &&
                Interface().baseLocations.Values.Where(location => location.getDistance(baseLoc.getTilePosition()) < DELTADISTANCE).Count() == 0
                )
                .OrderBy(baseLoc => bwta.getGroundDistance(startLoc, baseLoc.getTilePosition())).ToArray();

            if (scout.getDistance(pos[0].getPosition()) < DELTADISTANCE && !reachedBaseLocation)
            {
                Interface().baseLocations[(int)BuildSite.Natural] = pos[0].getTilePosition();
                reachedBaseLocation = true;
            }

            return false;
        }

        [ExecutableSense("DoneExploringLocal")]
        public bool DoneExploringLocal()
        {

            return reachedBaseLocation;
        }

        /// <summary>
        /// Returns the enemy race once it is known. The options are: -1 for unknown, 0 for Zerg, 1 for Protoss, 2 for Human
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("EnemyRace")]
        public int EnemyRace()
        {
            // currently we only expect 1-on-1 matches so there should be only one other player in the game

            int enemyRace = Interface().ActivePlayers.First().Value.getRace().getID();
            if (enemyRace == bwapi.Races_Unknown.getID())
            {
                Interface().enemyRace = Races.Unknown;
                return (int)Races.Unknown;
            }
            if (enemyRace == bwapi.Races_Zerg.getID())
            {
                Interface().enemyRace = Races.Zerg;
                return (int)Races.Zerg;
            }
            if (enemyRace == bwapi.Races_Protoss.getID())
            {
                Interface().enemyRace = Races.Protoss;
                return (int)Races.Protoss;
            }
            if (enemyRace == bwapi.Races_Terran.getID())
            {
                Interface().enemyRace = Races.Terran;
                return (int)Races.Terran;
            }

            return -1;
        }

        [ExecutableSense("GameRunning")]
        public bool GameRunning()
        {
            return Interface().GameRunning();
        }

        /// <summary>
        /// Used to switch between different implemented strategies. If one is detected or changes are slim of using it Strategy is switched.
        /// "0" refers to 3HachHydra, "1" is not defined yet
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("FollowStrategy")]
        public int FollowStrategy()
        {
            //TODO: implement proper logic for dete cting when to switch between different strats.
            if (startStrategy)
            {
                currentStrategy = Strategy.ThreeHatchHydra;
                startStrategy = false;
            }
            return (int)currentStrategy;
        }

        [ExecutableSense("BuildArmy")]
        public bool BuildArmy()
        {

            return false;
        }

        [ExecutableSense("DroneExtractorRatio")]
        public float DroneExtractorRatio()
        {
            return Interface().GetDrones().Where(drone => drone.isGatheringMinerals()).Count() / Interface().GetDrones().Where(drone => drone.isGatheringGas()).Count();
        }

        [ExecutableSense("DronesLured")]
        public bool DronesLured()
        {
            return Interface().GetDrones().Where(drone => drone.isUnderAttack()).Count() > 0;
        }

        [ExecutableSense("Alarm")]
        public float Alarm()
        {
            alarm = (alarm < 0.0f) ? 0.0f : alarm - 0.05f;

            return alarm;
        }

        [ExecutableSense("DroneScoutAvailable")]
        public bool DroneScoutAvailable()
        {
            return (droneScout is Unit && droneScout.getHitPoints() > 0) ? true : false;
        }


    }
}
