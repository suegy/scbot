﻿using System;
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
    public class CombatControl : AStarCraftBehaviour
    {

        /// <summary>
        /// the key is the units ID which does not change over the course of a game
        /// </summary>
        public Dictionary<int,Unit> enemyBuildings;

        /// <summary>
        /// the key is the units ID which does not change over the course of a game
        /// </summary>
        public Dictionary<int, Unit> enemyUnits;
        Position targetPosition;

        ForceLocations currentForce;
        /// <summary>
        /// contains the targets for the two armies we can control also identified by ForceLocations ArmyOne and ArmyTwo
        /// </summary>
        Dictionary<ForceLocations,ForceLocations> armyTargets;

        List<UnitAgent> selectedForce;
        Dictionary<ForceLocations, TacticalAgent> fights;

        private IEnumerable<BaseLocation> enemyStartLocations;

        public CombatControl(AgentBase agent)
            : base(agent, new string[] {}, new string[] {})
        {
            enemyBuildings = new Dictionary<int, Unit>();
            enemyUnits = new Dictionary<int, Unit>();
            armyTargets = new Dictionary<ForceLocations, ForceLocations>();
            selectedForce = new List<UnitAgent>();
            fights = new Dictionary<ForceLocations, TacticalAgent>();
        }

        //
        // INTERNAL
        //
        void AttackPursuer(IEnumerable<Unit> units)
        {
        }

        public static Position CalculateCentroidPosition(IEnumerable<Unit> units)
        {   
            double[] pos = CalculateCentroid(units);
            return new Position((int)pos[0], (int)pos[1]);
        }

        public static double[] CalculateCentroid(IEnumerable<Unit> units)
        {
            double[] centroid = new double[2];
            foreach (Unit unit in units)
            {
                centroid[0] += unit.getPosition().xConst();
                centroid[1] += unit.getPosition().yConst();
            }
            centroid[0] /= units.Count();
            centroid[1] /= units.Count();
            
            
            return centroid;
        }

        private bool DriftingTowardsLocation(Unit unit, Position location)
        {
            if (!unit.isMoving())
                return false;

            double [] pos = {unit.getPosition().xConst(), unit.getPosition().yConst()};
            double [] vector = {location.xConst() - pos[0],location.yConst() - pos[1]}; 


            if (
                (vector[0] < 0.00D && unit.getVelocityX() <= 0.00D &&
                vector[1] < 0.00D && unit.getVelocityY() <= 0.00D ) ||
                (vector[0] > 0.00D && unit.getVelocityX() >= 0.00D &&
                vector[1] > 0.00D && unit.getVelocityY() >= 0.00D )
                )
                return true;

            return false;
        }

        private Position AbsolutPosition(Position origin, int[] localPos, int viewSize)
        {
            int range = viewSize / 2;
            int [] absolut = new int[2];

            if (localPos[0] < range) // left of origin
            {
                absolut[0] = origin.xConst() - localPos[0];
            }
            else // right of origin
            {
                absolut[0] = origin.xConst() + localPos[0] - range;
            }

            // checking y position
            if (localPos[1] < range) // below of origin
            {
                absolut[1] = origin.xConst() - localPos[1];
            }
            else // above of origin
            {
                absolut[1] = origin.xConst() + localPos[1] - range;
            }

            return new Position(absolut[0],absolut[1]);
        }

        private int[] RelativPosition(Position origin, Position absolutPos)
        {
            int[] relative = new int[2];

            relative[0] = origin.xConst() - absolutPos.xConst();
            relative[1] = origin.yConst() - absolutPos.yConst();

            return relative;
        }

        //void Attack(IEnumerable<Unit> units, double deltaDist = DELTADISTANCE,Position baseCentroid = null, bool buildings = false)
        //{
        //    if (units.Count() == 0 )
        //        return;
        //    CheckEnemyUnits();
        //    if (buildings)
        //        CheckEnemyBuildings();

        //    Position centroid = (baseCentroid is Position) ? baseCentroid : CalculateCentroidPosition(units);
            
        //    // heatmap centered around the centroid containing attack attractors
        //    double [][] attack = new double[50][];
        //    double [][] move = new double[50][];
            
        //    for (int x=0; x<attack.Length; x++) 
        //    {
        //        attack[x] = new double[50];
        //        move[x] = new double[50];
        //        for (int y=0; y<move[x].Length; y++)
        //        {
        //            move[x][y] = ( AbsolutPosition(centroid,new int[]{x,y},move.Length).isValid() ) ? 0 : -1000;
        //        }
        //    }

        //    foreach (Unit enemy in enemyUnits.Values)
        //    {
        //        attack = ApplyUnitToHeatMap(attack,centroid,enemy);
        //    }


        //    foreach(Unit unit in enemyUnits)
        //    {

        //    }

            
        //    foreach (Unit unit in units)
        //        if (unit.getDistance(centroid) > deltaDist || !DriftingTowardsLocation(unit,centroid))
        //            unit.move(centroid, true);
        //        else
                    

        //    units.First().we
        //}

        void CheckEnemyBuildings()
        {
            foreach (int key in enemyBuildings.Keys)
                enemyBuildings.Where(unit => unit.Value.getHitPoints() <= 0);
        }

        void CheckEnemyUnits()
        {
            foreach(int key in enemyUnits.Keys)
                enemyUnits.Where(unit => unit.Value.getHitPoints() <= 0);
        }

        void UpdateUnits()
        {
            if (Interface().forces.ContainsKey(currentForce) && Interface().forces[currentForce] is List<UnitAgent>)
                Interface().forces[currentForce].RemoveAll(unit => unit.SCUnit.getHitPoints() <= 0);
        }

        protected bool AttackLocation(ForceLocations location)
        {
            TacticalAgent agent = null;
            
            if (fights.ContainsKey(location))
                agent = fights[location];
            else if (selectedForce != null && selectedForce.Count > 0)
            {
                agent = new TacticalAgent(selectedForce, log);
                fights.Add(location, agent);
            }
            if (agent.MySquad.Count > 0)
            { // update the squat by removing dead units
                agent.MySquad.RemoveAll(unit => unit.HealthLevelOk == 0);
            }
            if (agent.MySquad.Count == 0)
            {
                fights.Remove(location); //Own Amy annihilated
                return false;
            }

            if (agent.MySquad[0].SCUnit.getPosition().getApproxDistance(new Position(Interface().baseLocations[(int)location])) < 5 * DELTADISTANCE 
                || location == ForceLocations.NotAssigned || agent.MySquad[0].SCUnit.isUnderAttack()) //larger distance to not be over the base
                agent.ExecuteBestActionForSquad();
            else
            {
                agent.MySquad.All(ua => ua.SCUnit.rightClick(new Position(Interface().baseLocations[(int)location])));
            }

            return true;

        }

        private ForceLocations ClosestLocation(Unit unit, ForceLocations first, ForceLocations second)
        {
            double distanceFirst = Interface().baseLocations[(int)first].getDistance(unit.getTilePosition());
            double distanceSecond = Interface().baseLocations[(int)second].getDistance(unit.getTilePosition());

            return (distanceFirst < distanceSecond) ? first : second;
        }


        //
        // ACTIONS
        //
        [ExecutableAction("RetreatForce")]
        public bool RetreatForce()
        {
            if (!Interface().forces.ContainsKey(currentForce))
                return false;

            ForceLocations loc = ForceLocations.NaturalChoke;
            UpdateUnits();

            if (!Interface().forcePoints.ContainsKey(loc))
                loc = ForceLocations.OwnStart;
            
            Interface().forces[currentForce].Where(unit => unit.SCUnit.move(new Position(Interface().forcePoints[loc]), true));

                foreach (UnitAgent unit in Interface().forces[currentForce].Where(unit => unit.SCUnit.getTargetPosition().getDistance(new Position(Interface().forcePoints[loc])) < DELTADISTANCE))
                {
                    Interface().forces[loc].Add(unit);
                }

                Interface().forces[currentForce].RemoveAll(unit => unit.SCUnit.getTargetPosition().getDistance(new Position(Interface().forcePoints[loc])) < DELTADISTANCE);
            
            return true;
        }

        [ExecutableAction("SelectAttackedBase")]
        public bool SelectAttackedBase()
        {
            if (Interface().GetHatcheries().Count() < 1)
                return false;

            double damageStart = 0;
            double damageNatural = 0;

            IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(building => building.isUnderAttack());
            foreach (Unit building in buildings)
            {
                if (ClosestLocation(building, ForceLocations.OwnStart, ForceLocations.Natural) == ForceLocations.OwnStart)
                    damageStart += (building.getType().maxHitPoints() - building.getHitPoints());
                else
                    damageNatural += (building.getType().maxHitPoints() - building.getHitPoints());
            }

            if (damageStart < 1 && damageNatural < 1)
                return false;
            armyTargets[ForceLocations.ArmyTwo] = (damageStart >= damageNatural) ?ForceLocations.OwnStart : ForceLocations.Natural;

            return true;
        }
        bool stopAttacks = false;

        [ExecutableAction("StopAttacks")]
        public bool StopAttack()
        {
            stopAttacks = true;
            return stopAttacks;
        }

        [ExecutableAction("AttackEnemyMainBase")]
        public bool AttackEnemyMainBase()
        {
            return AttackLocation(ForceLocations.EnemyStart);
                
        }

        [ExecutableAction("AttackEnemyNatural")]
        public bool AttackEnemyNatural()
        {
            return AttackLocation(ForceLocations.EnemyNatural);
        }

        [ExecutableAction("AttackEnemyUnDirected")]
        public bool AttackEnemyUnDirected()
        {
            return AttackLocation(ForceLocations.NotAssigned);
        }

        [ExecutableAction("FendOffUnits")]
        public bool FendOffUnits()
        {
            return AttackLocation(this.armyTargets[ForceLocations.ArmyTwo]);
        }

        [ExecutableAction("SelectForceStartingLocation")]
        public bool SelectForceStartingLocation()
        {
            currentForce = ForceLocations.OwnStart;

            return true;
        }

        [ExecutableAction("SelectForceNatural")]
        public bool SelectForceNatural()
        {
            currentForce = ForceLocations.Natural;

            if (Interface().baseLocations.ContainsKey((int)ForceLocations.Natural))
                return true;
            currentForce = ForceLocations.OwnStart;

            return false;
        }

        [ExecutableAction("SelectForceExtension")]
        public bool SelectForceExtension()
        {
            currentForce = ForceLocations.Extension;

            if (Interface().baseLocations.ContainsKey((int)ForceLocations.Extension))
                return true;
            currentForce = ForceLocations.OwnStart;

            return false;
        }

        [ExecutableAction("SelectAllUnits")]
        public bool SelectAllUnits()
        {
            IEnumerable<Unit> force = Interface().GetAllUnits(false);
            List<UnitAgent> ag = new List<UnitAgent>();
            foreach (Unit un in force)
                ag.Add(new UnitAgent(un, new UnitAgentOptimizedProperties(), this)); //TODO: needs to be altered to be more elegant why not creatre UnitAgents when the unit is created in UnitControl

            selectedForce = ag;

            return (selectedForce.Count > 0) ? true : false;
        }

        [ExecutableAction("AssignArmyOne")]
        public bool AssignArmyOne()
        {
            IEnumerable<Unit> force = Interface().GetAllUnits(false).Where(unit=> unit.getDistance(new Position(Interface().forcePoints[currentForce])) < DELTADISTANCE);
            
            List<UnitAgent> ag = new List<UnitAgent>();
            foreach (Unit un in force)
                if (!Interface().forces.ContainsKey(ForceLocations.ArmyTwo) || !(Interface().forces[ForceLocations.ArmyTwo] is List<UnitAgent>) || Interface().forces[ForceLocations.ArmyTwo].Count < 1 || Interface().forces[ForceLocations.ArmyTwo].First(unit => unit.SCUnit.getID() == un.getID()) == null)
                    ag.Add(new UnitAgent(un, new UnitAgentOptimizedProperties(), this)); //TODO: needs to be altered to be more elegant why not creatre UnitAgents when the unit is created in UnitControl


            Interface().forces[ForceLocations.ArmyOne] = ag;

            return (Interface().forces.ContainsKey(ForceLocations.ArmyOne) && Interface().forces[ForceLocations.ArmyOne] is List<UnitAgent> )? true : false; 
        }

        [ExecutableAction("AssignArmyTwo")]
        public bool AssignArmyTwo()
        {
            IEnumerable<Unit> force = Interface().GetAllUnits(false);

            List<UnitAgent> ag = new List<UnitAgent>();
            foreach (Unit un in force)
            {
                if (!Interface().forces.ContainsKey(ForceLocations.ArmyOne) || !(Interface().forces[ForceLocations.ArmyOne] is List<UnitAgent>) || Interface().forces[ForceLocations.ArmyOne].Count < 1 ||Interface().forces[ForceLocations.ArmyOne].First(unit => unit.SCUnit.getID() == un.getID()) == null )
                    ag.Add(new UnitAgent(un, new UnitAgentOptimizedProperties(), this)); //TODO: needs to be altered to be more elegant why not creatre UnitAgents when the unit is created in UnitControl

            }
            Interface().forces[ForceLocations.ArmyTwo] = ag;

            return (Interface().forces.ContainsKey(ForceLocations.ArmyTwo) && Interface().forces[ForceLocations.ArmyTwo] is List<UnitAgent>) ? true : false; 
        }

        [ExecutableAction("SelectForce")]
        public bool SelectForce()
        {
            IEnumerable<Unit> force = Interface().GetAllUnits(false).Where(unit => unit.getDistance(new Position(Interface().forcePoints[currentForce])) < DELTADISTANCE);

            List<UnitAgent> ag = new List<UnitAgent>();
            foreach (Unit un in force)
            {
                if (
                    (!Interface().forces.ContainsKey(ForceLocations.ArmyOne) || !(Interface().forces[ForceLocations.ArmyOne] is List<UnitAgent>) || Interface().forces[ForceLocations.ArmyOne].First(unit => unit.SCUnit.getID() == un.getID()) == null) &&
                    (!Interface().forces.ContainsKey(ForceLocations.ArmyOne) || !(Interface().forces[ForceLocations.ArmyTwo] is List<UnitAgent>) || Interface().forces[ForceLocations.ArmyTwo].First(unit => unit.SCUnit.getID() == un.getID()) == null) 
                    )
                    ag.Add(new UnitAgent(un, new UnitAgentOptimizedProperties(), this)); //TODO: needs to be altered to be more elegant why not creatre UnitAgents when the unit is created in UnitControl
            }

            selectedForce = ag;
            
            return (selectedForce.Count > 0);
        }
        /// <summary>
        /// Switches currenty active army to Army One.
        /// </summary>
        /// <returns>Success if Army One was selected</returns>
        [ExecutableAction("SelectArmyOne",1.0f)]
        public bool SelectArmyOne()
        {
            selectedForce = Interface().forces[ForceLocations.ArmyOne];

            return (selectedForce != null && selectedForce.Count > 0);
        }

        [ExecutableAction("SelectArmyTwo")]
        public bool SelectArmyTwo()
        {
            selectedForce = Interface().forces[ForceLocations.ArmyTwo];

            return (selectedForce != null && selectedForce.Count > 0);
        }

        [ExecutableAction("SelectAllArmies")]
        public bool SelectAllArmies()
        {
            selectedForce = Interface().forces[ForceLocations.ArmyTwo];
            selectedForce.AddRange(Interface().forces[ForceLocations.ArmyTwo]);

            return (selectedForce != null && selectedForce.Count > 0);
        }

        [ExecutableSense("GuessEnemyBase")]
        public bool GuessEnemyBase()
        {
            if (enemyStartLocations is IEnumerable<BaseLocation> && enemyStartLocations.GetEnumerator().Current != null)
            {
                
                TilePosition ePos = enemyStartLocations.GetEnumerator().Current.getTilePosition();
                if (ePos != null && ePos.isValid())
                {
                    Interface().baseLocations[(int)ForceLocations.EnemyStart] = ePos;
                    enemyStartLocations.GetEnumerator().MoveNext();
                    return true;
                }
            }
            enemyStartLocations = bwta.getBaseLocations().Where(loc => loc.isStartLocation() && !loc.getTilePosition().opEquals(Interface().StartLocation()));
            enemyStartLocations.GetEnumerator().MoveNext();

            TilePosition ePos2 = enemyStartLocations.GetEnumerator().Current.getTilePosition();
            if (ePos2 != null && ePos2.isValid())
            {
                Interface().baseLocations[(int)ForceLocations.EnemyStart] = ePos2;
                enemyStartLocations.GetEnumerator().MoveNext();
                return true;
            }

            return false;
        }



        //
        // SENSES
        //


        
        [ExecutableSense("EnemyDetected")]
        public int EnemyDetected()
        {
            IEnumerable<Unit> shownUnits = Interface().UnitShow.Where(pair => pair.Key < (Core.Timer.Time() - DELTATIME)).OrderByDescending(pair => pair.Key).Select(pair => pair.Value);
            bool detectedNew = false;

            foreach (Unit unit in shownUnits)
            {
                if (unit.getHitPoints() > 0 && unit.getPlayer() != Interface().Self())
                    continue;
                if (unit.getType().isBuilding())
                {
                    if (!enemyBuildings.ContainsKey(unit.getID()))
                    {
                        enemyBuildings[unit.getID()] = unit;
                        detectedNew = true;
                    }
                }
                else // the unit is not a building 
                {
                    if (!enemyUnits.ContainsKey(unit.getID()))
                    {
                        enemyUnits[unit.getID()] = unit;
                        detectedNew = true;
                    }
                }
            }

            return (detectedNew) ? 1 : 0;
        }

        [ExecutableSense("BaseUnderAttack")]
        public bool BaseUnderAttack()
        {
            if (Interface().GetAllBuildings().Count() < 1)
                return false;
            int attackCounter = Interface().GetAllBuildings().Where(building => building.isUnderAttack()).Count();
            if (attackCounter > 0)
                return true;
            
            int randomMult = 3;

            foreach (Unit enemy in bwapi.Broodwar.enemy().getUnits().Where(unit => unit.getPosition().getDistance(new Position(Interface().baseLocations[(int)BuildSite.StartingLocation])) <= randomMult * DELTADISTANCE))
                Console.Out.WriteLine(++attackCounter+"enemy at:" + enemy.getTilePosition().xConst() + "" + enemy.getTilePosition().yConst());
            
            if (Interface().baseLocations.ContainsKey((int)BuildSite.StartingLocation))
            {
                attackCounter += Interface().GetAllUnits(true).Where(unit => unit.isUnderAttack() && unit.getPosition().getDistance(new Position(Interface().baseLocations[(int)BuildSite.StartingLocation])) < randomMult * DELTADISTANCE).Count();
            }
            if (Interface().baseLocations.ContainsKey((int)BuildSite.Natural))
            {
                attackCounter += Interface().GetAllUnits(true).Where(unit => unit.isUnderAttack() && unit.getPosition().getDistance(new Position(Interface().baseLocations[(int)BuildSite.Natural])) < randomMult * DELTADISTANCE).Count();
            }
            return attackCounter > 0;
        }

        /// <summary>
        /// returns the ForceLocation identifier of the force losing. There are currently 8 forceLocations specified in BODStarraftBot.
        /// Zero means no force is losing.
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("ForceIsLosing")]
        public int ForceIsLosing()
        {
            return 0;
        }

        /// <summary>
        /// Returns the Force which is currently fighting. There are currently only two forces. If zero is return no force is fighting.
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("ForceInFight")]
        public int ForceInFight()
        {
            return 0;
        }

        [ExecutableSense("AttackPrepared")]
        public bool AttackPrepared()
        {
            return false;
        }

        [ExecutableSense("DefenderAttackerReadyRatio")]
        public float DefenderAttackerReadyRatio()
        {
            float result = 0.5f;
            
            List<UnitAgent> defender = Interface().forces.ContainsKey(ForceLocations.ArmyTwo) ? Interface().forces[ForceLocations.ArmyTwo]: new List<UnitAgent>();
            List<UnitAgent> attacker = Interface().forces.ContainsKey(ForceLocations.ArmyOne) ? Interface().forces[ForceLocations.ArmyOne]: new List<UnitAgent>();

            result = (defender.Count+1) / (attacker.Count+1);
            return result;
        }

        [ExecutableSense("CombatUnits")]
        public int CombatUnits()
        {
            return Interface().GetAllUnits(false).Count();
        }

        [ExecutableSense("KnowEnemyBase")]
        public bool KnowEnemyBase()
        {
            TilePosition ePos= bwapi.Broodwar.enemy().getStartLocation();
            if (ePos != null && ePos.isValid())
            {
                Interface().baseLocations[(int)ForceLocations.EnemyStart] = ePos;
                return true;
            }

            return false;
        }

        [ExecutableSense("ArmyOneReady")]
        public bool ArmyOneReady()
        {
            if (Interface().forces.ContainsKey(ForceLocations.ArmyOne) && Interface().forces[ForceLocations.ArmyOne].Count > 0)
                return true;

            return false;
        }

        [ExecutableSense("ArmyTwoReady")]
        public bool ArmyTwoReady()
        {
            if (Interface().forces.ContainsKey(ForceLocations.ArmyTwo) && Interface().forces[ForceLocations.ArmyTwo].Count > 0)
                return true;

            return false;
        }


        [ExecutableSense("KnowEnemyNatural")]
        public bool KnowEnemyNatural()
        {
            if (Interface().baseLocations[(int)ForceLocations.EnemyNatural] is TilePosition)
                return true;
            
            TilePosition ePos = bwapi.Broodwar.enemy().getStartLocation();
            if (ePos != null && ePos.isValid())
            {
                TilePosition eNat = bwta.getBaseLocations().Where(loc=> !loc.isStartLocation()).OrderBy(loc=> loc.getPosition().getApproxDistance(new Position(ePos))).First().getTilePosition();
                Interface().baseLocations[(int)ForceLocations.EnemyNatural] = eNat;
                return true;
            }

            // only two starting locations so its easy to determine where the enemy is
            if (bwta.getBaseLocations().Where(loc => loc.isStartLocation() && !loc.getTilePosition().opEquals(Interface().StartLocation())).Count() == 2)
            {
                TilePosition eBase = bwta.getBaseLocations().Where(loc => loc.isStartLocation() && !loc.getTilePosition().opEquals(Interface().baseLocations[(int)ForceLocations.OwnStart])).First().getTilePosition();
                TilePosition eNat = bwta.getBaseLocations().Where(loc => !loc.isStartLocation()).OrderBy(loc => loc.getPosition().getApproxDistance(new Position(ePos))).First().getTilePosition();
                Interface().baseLocations[(int)ForceLocations.EnemyNatural] = eNat;
                return true;
            }

            return false;
        }




    }
}
