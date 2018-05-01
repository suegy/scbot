using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.logic
{
    class UnitEqualityComparer : IEqualityComparer<Unit>
    {
        public bool Equals(Unit x, Unit y)
        {
            return (x is Unit && y is Unit && x.getID() == y.getID()) ? true : false;
        }

        public int GetHashCode(Unit obj)
        {
            return obj.getID();
        }
    }
}
