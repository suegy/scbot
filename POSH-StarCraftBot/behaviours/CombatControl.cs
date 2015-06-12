using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;

namespace POSH_StarCraftBot.behaviours
{
    class CombatControl : AStarCraftBehaviour
    {
        public CombatControl(AgentBase agent)
            : base(agent, new string[] {}, new string[] {})
        {

        }

        //
        // INTERNAL
        //

        //
        // ACTIONS
        //
        [ExecutableAction("RetreatForce")]
        public bool RetreatForce()
        {
            throw new NotImplementedException();
        }

        [ExecutableSense("AttackBase")]
        public bool AttackBase()
        {
            throw new NotImplementedException();
        }
        //
        // SENSES
        //

        [ExecutableSense("BaseUnderAttack")]
        public bool BaseUnderAttack()
        {
            return false;
        }

        [ExecutableSense("ForceIsLosing")]
        public bool ForceIsLosing()
        {
            return false;
        }

        [ExecutableSense("AttackPrepared")]
        public bool AttackPrepared()
        {
            return false;
        }


    }
}
