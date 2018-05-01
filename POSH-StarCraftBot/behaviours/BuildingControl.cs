using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using SWIG.BWAPI;
using SWIG.BWTA;
using POSHStarCraftBot.logic;

namespace POSHStarCraftBot.behaviours
{
    public class BuildingControl : AStarCraftBehaviour
    {

        TilePosition buildLocation;
        Dictionary<int, Unit> destroyedBuildings;
        Unit buildingToRepair;
        Unit repairDrone;
        Unit builder;

        private bool needBuilding = true;

        /// <summary>
        /// contains the current location and build queue 
        /// </summary>
        Dictionary<int, List<TilePosition>> buildQueue;

        /// <summary>
        /// contains the buildings which are currently built and still in progress. Once a building is complete it 
        /// gets removed from both dictionaries buildQueue and currentlyBuilt.
        /// </summary>
        Dictionary<Unit, TilePosition> buildingInProgress;

        public BuildingControl(AgentBase agent)
            : base(agent,
            new string[] { },
            new string[] { })
        {
            buildQueue = new Dictionary<int, List<TilePosition>>();
            buildingInProgress = new Dictionary<Unit, TilePosition>();
            destroyedBuildings = new Dictionary<int, Unit>();
        }


        //
        // INTERNAL
        //
        private TilePosition GetBaseLocation()
        {
            TilePosition baseLoc = Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite) ? Interface().baseLocations[(int)Interface().currentBuildSite] : null;
            if (!(baseLoc is TilePosition))
                baseLoc = Interface().baseLocations[(int)BuildSite.StartingLocation];

            return baseLoc;
        }

        private TilePosition addToTile(TilePosition pos, int x, int y)
        {
            TilePosition output = new TilePosition(x+pos.xConst(), y+pos.yConst());
            
            return output;
        }

        private TilePosition 
            PossibleBuildLocation(TilePosition start, int xSpace, int ySpace, int iterations, Unit builder, UnitType building)
        {
            int x =   0;
            int y =   0;
            int dx =  0;
            int dy = -1;
            int t = (int) Math.Sqrt(iterations);
            for (int i = 0; i < iterations;i++){
                if ((-Math.Sqrt(iterations) / 2 < x && x <= Math.Sqrt(iterations) / 2) && (-Math.Sqrt(iterations) / 2 < y && y <= Math.Sqrt(iterations) / 2))
                {
                    if (bwapi.Broodwar.canBuildHere(builder, addToTile(start,x,y), building))
                    {
                        Console.Out.WriteLine("building "+building.getName()+ " at:"+addToTile(start,x,y).xConst()+":"+addToTile(start,x,y).yConst());
                        return addToTile(start,x,y);
                    }
                }
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1-y))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }
                x += dx;
                y += dy;
            }     
            return null;
        }

        protected int CountUnbuiltBuildings(UnitType type)
        {
            int count = 0;
            if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is List<TilePosition>))
                return count;
            
            for (int i = buildQueue[type.getID()].Count()-1; i > 0; i--)
            {
                TilePosition pos = buildQueue[type.getID()].ElementAt(i);
                IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(unit => unit.getType().getID() == type.getID() && unit.getTilePosition().getDistance(pos) < 10);
                if (buildings.Count() < 1)
                {
                    buildQueue[type.getID()].RemoveAt(i);
                }
                else
                {
                    for (int j = buildings.Count()-1; j > 0; j-- )
                    {
                        Unit building = buildings.ElementAt(j);
                        if (building.isBeingConstructed())
                        {
                            buildingInProgress[building] = building.getTilePosition();
                            count++;
                        }
                        else if (building.getHitPoints() == 0 || building.isCompleted())
                        {
                            buildQueue[type.getID()].RemoveAt(j);
                        }
                    }
                } 
            }

            return count;
        }

        protected int CountBuildingsinProgress(UnitType type)
        {
            for (int i = buildingInProgress.Keys.Count() - 1; i > 0; i--)
                {
                    Unit unit = buildingInProgress.Keys.ElementAt(i);
                    if (unit.getHitPoints() == 0 || unit.isCompleted())
                    {
                        buildingInProgress.Remove(unit);
                    }

                }

            return buildingInProgress.Where(pair => pair.Key.getType().getID() == type.getID()).Count();
        }

        protected bool Build(UnitType type, int timeout = 10)
        {
            bool building = false;
            if (buildLocation is TilePosition && CanMorphUnit(type) && builder is Unit && !builder.isConstructing())
            {
                if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is List<TilePosition>))
                    buildQueue[type.getID()] = new List<TilePosition>();

                if (buildQueue[type.getID()].Contains(buildLocation) && builder.getTilePosition().getDistance(buildLocation) < 10 && builder.isConstructing())
                    return true;
                
                if (!builder.isConstructing() && builder.getHitPoints() > 0 )
                {
                    building = builder.build(buildLocation, type);
                }
                if (building && !buildQueue[type.getID()].Contains(buildLocation))
                    buildQueue[type.getID()].Add(buildLocation);


                return building;

            }
            return false;
        }

        /**
         * Based on Code from James for his Protoss Bot
        */
        protected bool Position(UnitType type, int X, int Y, int Z)
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite) && Interface().currentBuildSite != BuildSite.NaturalChoke)
                return false;

            TilePosition buildPosition;
            if (Interface().currentBuildSite == BuildSite.NaturalChoke)
                buildPosition = Interface().chokeBuild;
            else
                buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);

            double dist = Z;
            if (buildLocation is TilePosition && buildPosition is TilePosition)
                dist = buildLocation.getDistance(buildPosition);
            if (buildLocation != null && dist < Z && bwapi.Broodwar.canBuildHere(builder, buildLocation, type)) { }
            else
            {
                Position pos = new Position(buildPosition);
                Console.Out.WriteLine("Base:" + Interface().currentBuildSite + " loc:" + buildPosition.xConst() + ":" + buildPosition.yConst() + " pos" + pos.xConst() + ":" + pos.yConst());
                buildPosition = PossibleBuildLocation(buildPosition, X, Y, Z, builder, type);
                buildLocation = buildPosition;
            }
            if (buildLocation is TilePosition)
            {
                move(new Position(buildLocation), builder);
                if (builder.getDistance(new Position(buildPosition)) < DELTADISTANCE)
                    return true;
            }
            return false;
        }


        //
        // ACTIONS
        //
        [ExecutableAction("SelectExtractorLocation")]
        public bool SelectExtractorLocation()
        {
            // enough resources available?
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Extractor) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            // are there any geysers available/visible?
            IEnumerable<Unit> geysers = Interface()
                .GetGeysers().Where(geyser => geyser.getResources() > 0);
            if (geysers.Count() < 1)
                return false;


            // sort by closest path for ground units from selected build base
            TilePosition closest = geysers
                .OrderBy(geyser => geyser.getDistance(new Position(buildPosition)))
                .First().getTilePosition();
            
            // if there is a close geyers we are done
            if (closest is TilePosition)
            {
                this.buildLocation = closest;
                builder = Interface().GetBuilder(buildPosition);
                //move(new Position(closest), builder);
                // if (builder.getDistance(new Position(closest)) < DELTADISTANCE)
                //     return true;
                return true;
            }

            return false;
        }

        [ExecutableAction("BuildExtractor")]
        public bool BuildExtractor()
        {
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Extractor) && buildLocation is TilePosition)
            {

                return Build(bwapi.UnitTypes_Zerg_Extractor);
            }
            return false;
        }

        [ExecutableAction("SelectDefenceBase")]
        public bool SelectDefenceBase()
        {
            if (Interface().GetHatcheries().Count() < 1)
                return false;
            double distanceStart = Interface().baseLocations[(int)ForceLocations.OwnStart].getDistance(buildingToRepair.getTilePosition());
            double distanceNatural = Interface().baseLocations[(int)ForceLocations.Natural].getDistance(buildingToRepair.getTilePosition());

            ForceLocations defenceBase = (distanceStart < distanceNatural) ? ForceLocations.OwnStart : ForceLocations.Natural;
            Interface().currentBuildSite = (BuildSite)defenceBase;
            return true;
        }


        /// <summary>
        /// Select suitable location for the spawning pool
        /// </summary>
        /// <returns></returns>
        [ExecutableAction("PositionSpwnPl")]
        public bool PositionSpwnPl()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition); 
            
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 100, builder, bwapi.UnitTypes_Zerg_Spawning_Pool);
            buildLocation = buildPosition;

            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);
                return true;
            }
            return false;
        }

        [ExecutableAction("BuildSpwnPl")]
        public bool BuildSpwnPl()
        {
            return Build(bwapi.UnitTypes_Zerg_Spawning_Pool);
        }

        /// <summary>
        /// Select suitable location for the spawning pool
        /// </summary>
        /// <returns></returns>
        [ExecutableAction("PositionHydraDen")]
        public bool PositionHydraDen()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);
            
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 200, builder, bwapi.UnitTypes_Zerg_Hydralisk_Den);
            buildLocation = buildPosition;
            
            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);
                return true;
            }

            return false;
        }

        [ExecutableAction("BuildHydraDen")]
        public bool BuildHydraDen()
        {
            return Build(bwapi.UnitTypes_Zerg_Hydralisk_Den);
        }


        [ExecutableAction("PositionHatchery")]
        public bool PositionHatchery()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 200, builder, bwapi.UnitTypes_Zerg_Hatchery);
            
            buildLocation = buildPosition;
            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);

                if (builder.getDistance(new Position(buildPosition)) < DELTADISTANCE )
                    return true;
            }
            return false;
        }

        [ExecutableAction("BuildHatchery")]
        public bool BuildHatchery()
        {
            return Build(bwapi.UnitTypes_Zerg_Hatchery);
        }

        [ExecutableAction("UpgradeHatchery")]
        public bool UpgradeHatchery()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Lair) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            return Interface().GetHatcheries()
                .OrderBy(hatch => Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(hatch.getTilePosition()))
                .First().morph(bwapi.UnitTypes_Zerg_Lair);
        }

        [ExecutableAction("PositionCreepColony")]
        public bool PositionCreepColony()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 200, builder, bwapi.UnitTypes_Zerg_Creep_Colony);
            
            buildLocation = buildPosition;

            move(new Position(buildPosition), builder);
            if (builder.getDistance(new Position(buildPosition)) < DELTADISTANCE)
                return true;

            return false;
        }

        [ExecutableAction("BuildCreepColony")]
        public bool BuildCreepColony()
        {
            return Build(bwapi.UnitTypes_Zerg_Creep_Colony);
        }

        [ExecutableAction("UpgradeSunkenColony")]
        public bool UpgradeCreepColony()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Sunken_Colony) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            double dist = Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(Interface().baseLocations[(int)BuildSite.StartingLocation]) / 3;

            return Interface().GetCreepColonies().Where(col => col.getDistance(new Position(Interface().baseLocations[(int)Interface().currentBuildSite])) < dist)
                .OrderByDescending(hatch => Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(hatch.getTilePosition()))
                .ElementAt(0).morph(bwapi.UnitTypes_Zerg_Sunken_Colony);
        }

        [ExecutableAction("UpgradeSporeColony")]
        public bool UpgradeSporeColony()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Spore_Colony) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            double dist = Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(Interface().baseLocations[(int)BuildSite.StartingLocation]) / 3;

            return Interface().GetCreepColonies().Where(col => col.getDistance(new Position(Interface().baseLocations[(int)Interface().currentBuildSite])) < dist)
                .OrderByDescending(hatch => Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(hatch.getTilePosition()))
                .ElementAt(0).morph(bwapi.UnitTypes_Zerg_Spore_Colony);
        }

        [ExecutableAction("RepairBuilding")]
        public bool RepairBuilding()
        {
            if (repairDrone == null || repairDrone.getHitPoints() <= 0 || buildingToRepair == null || buildingToRepair.getHitPoints() <= 0)
                return false;
            move(buildingToRepair.getPosition(), repairDrone);
            return repairDrone.repair(buildingToRepair, true);
        }


        [ExecutableAction("FinishedThreeHatchBuilding")]
        public bool FinishedThreeHatchBuilding()
        {
            needBuilding = false;

            return needBuilding;
        }

        //
        // SENSES
        //
        [ExecutableSense("NeedBuilding")]
        public bool NeedBuilding()
        {
            return needBuilding;
        }

        [ExecutableSense("HatcheryCount")]
        public int HatcheryCount()
        {
            return Interface().GetHatcheries().Count() + Interface().GetLairs().Count() +
                CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Hatchery) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Hatchery) +
                CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Lair) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Lair);
        }
        
        [ExecutableSense("SpawnPoolCount")]
        public int SpawnPoolCount()
        {
            return Interface().GetSpawningPools().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Spawning_Pool) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Spawning_Pool);
        }
        [ExecutableSense("ExtractorCount")]
        public int ExtractorCount()
        {
            return Interface().GetExtractors().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Extractor) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Extractor);
        }

        [ExecutableSense("HydraDenCount")]
        public int HydraDenCount()
        {
            return Interface().GetHydraDens().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Hydralisk_Den) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Hydralisk_Den);
        }

        [ExecutableSense("LairCount")]
        public int LairCount()
        {
            return Interface().GetLairs().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Lair) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Lair);
        }

        [ExecutableSense("SunkenColonyCount")]
        public int SunkenColonyCount()
        {
            return Interface().GetSunkenColonies().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Sunken_Colony) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Sunken_Colony);
        }



        /// <summary>
        /// Select a unit for building a structure
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("HaveBuilder")]
        public bool HaveBuilder()
        {
            builder = UnitManager().GetDrone();

            return (builder is Unit) ? true : false;
        }

        [ExecutableSense("HaveNaturalHatchery")]
        public bool NaturalHatchery()
        {
            TilePosition natural = Interface().baseLocations.ContainsKey((int)BuildSite.Natural) ? Interface().baseLocations[(int)BuildSite.Natural] : null;
            TilePosition start = Interface().baseLocations[(int)BuildSite.StartingLocation];

            // natural not known
            if (natural == null)
                return false;

            // arbitratry distance measure to determine if the hatchery is closer to the natural or the starting location
            double dist = new Position(natural).getDistance(new Position(start)) / 3;

            if (Interface().GetHatcheries().Where(hatch => hatch.getDistance(new Position(natural)) < dist).Count() > 0)
                return true;

            foreach (Unit unit in this.buildingInProgress.Keys)
                if (unit.getType().getID() == bwapi.UnitTypes_Zerg_Hatchery.getID() &&
                    unit.getDistance(new Position(natural)) < dist)
                    return true;

            return false;
        }

        [ExecutableSense("BuildingDamaged")]
        public bool BuildingDamaged()
        {

            IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(building => building.isCompleted() && building.getHitPoints() < building.getType().maxHitPoints()).Where(building => building.getHitPoints() > 0);
            
            return (buildings.Count() > 0);
        }

        [ExecutableSense("FindDamagedBuilding")]
        public bool FindDamagedBuilding()
        {
            IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(building => building.isCompleted() && building.getHitPoints() < building.getType().maxHitPoints())
                .Where(building => building.getHitPoints() > 0)
                .OrderBy(building => building.getHitPoints());
            
            // nothing to repair so reset memory and continue
            if (buildings.Count() < 1)
            {
                repairDrone = null;
                buildingToRepair = null;
                return false;
            }
            if (repairDrone == null || repairDrone.getHitPoints() <= 0)
                repairDrone = Interface().GetDrones().Where(drone => drone.getHitPoints() > 0).OrderBy(drone => drone.getDistance(buildings.First())).First();

            if (buildingToRepair == null || buildingToRepair.getHitPoints() <= 0 )
                buildingToRepair = buildings.First();

            return (repairDrone is Unit && buildingToRepair is Unit);
        }


    }
}
