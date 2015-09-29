using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.logic
{
    /// <summary>
    /// Walk Tiles - each walk tile is an 8x8 square of pixels. These are called walk tiles because walkability data is available at this resolution.
    /// ***(Walk Tiles - each build tile is a 4x4 square of pixels. WRONG)***
    /// </summary>
    class WalkTile
    {
        public int X { get; set; }
        public int Y { get; set; }

        public WalkTile(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public WalkTile(Position position)
        {
            this.X = GetWalkTileCoordinate(position.xConst());
            this.Y = GetWalkTileCoordinate(position.yConst());
        }

        protected int GetWalkTileCoordinate(int positionCoordinate)
        {
            return Convert.ToInt32(positionCoordinate * 1.0 / 8.0); // divide by 4 WRONG
        }

        public static int Width { get { return 8; } }// return 4 WRONG
        public static int Height { get { return 8; } }// return 4 WRONG





    }
}
