using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using BWAPI;
using POSH.sys.strict;
using SWIG.BWTA;
using log4net;
using POSH_StarCraftBot.logic;

namespace POSH_StarCraftBot
{
    public enum BuildSite { Error = -1, None = 0, StartingLocation = 1, Natural = 2, Extension = 3 };
    public enum ForceLocations { NotAssigned = 0, OwnStart = 1, Natural = 2, Extension = 3, NaturalChoke = 4, EnemyNatural = 5, EnemyStart = 6, ArmyOne = 7, ArmyTwo = 8 };
    public enum GamePhase { Early, Mid, End }
    
    public class BODStarCraftBot : IStarcraftBot
    {

        protected internal Dictionary<string,Player> ActivePlayers { get; private set; }
        protected internal Dictionary<long, Unit> UnitDiscovered { get; private set; }
        protected internal Dictionary<long, Unit> UnitEvade { get; private set; }

        protected internal Dictionary<long, Unit> UnitShow { get; private set; }
        protected internal Dictionary<long, Unit> UnitHide { get; private set; }

        protected internal Dictionary<long, Unit> UnitCreated { get; private set; }
        protected internal Dictionary<long, Unit> UnitDestroyed { get; private set; }
        protected internal Dictionary<long, Unit> UnitMorphed { get; private set; }
        protected internal Dictionary<long, Unit> UnitRenegade { get; private set; }

        private int[] mapDim;
        protected log4net.ILog LOG;
        
        protected internal POSH_StarCraftBot.behaviours.AStarCraftBehaviour.Races enemyRace { get; set; }

        /// <summary>
        /// Contains upto 7 forces of different size. The forces are at forcePoints identified by the same force location key.
        /// Forces ArmyOne and ArmyTwo are mobile and have no fixed TilePosition. 
        /// </summary>
        protected internal Dictionary<ForceLocations, List<UnitAgent>> forces;

        /// <summary>
        /// The forcePoints identify upto 8 different locations based on the ForceLocation associated with a forcePoint. 
        /// Forces ArmyOne and ArmyTwo are moving forces whereas the others are static at specific locations.
        /// </summary>
        protected internal Dictionary<ForceLocations, TilePosition> forcePoints;
        protected internal ForceLocations currentForcePoint;

        /// <summary>
        /// The base locations used within the game. "0" is starting location, "1" is natural, "2" is first Extension
        /// </summary>
        public Dictionary<int,TilePosition> baseLocations { get; set; }

        

        /// <summary>
        /// The base we want to build at. "O" means starting base, "1" is natural "2" is first Extension "-1" is error state.
        /// </summary>
        public BuildSite currentBuildSite;

        private int[] mineralPatchIDs = new int[3] { bwapi.UnitTypes_Resource_Mineral_Field.getID(), 
                bwapi.UnitTypes_Resource_Mineral_Field_Type_2.getID(), 
                bwapi.UnitTypes_Resource_Mineral_Field_Type_3.getID() };
        public BODStarCraftBot(ILog log)
        {
            this.LOG = log;
        }

        void IStarcraftBot.onStart()
        {
            System.Console.WriteLine("Starting Match!");
            bwapi.Broodwar.sendText("Hello world! This is POSH!");
            mapDim = new int[2];
            ActivePlayers = new Dictionary<string,Player>();
            UnitDiscovered = new Dictionary<long, Unit>();
            UnitEvade = new Dictionary<long, Unit>();
            UnitShow = new Dictionary<long, Unit>();
            UnitHide = new Dictionary<long, Unit>();

            UnitCreated = new Dictionary<long, Unit>();
            UnitDestroyed = new Dictionary<long, Unit>();
            UnitMorphed = new Dictionary<long, Unit>();
            UnitRenegade = new Dictionary<long, Unit>();

            baseLocations = new Dictionary<int,TilePosition>();
            currentBuildSite = BuildSite.StartingLocation;

            foreach (Player pl in bwapi.Broodwar.getPlayers())
                ActivePlayers.Add(pl.getName(),pl);
            if (ActivePlayers.ContainsKey(Self().getName()))
                ActivePlayers.Remove(Self().getName());

            forces = new Dictionary<ForceLocations, List<UnitAgent>>();
            forcePoints = new Dictionary<ForceLocations, TilePosition>();

                // initiating the starting location
            if (Self().getStartLocation() is TilePosition)
            {
                baseLocations[(int)BuildSite.StartingLocation] = Self().getStartLocation();
                forcePoints[ForceLocations.OwnStart] = Self().getStartLocation();

            }

            currentForcePoint = ForceLocations.OwnStart;
            currentBuildSite = BuildSite.StartingLocation;
        }
        //
        // own Player
        //
        public Player Self()
        {
            return bwapi.Broodwar.self();
        }

        public bool GameRunning()
        {
            return bwapi.Broodwar.isInGame();
        }

        public TilePosition StartLocation()
        {
            return baseLocations[0];
        }

        public int GetMapHeight()
        {
            if (mapDim[0] < 1)
                mapDim[0] = bwapi.Broodwar.mapHeight();

            return mapDim[0];
        }

        public int GetMapWidht()
        {
            if (mapDim[1] < 1)
                mapDim[1] = bwapi.Broodwar.mapHeight();

            return mapDim[1];
        }

        //
        //
        // Zerg Resources
        //
        //

        public int MineralCount()
        {
            return bwapi.Broodwar.self().minerals();
        }

        public int GasCount()
        {
            return bwapi.Broodwar.self().gas();
        }

        public int AvailableSupply()
        {
            return (bwapi.Broodwar.self().supplyTotal() - bwapi.Broodwar.self().supplyUsed()) / 2;
        }

        public int SupplyCount()
        {
            return (bwapi.Broodwar.self().supplyUsed()) / 2;
        }

        /// <summary>
        /// Supply is devided by 2 because BWAPI doubled the amount to have a mininimum of 1 supply for Zerglings which is normally 0.5
        /// </summary>
        /// <returns></returns>
        public int TotalSupply()
        {
            return bwapi.Broodwar.self().supplyTotal() / 2;
        }

        //
        //
        // Zerg Units
        //
        //

        public int LarvaeCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Larva);
        }

        public int DroneCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Drone);
        }

        public int ZerglingCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Zergling);
        }

        public int OverlordCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Overlord);
        }

        public int HydraliskCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Hydralisk);
        }

        public int MutaliskCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Mutalisk);
        }

        public int LurkerCount()
        {
            return bwapi.Broodwar.self().allUnitCount(bwapi.UnitTypes_Zerg_Lurker);
        }

        public IEnumerable<Unit> GetLarvae()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Larva.getID());
        }

        public IEnumerable<Unit> GetLarvae(int amount)
        {
            return GetLarvae().Take(amount);
        }

        public IEnumerable<Unit> GetDrones(int amount)
        {
            return GetDrones().Take(amount);
        }
        public IEnumerable<Unit> GetDrones()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Drone.getID());
        }

        public IEnumerable<Unit> GetIdleDrones()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Drone.getID()).Where(drone => drone.isIdle());
        }

        public IEnumerable<Unit> GetZerglings(int amount)
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Zergling.getID()).Take(amount);
        }

        public IEnumerable<Unit> GetOverlord(int amount)
        {
            return GetOverlord().Take(amount);
        }

        public IEnumerable<Unit> GetOverlord()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Overlord.getID());
        }

        public IEnumerable<Unit> GetHydralisk(int amount)
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Hydralisk.getID()).Take(amount);
        }

        public IEnumerable<Unit> GetMutalisk(int amount)
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Mutalisk.getID()).Take(amount);
        }

        public IEnumerable<Unit> GetLurker(int amount)
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Lurker.getID()).Take(amount);
        }

        public IEnumerable<Unit> GetAllUnits(bool worker)
        {
            return bwapi.Broodwar.self().getUnits().Where(unit =>
                !unit.getType().isBuilding() && 
                (worker) ? unit.getType().isWorker() : !unit.getType().isWorker()
                );
        }


        //
        //
        // Zerg Buildings
        //
        //

        public IEnumerable<Unit> GetHatcheries()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Hatchery.getID());
        }

        public IEnumerable<Unit> GetLairs()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Lair.getID());
        }

        public IEnumerable<Unit> GetExtractors()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Extractor.getID());
        }

        public IEnumerable<Unit> GetHydraDens()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Hydralisk_Den.getID());
        }

        public IEnumerable<Unit> GetSunkenColonies()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Sunken_Colony.getID());
        }

        public IEnumerable<Unit> GetCreepColonies()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Creep_Colony.getID());
        }

        public IEnumerable<Unit> GetSporeColonies()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Spore_Colony.getID());
        }

        public IEnumerable<Unit> GetSpire()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Spire.getID());
        }

        public IEnumerable<Unit> GetGreaterSpire()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Greater_Spire.getID());
        }

        public IEnumerable<Unit> GetAllBuildings()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit =>
                unit.getType().isBuilding()
                );
        }

        public IEnumerable<Unit> GetGeysers()
        {
            return bwapi.Broodwar.getGeysers().Where(patch => patch.getResources() > 0);
        }

        public IEnumerable<Unit> GetMineralPatches()
        {
            return bwapi.Broodwar.getMinerals().Where(patch => patch.getResources() > 0);
        }

        /// <summary>
        /// Clears the internal dictionaries which keep track of the incomming information from the game.
        /// </summary>
        /// <param name="history">
        ///     Specifies length of time in milliseconds from the current time until the last event to remember.
        ///     Everything older that that is removed from the memory.
        /// </param>
        public void ReleaseOldInfo(long history = 60000L)
        {
            long leaseTime = Core.Timer.Time() - history;
            if (leaseTime < 0L)
                leaseTime = 0L;

            emptyDictionaryBeforeTimeStamp(10000L, new Dictionary<long, Unit>[] { UnitDestroyed, UnitEvade, UnitShow, UnitHide, UnitCreated, UnitDestroyed, UnitMorphed, UnitRenegade });


        }

        private void emptyDictionaryBeforeTimeStamp(long timestamp, Dictionary<long, Unit>[] memories)
        {
            foreach (Dictionary<long, Unit> memory in memories)
                emptyDictionaryBeforeTimeStamp(timestamp, memory);
        }
        private void emptyDictionaryBeforeTimeStamp(long timestamp, Dictionary<long, Unit> memory)
        {
            foreach (long evnt in memory.Keys)
                if (evnt <= timestamp)
                    memory.Remove(evnt);
        }

        void IStarcraftBot.onEnd(bool isWinner)
        {
            //throw new NotImplementedException();
        }

        void IStarcraftBot.onFrame()
        {
            //UnitPtrSet set =  bwapi.Broodwar.getMinerals();

        }

        void IStarcraftBot.onSendText(string text)
        {
            //throw new NotImplementedException();
        }

        void IStarcraftBot.onReceiveText(SWIG.BWAPI.Player player, string text)
        {
            //throw new NotImplementedException();
        }

        void IStarcraftBot.onPlayerLeft(SWIG.BWAPI.Player player)
        {
            if (ActivePlayers.ContainsKey(player.getName()))
                ActivePlayers.Remove(player.getName());
        }

        void IStarcraftBot.onNukeDetect(SWIG.BWAPI.Position target)
        {
            //throw new NotImplementedException();
        }

        void IStarcraftBot.onUnitDiscover(SWIG.BWAPI.Unit unit)
        {
            UnitDiscovered[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitEvade(SWIG.BWAPI.Unit unit)
        {
            UnitEvade[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitShow(SWIG.BWAPI.Unit unit)
        {
            UnitShow[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitHide(SWIG.BWAPI.Unit unit)
        {
            UnitHide[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitCreate(SWIG.BWAPI.Unit unit)
        {
            UnitCreated[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitDestroy(SWIG.BWAPI.Unit unit)
        {
            UnitDestroyed[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitMorph(SWIG.BWAPI.Unit unit)
        {
            UnitMorphed[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onUnitRenegade(SWIG.BWAPI.Unit unit)
        {
            UnitRenegade[Core.Timer.Time()] = unit;
        }

        void IStarcraftBot.onSaveGame(string gameName)
        {
            //throw new NotImplementedException();
        }
    }
}
