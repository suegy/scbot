using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using BWAPI;
using POSH.sys.strict;
using SWIG.BWTA;

namespace POSH_StarCraftBot
{
    public class BODStarCraftBot : IStarcraftBot
    {

        protected internal Dictionary<string, Player>   ActivePlayers { get; private set; }
        protected internal Dictionary<long, Unit>       UnitDiscovered { get; private set; }
        protected internal Dictionary<long, Unit>       UnitEvade { get; private set; }
        
        protected internal Dictionary<long, Unit>       UnitShow { get; private set; }
        protected internal Dictionary<long, Unit>       UnitHide { get; private set; }
        
        protected internal Dictionary<long, Unit>       UnitCreated { get; private set; }
        protected internal Dictionary<long, Unit>       UnitDestroyed { get; private set; }
        protected internal Dictionary<long, Unit>       UnitMorphed { get; private set; }
        protected internal Dictionary<long, Unit>       UnitRenegade { get; private set; }

        private int[] mineralPatchIDs = new int[3] { bwapi.UnitTypes_Resource_Mineral_Field.getID(), 
                bwapi.UnitTypes_Resource_Mineral_Field_Type_2.getID(), 
                bwapi.UnitTypes_Resource_Mineral_Field_Type_3.getID() };

        
        void IStarcraftBot.onStart()
        {
            System.Console.WriteLine("Starting Match!");
            bwapi.Broodwar.sendText("Hello world!");

            ActivePlayers = new Dictionary<string, Player>();
            UnitDiscovered = new Dictionary<long, Unit>();
            UnitEvade = new Dictionary<long,Unit>();
            UnitShow = new Dictionary<long, Unit>();
            UnitHide = new Dictionary<long, Unit>();

            UnitCreated = new Dictionary<long, Unit>();
            UnitDestroyed = new Dictionary<long, Unit>();
            UnitMorphed = new Dictionary<long, Unit>();
            UnitRenegade = new Dictionary<long, Unit>();

            foreach (Player pl in bwapi.Broodwar.getPlayers())
                ActivePlayers[pl.getName()] = pl;

            bwapi.Broodwar.self().minerals();
        }
        //
        // own Player
        //
        public Player Self()
        {
            return bwapi.Broodwar.self();
        }

        public BaseLocation StartLocation()
        {
            return bwta.getStartLocation(Self());
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
            return ( bwapi.Broodwar.self().supplyTotal() - bwapi.Broodwar.self().supplyUsed() );
        }

        public int TotalSupply()
        {
            return bwapi.Broodwar.self().supplyTotal();
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

        public IEnumerable<Unit> GetLarvae(int amount)
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Larva.getID()).Take(amount);
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
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Zerg_Overlord.getID()).Take(amount);
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

        public IEnumerable<Unit> GetGeysers()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => unit.getType().getID() == bwapi.UnitTypes_Resource_Vespene_Geyser.getID());
        }

        public IEnumerable<Unit> GetMineralPatches()
        {
            return bwapi.Broodwar.self().getUnits().Where(unit => mineralPatchIDs.Contains(unit.getType().getID())).Where(patch => patch.getResources() > 0);
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
            long leaseTime = Core.Timer.Time()-history;
            if (leaseTime < 0L)
                leaseTime = 0L;

            emptyDictionaryBeforeTimeStamp(10000L, new Dictionary<long, Unit>[] 
                { UnitDestroyed, UnitEvade, UnitShow, UnitHide, UnitCreated, UnitDestroyed, UnitMorphed, UnitRenegade} );


        }

        private void emptyDictionaryBeforeTimeStamp(long timestamp, Dictionary<long, Unit>[] memories)
        {
            foreach (Dictionary<long, Unit> memory in memories)
                emptyDictionaryBeforeTimeStamp(timestamp, memory);
        }
        private void emptyDictionaryBeforeTimeStamp(long timestamp,Dictionary<long,Unit> memory)
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
