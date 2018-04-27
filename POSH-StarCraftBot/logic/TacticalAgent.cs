using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using SWIG.BWAPI;

namespace POSHStarCraftBot.logic
{
    /// <summary>
    /// Tactical Assault Agent class...
    /// @author Thomas Willer Sandberg (http://twsandberg.dk/)
    /// @version (1.0, January 2011)
    /// </summary>
    class TacticalAgent
    {
        private ILog log;

        public TacticalAgent(List<UnitAgent> mySquad, ILog log)//ref List<UnitAgent> mySquad)//(List<UnitAgent> mySquad, List<Unit> enemyUnits, int maxDistance)
        {
            this.log = log;
            MySquad = mySquad;
        }

        public void FindBestGoalsForAllUnitsInSquad()//Position position)
        {
            if (MySquad != null && MySquad.Count > 0)
            {
                foreach (UnitAgent unitAgent in MySquad)
                    FindAndSetOptimalGoalForUnitAgent(unitAgent);
                //unitAgent.GoalPosition = SquadMainGoalPosition;
            }
            else
            {
                log.Error("MySquad is null in method FindBestGoalsForAllUnitsInSquad");
                throw new ArgumentNullException("MySquad");
            }
        }

        public void FindAndSetOptimalGoalForUnitAgent(UnitAgent unitAgent)
        {
            if (unitAgent == null)
            {
                log.Error("unitAgent is null in method SetGoalPositionForUnitAgentToClosestEnemy");
                throw new ArgumentNullException("unitAgent");
            }

            Unit enemyUnitToAttack = SCMath.GetClosestEnemyUnit(unitAgent.SCUnit);//unitAgent.GetClosestEnemyUnit();
            if (enemyUnitToAttack != null)
            {
                unitAgent.GoalUnitToAttack = enemyUnitToAttack;
                unitAgent.GoalPosition = enemyUnitToAttack.getPosition();
            }
        }

        /// <summary>
        /// Checks if there are any units near the specified unit.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns>True if there are any units near the specified unit.</returns>
        public bool AnyFriendsNear(Unit unit)
        {
            return MySquad.Any(u => unit.getDistance(u.SCUnit) < 2 && unit.getDistance(u.SCUnit) != 0);
            /*foreach (UnitAgent u in MySquad)
                if (unit.GetDistanceToPosition(u.MyUnit.Position) < 2 && unit.GetDistanceToPosition(u.MyUnit.Position) != 0)
                    return true;
            return false;*/
        }

        /// <summary>
        /// The tactical assault agent will deside which action that would be best for the squad.
        /// For instance find and attack closest enemy units using the squad (team) of own units. 
        /// A tactic could be to attack the units with lowest health first, and using medics to 
        /// heal the most injured melee units first.
        /// </summary>
        public void ExecuteBestActionForSquad()
        {
            foreach (UnitAgent myUnitAgent in MySquad)
            {
                FindBestGoalsForAllUnitsInSquad();
                myUnitAgent.ExecuteBestActionForUnitAgent(MySquad);
            }
        }

        /***********************************************************************
         * All the properties and variables for the TacticalAssaultAgent class *
         ***********************************************************************/
        public List<UnitAgent> MySquad { get; set; }


        /***********************************************************************
         * NOT IMPLEMENTED METHODS                                             *
         ***********************************************************************/
        /// <summary>
        /// Run towards the assault squad, to assist the assault squad in battle.
        /// (Hígh potential field in a radius around the assault squad, and lower PF the farther the support team are from the squad.)
        /// Quote from StarCraft: "Warriors has engaged the enemy".
        /// </summary>
        /// <param name="supportSquad">To assist the assault squad (Can be one to many.)</param>
        public void SupportExistingSquad(List<Unit> supportSquad)
        {
            throw new NotImplementedException();
            //TODO: Run towards the assault squad, to assist the assault squad in battle.
            //Hígh potential field in a radius around the assault squad, and lower PF the farther the support team are from the squad.
        }
    }
}
