using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using SWIG.BWAPI;
using SWIG.BWTA;

namespace POSH_StarCraftBot.behaviours
{
    public class BuildingControl : AStarCraftBehaviour
    {
        TilePosition buildLocation;
        Unit builder;
        TilePosition activeBase;
        TilePosition centerBase;
        ICollection<TilePosition> extentionBases;

        public BuildingControl(AgentBase agent)
            : base(agent, 
            new string[] {},
            new string[] {})
        {
            extentionBases = new HashSet<TilePosition>();
        }


        //
        // INTERNAL
        //
        
        private TilePosition PossibleBuildLocation(TilePosition start, int xSpace, int ySpace, int iterations, Unit builder, UnitType building)
        {
            const int xSpread = 10;
            const int ySpread = 10;

            TilePosition[] directions = new TilePosition[] {
                new TilePosition(xSpace,ySpace), new TilePosition(xSpread*xSpace,0), new TilePosition(-xSpread*xSpace,0),
                new TilePosition(xSpread*xSpace,ySpread*ySpace),new TilePosition(xSpread*xSpace,-ySpread*ySpace),
                new TilePosition(-xSpread*xSpace,ySpread*ySpace),new TilePosition(-xSpread*xSpace,-ySpread*ySpace),
                new TilePosition(0,ySpread*ySpace),new TilePosition(0,-ySpread*ySpace) };
            if (iterations < 0)
                return null;

            
            foreach (TilePosition pos in directions)
            {
                if (bwapi.Broodwar.canBuildHere(this.builder, start.opAdd(pos), bwapi.UnitTypes_Zerg_Spawning_Pool))
                    return start.opAdd(pos);
            }

            return PossibleBuildLocation(start, xSpace++, ySpace++, iterations--, builder, building);
        }


        //
        // ACTIONS
        //
        [ExecutableAction("SelectExtractorLocation")]
        public bool SelectExtractorLocation()
        {
            // enough resources available?
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Extractor))
                return false;

            // are there any geysers available/visible?
            IEnumerable<Unit> geysers = Interface().
                                        GetGeysers().
                                        Where(geyser => geyser.getType() == bwapi.UnitTypes_Resource_Vespene_Geyser).
                                        Where(geyser => geyser.hasPath(Interface().StartLocation().getPosition()));
            if (geysers.Count() < 1)
                return false;

            // are there geysers which are stored in the baselocation which can be used?
            if (geysers.Where(geyser => Interface().StartLocation().getGeysers().Contains(geyser)).Count() > 0 )
            {
                buildLocation = geysers.Where(geyser => Interface().StartLocation().getGeysers().Contains(geyser)).ElementAt(0).getTilePosition();
                return true;
            }

            // get the positions of all accessible geysers
            TilePositionSet set  = (TilePositionSet)geysers.Select(geyser => geyser.getTilePosition());

            TilePosition closest = bwta.getGroundDistances(Interface().StartLocation().getTilePosition(), set).
                                    OrderBy(pos => pos.Value).
                                    ElementAt(0).Key;
            
            // if there is a close geyers we are done
            if (closest is TilePosition)
            {
                buildLocation = closest;
                return true;
            }

            return false;
        }

        [ExecutableAction("BuildExtractor")]
        public bool BuildExtractor()
        {
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Extractor) && buildLocation is TilePosition)
                return Interface().
                    GetDrones().
                    Where(drone => buildLocation.hasPath(drone.getTilePosition())).
                    OrderBy(drone => buildLocation.getDistance(drone.getTilePosition())).
                    ElementAt(0).build(buildLocation,bwapi.UnitTypes_Zerg_Extractor);

            return false;
        }



        /// <summary>
        /// Select suitable location for the spawning pool
        /// </summary>
        /// <returns></returns>
        [ExecutableAction("SelLocSpwnPl")]
        public bool SelLocSpwnPl()
        {
            TilePosition buildPosition;
            if (activeBase == null)
                activeBase = Interface().GetHatcheries().Where(hatchery => hatchery.getPlayer().getID() == Interface().Self().getID()).First().getTilePosition();

            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            buildPosition= PossibleBuildLocation(activeBase, 10, 0, 10, this.builder, bwapi.UnitTypes_Zerg_Spawning_Pool);
            if (buildPosition != null)
            {
                this.buildLocation = buildPosition;
                return true;
            }
            return false;
        }

        [ExecutableAction("BuildSpwnPl")]
        public bool BuildSpwnPl()
        {
            return builder.build(buildLocation, bwapi.UnitTypes_Zerg_Spawning_Pool);
        }


        [ExecutableAction("SelSecondBase")]
        public bool SelSecondBase()
        {
            if (centerBase == null)
                centerBase = Interface().GetHatcheries().Where(hatchery => hatchery.getPlayer().getID() == Interface().Self().getID()).First().getTilePosition();
            
            TilePosition pos = bwta.getBaseLocations().Where(loc => loc.getTilePosition() != centerBase)
                .OrderBy(loc => centerBase.getDistance(loc.getTilePosition()))
                .ElementAt(0).getTilePosition();
            if (pos != null && !extentionBases.Contains(pos))
            {
                pos = PossibleBuildLocation(pos, 0, 0, 10, this.builder, bwapi.UnitTypes_Zerg_Spawning_Pool);
                if (pos != null)
                {
                    this.buildLocation = pos;
                    return true;
                }
            }
            
            return false;
        }

        [ExecutableAction("BuildHatchery")]
        public bool BuildHatchery()
        {
            return builder.build(buildLocation, bwapi.UnitTypes_Zerg_Hatchery);
        }

        [ExecutableAction("UpgradeHatchery")]
        public bool UpgradeHatchery()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Lair))
                return false;

            return Interface().GetHatcheries()
                .OrderBy(hatch => centerBase.getDistance(hatch.getTilePosition()))
                .ElementAt(0).morph(bwapi.UnitTypes_Zerg_Lair);
        }

        //
        // SENSES
        //
        [ExecutableSense("HatcheryCount")]
        public int HatcheryCount()
        {
            return Interface().GetHatcheries().Count();
        }

        [ExecutableSense("ExtractorCount")]
        public int ExtractorCount()
        {
            return Interface().GetExtractors().Count();
        }

        /// <summary>
        /// Select a unit for building a structure
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("HaveBuilder")]
        public bool HaveBuilder()
        {
            builder = UnitManager().GetDrone();

            return (builder == null) ? true : false;
        }

        [ExecutableSense("NeedBuilding")]
        public bool NeedBuilding()
        {
            return true;
        }
    }
}
