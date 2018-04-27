using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace POSHStarCraftBot.logic
{
    /// <summary>
    /// 
    /// Modified version of MYMath from:
    /// @author (Thomas Willer Sandberg) 
    /// @version (1.0, January 2011)
    /// </summary>
    sealed class SCMath
    {
        /// <summary>
        /// Calculates the hypothenuse of a right-angled triangle using Pythagorean theorem.
        /// </summary>
        /// <param name="a">cathetus a</param>
        /// <param name="b">cathetus b</param>
        /// <returns>return the length of the hypothenuse (c).</returns>
        public static double Pythagoras(double a, double b)
        {
            return System.Math.Sqrt(a * a + b * b);
        }

        /// <summary>
        /// Calculates the hypothenuse of a right-angled triangle using Pythagorean theorem.
        /// </summary>
        /// <param name="a">cathetus a</param>
        /// <param name="b">cathetus b</param>
        /// <returns>return the length of the hypothenuse (c) ceiled to the closest integer.</returns>
        public static int PythagorasInt(float a, float b)
        {
            return (int)System.Math.Ceiling(System.Math.Sqrt(a * a + b * b));
        }

        /// <summary>
        ///  Returns true if the specified Position in pixels exists (not outside the map).
        /// </summary>
        /// <param name="realPosition"></param>
        /// <returns></returns>
        public static bool PositionRealExist(Position realPosition)
        {
            if (realPosition.xConst() > 0 && realPosition.yConst() > 0 && realPosition.xConst() < bwapi.Broodwar.mapWidth() * tileSize && realPosition.yConst() < bwapi.Broodwar.mapHeight() * tileSize)
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the specified Position in tiles exists (not outside the map). Tilesize is 32.
        /// </summary>
        /// <param name="tilePosition"></param>
        /// <returns></returns>
        public static Boolean PositionTileExist(TilePosition tilePosition)
        {
            if (tilePosition.xConst() > 0 && tilePosition.yConst() > 0 && tilePosition.xConst() < bwapi.Broodwar.mapWidth() * tileSize && tilePosition.yConst() < bwapi.Broodwar.mapHeight() * tileSize)
                return true;
            return false;
        }

        /// <summary>
        /// Converts a real position (in pixels) to a WalkTile Position (8 pixels wide and 8 pixels high).
        /// </summary>
        /// <param name="realPos"></param>
        /// <returns></returns>
        public Position ConvertRealPosToWalkTilePos(Position realPos)
        {
            return new Position(realPos.xConst() / 8, realPos.yConst() / 8);
        }

        /// <summary>
        /// *****************VIRKER****************
        /// Checks if a WalkTile is walkable. WalkTile is 8*8 pixels. The inputs should be in real coordinates. The Method will automatically convert them to WalkTiles and check if this is walkable.
        /// </summary>
        /// <param name="realX"></param>
        /// <param name="realY"></param>
        /// <returns></returns>
        public static bool IsRealWalkable(int realX, int realY)
        {
            if (SWIG.BWAPI.bwapi.Broodwar.isWalkable((int)Math.Round((double)(realX / walkTileSize), MidpointRounding.AwayFromZero), (int)System.Math.Round((double)(realY / walkTileSize), MidpointRounding.AwayFromZero)))
                return true;
            return false;
        }

        /// <summary>
        /// Add a list to another list.
        /// </summary>
        /// <param name="surroundingPositions1"></param>
        /// <param name="surroundingPositions2"></param>
        /// <returns></returns>
        public static List<Position> AddPosListToPosList(List<Position> surroundingPositions1, List<Position> surroundingPositions2)
        {
            List<Position> positions = surroundingPositions1;
            foreach (Position p in surroundingPositions2)
            {
                if (!surroundingPositions1.Contains(p))//Don't add duplicates.
                    positions.Add(p);
            }

            return positions; //.Distinct().ToList(); //Remove Duplicates.
        }

        /// <summary>
        /// Get the unit size.
        /// Inspiration from BTHAI StarCraft bot ver. 1.00: http://code.google.com/p/bthai/
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetUnitSize(UnitType type)
        {
            if (type.isWorker())
                return 6;
            if (type.size().getName() == "Small")
                return 6;
            if (type.size().getName() == "Medium")
                return 12;
            if (type.size().getName() == "Large")
                return 16;
            return 12;
        }

        /// <summary>
        /// Calculates a linear repulsive magnitude starting from a large negative values and go towards zero.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="forceStep"></param>
        /// <param name="distance"></param>
        /// <param name="canBePositive"></param>
        /// <returns>The closer the distance (d) is to 0 the greater the negative magnitude (m) will be. 
        /// (Example using these inputs: = force=200, forceStep=-1,2 -> d=0 m=-200, d=1 m=-198,8, d=2 m=-197,6, d=3 m=-196,4)</returns>
        public static double CalculateRepulsiveMagnitude(double force, double forceStep, double distance, bool canBePositive)
        {
            if (!canBePositive)
            {
                var magnitude = -(force - forceStep * distance);
                return magnitude < 0 ? magnitude : 0;
            }
            return -(force - forceStep * distance);
        }

        /// <summary>
        /// Calculates a linear repulsive magnitude starting from zero and go towards +infinite.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static double CalculateRepulsiveMagnitudePositive(double force, double distance)
        {
            return distance * force;
        }
        //public static double CalculateRepulsiveMagnitudePositive(double force, double forceStep, double distance)
        //{
        //    return distance * force / forceStep;
        //}

        /// <summary>
        /// Calculates a linear attractive magnitude.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="forceStep"></param>
        /// <param name="distance"></param>
        /// <param name="canBeNegative"></param>
        /// <returns>The closer the distance is to 0 the greater the magnitude will be.</returns>
        public static double CalculateAttractiveMagnitude(double force, double forceStep, double distance, Boolean canBeNegative)
        {
            if (!canBeNegative)
            {
                var magnitude = force - forceStep * distance;
                return magnitude > 0 ? magnitude : 0;
            }
            return force - forceStep * distance;
        }

        /// <summary>
        /// Calculates a linear attractive magnitude starting from zero and go towards -infinite.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static double CalculateAttractiveMagnitudeNegative(double force, double distance)
        {
            return -(distance * force);
        }

        /// <summary>
        /// Used for Potential Fields
        /// Calculates if a two of my own units is standing too close to each other.
        /// Best PF value is 0 (No risk for collision with own units). Lower values if there is risk of splash damage or collision with other units.
        /// There is a special case for cloaked ground units: They should avoid getting to close to none cloaked units (to avoid splash damage).
        /// Inspiration from BTHAI StarCraft bot ver. 1.00 method calcOwnUnitP: http://code.google.com/p/bthai/
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="field"></param>
        /// <param name="myOtherUnit"></param>
        /// <param name="force"></param>
        /// <param name="forceStep">Should always be lower than punishment</param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static double CalculatePFOwnUnitRepulsion(Unit unit, Position field, Unit myOtherUnit, double force, double forceStep, int range)
        {
            double distance = myOtherUnit.getDistance(field);
            if (unit.getID() == myOtherUnit.getID() || unit.getType().isFlyer() || myOtherUnit.getType().isFlyer() || unit.getType().isFlyingBuilding() || myOtherUnit.getType().isFlyingBuilding())//If both units has the same ID, there can't be a collision, so don't calculate anything.
                return 0;

            if (myOtherUnit.getType().getID() == bwapi.UnitTypes_Terran_Vulture_Spider_Mine.getID())
                if (distance <= 125 + GetUnitSize(unit.getType()))//Avoid Spider Mines.
                    return CalculateRepulsiveMagnitude(force, forceStep, distance, false);//-20;

            if (unit.isCloaked() && !myOtherUnit.isCloaked()) //Cloak units should avoid standing to close to other non cloaked units to avoid splash damage from tanks for instance.
                if (distance <= 50 + GetUnitSize(unit.getType()))
                    return CalculateRepulsiveMagnitude(force, forceStep, distance, false);//-20;

            //OPTIMIZE BY EVOLUTIONARY COMPUTATION
            return distance <= GetUnitSize(unit.getType()) + range ? CalculateRepulsiveMagnitude(force, forceStep, distance, false) : 0;
        }

        /// <summary>
        /// Calculates the potential field repulsion value from a neutral unit.
        /// Flying neutral units will be ignored and return 0.
        /// All other neutral units will use this formula: -(force - forceStep * distance).
        /// Inspiration from: BTHAI StarCraft bot ver. 1.00 calcMineP method.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="neutralUnit"></param>
        /// <param name="force"></param>
        /// <param name="forceStep"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static double CalculatePFNeutralUnitRepulsion(Unit neutralUnit, Position field, double force, double forceStep, int range)
        {
            double distance = neutralUnit.getDistance(field);
            if (neutralUnit.getType().isFlyer())
                return 0;
            if (neutralUnit.getType().isFlyingBuilding())
                return 0;
            return distance <= GetUnitSize(neutralUnit.getType()) + range ? CalculateRepulsiveMagnitude(force, forceStep, distance, false) : 0;
        }

        public static int GetRange(Unit unit)
        {
            return Math.Max(unit.getType().groundWeapon().maxRange(), unit.getType().airWeapon().maxRange());
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="field"></param>
        /// <param name="enemyUnit"></param>
        /// <param name="force"></param>
        /// <param name="forceStep"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static double CalculatePFEnemyUnitRepulsion(Unit unit, Position field, Unit enemyUnit, double force, double forceStep)
        {
            if (unit.getID() == enemyUnit.getID())//If both units has the same ID, there can't be a collision, so don't calculate anything.
                return 0;

            double distance = enemyUnit.getDistance(field);

            int myUnitMSD = unit.getType().groundWeapon().maxRange();
            int enemyUnitMSD = enemyUnit.getType().groundWeapon().maxRange();
            int MSDDifference = myUnitMSD - enemyUnitMSD;

            if (MSDDifference > 0)
            {
                if (distance <= enemyUnitMSD)
                    return CalculateRepulsiveMagnitude(force, forceStep, distance, false);//-20;
            }
            else if (distance <= GetUnitSize(unit.getType()))
                return -force;
            return 0;
        }

        /// <summary>
        /// Calculates the repulsion factor/magnitude of the specified field relative to the edges of the map.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="force"></param>545455555555555555555555555555555111111111
        /// <param name="forceStep"></param>
        /// <param name="range">How close units are allowed to get to the edge of the map, before they are repulsed</param>
        /// <returns></returns>
        public static double CalculateMapEdgeRepulsion(Position field, double force, double forceStep, int range)
        {
            double magnitude = 0;
            if (field.xConst() <= range) //Distance to wall left.
                magnitude += -(force - forceStep * field.getDistance(new Position(0, field.yConst())));

            if (field.yConst() <= range) //Distance to wall top.
                magnitude += -(force - forceStep * field.getDistance(new Position(field.xConst(), 0)));

            if (field.xConst() >= bwapi.Broodwar.mapWidth() * tileSize - range) //Distance to wall right.
                magnitude += -(force - forceStep * field.getDistance(new Position(bwapi.Broodwar.mapWidth() * tileSize, field.yConst())));

            if (field.yConst() >= bwapi.Broodwar.mapHeight() * tileSize - range) //Distance to wall bottom.
                magnitude += -(force - forceStep * field.getDistance(new Position(field.xConst(), bwapi.Broodwar.mapHeight() * tileSize)));

            return magnitude < 0 ? magnitude : 0;
        }



        /// <summary>
        /// Is this real position (in pixels) on the map walkable (will automatically be converted to walktiles)
        /// </summary>
        /// <param name="position">The position in real pixels not divided by 8 like Walktiles (the method automatically converts the position to walktile)</param>
        /// <returns>True if the walktile (the specified position is in) is walkable</returns>
        public static bool IsRealWalkablePythagoras(Position position)
        {
            foreach (Position pos in GetAllSurroundingPositionsPythagoras(position.xConst(), position.yConst(), walkTileSize, true))
                if (!SWIG.BWAPI.bwapi.Broodwar.isWalkable(pos.xConst() / 8, pos.yConst() / 8) || !PositionRealExist(position)) //!Game.IsWalkable(wt))
                    return false;
            return true;
        }

        /// <summary>
        /// DOESN'T seem to work at the moment. Looks at BuildTiles instead of walktiles.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static bool IsSameGroundHigh(Position current, Position next)
        {
            if (SWIG.BWAPI.bwapi.Broodwar.getGroundHeight(current.xConst(), current.yConst()) == bwapi.Broodwar.getGroundHeight(next.xConst(), next.yConst()))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Get all the surrounding positions (using box rotation) that is walkable.
        /// </summary>
        /// <param name="currentX">Midpoint x coordinate</param>
        /// <param name="currentY">Midpoint y coordinate</param>
        /// <param name="myTileSize">The size of the tiles, in other words the distance from the current position (centrum)
        /// to the surrounding positions where you could check for collision and Potential Field Values for instance.</param>
        /// <param name="newAngle">The angle for how much the positions should be rotated. If 0 the positions will
        /// represent that they are placed like 3*3 tiles and not tilted.</param>
        /// <returns>All surrounding positions (rotated as specified in the newAngle parameter)</returns>
        public static List<Position> GetPossibleSurroundingPositionsRotation(int currentX, int currentY, int myTileSize, int newAngle)
        {
            var tiles = new List<Position>();
            double length = Pythagoras(myTileSize, myTileSize);
            int angle = newAngle;

            for (int i = 0; i < 8; i++)
            {
                if (i % 2 == 0)
                {
                    Position p = GetRotatedPosition(currentX, currentY, angle, myTileSize);
                    if (IsRealWalkablePythagoras(p)) //&& IsSameGroundHigh(new Position(currentX, currentY), p)) //With IsSameGroundHigh it will not be possible to go to a lower or higher platform. But without this check the unit could get stucked.
                        tiles.Add(p);
                }
                else
                {
                    Position p = GetRotatedPosition(currentX, currentY, angle, length);
                    if (IsRealWalkablePythagoras(p)) //&& IsSameGroundHigh(new Position(currentX, currentY), p))
                        tiles.Add(p);
                }

                if (angle + 45 > 360)
                    angle = (360 - (angle + 45)) * -1;
                else
                    angle += 45;
            }
            return tiles;
        }

        /// <summary>
        /// Get all the surrounding positions and tilt/rotate the environment/positions as you like.
        /// </summary>
        /// <param name="currentX">Midpoint x coordinate</param>
        /// <param name="currentY">Midpoint y coordinate</param>
        /// <param name="myTileSize">The size of the tiles, in other words the distance from the current position (centrum)
        /// to the surrounding positions where you could check for collision and Potential Field Values for instance.</param>
        /// <param name="newAngle">The angle for how much the positions should be rotated. If 0 the positions will
        /// represent that they are placed like 3*3 tiles and not tilted.</param>
        /// <returns>All surrounding positions (rotated as specified in the newAngle parameter)</returns>
        public static List<Position> GetAllSurroundingPositionsRotation(int currentX, int currentY, int myTileSize, int newAngle)
        {
            var tiles = new List<Position>();
            double length = Pythagoras(myTileSize, myTileSize);
            int angle = newAngle;

            for (int i = 0; i < 8; i++)
            {
                if (i % 2 == 0)
                    tiles.Add(GetRotatedPosition(currentX, currentY, angle, myTileSize));
                else
                    tiles.Add(GetRotatedPosition(currentX, currentY, angle, length));

                if (angle + 45 > 360)
                    angle = (360 - (angle + 45)) * -1;
                else
                    angle += 45;
            }
            return tiles;
        }

        /// <summary>
        /// Get a position placed in the specified angle and length (distance to the currentXY)</returns>
        /// </summary>
        /// <param name="currentX">Midpoint x coordinate</param>
        /// <param name="currentY">Midpoint y coordinate</param>
        /// <param name="angle">The angle for how much the positions should be rotated. If 0 the positions will
        /// represent that they are placed like 3*3 tiles and not tilted.</param>
        /// <param name="length">Represents the distance between the two corners of a tile 
        /// (or the distance between the centrum of one tile to the centrum of the next tile)</param>
        /// <returns>A position placed in the specified angle and length (distance to the currentXY)</returns>
        public static Position GetRotatedPosition(int currentX, int currentY, int angle, double length)
        {
            double angleRadian = System.Math.PI * angle / 180.0; //This calculation is necessary because the math package in .NET calculates angles in radians.
            //x = length * cos(angle in radians) + X1 . + X1 necessary because the Position is not necessary in origo (0.0).
            int newX = (int)System.Math.Round(length * System.Math.Cos(angleRadian) + currentX, MidpointRounding.AwayFromZero);
            //y = length * sin(angle in radians) + Y1 . + Y1 necessary because the Position is not necessary in origo (0.0).
            int newY = (int)System.Math.Round(length * System.Math.Sin(angleRadian) + currentY, MidpointRounding.AwayFromZero);
            return new Position(newX, newY);
        }

        /// <summary>
        /// Get all the surrounding positions in a tilted environment.
        /// </summary>
        /// <param name="currentX">the current x position</param>
        /// <param name="currentY">the current y position</param>
        /// <param name="myTileSize">the tile size you want to work with. In other word how far away is the positions you want to find.</param>
        /// <param name="centerTileIncluded">If true the specified current position will be included in the returned result, else if false, 
        /// only the surrounding positions will be returned.</param>
        /// <returns>return all the surrounding positions.</returns>
        public static List<Position> GetAllSurroundingPositionsPythagoras(int currentX, int currentY, int myTileSize, Boolean centerTileIncluded)
        {
            var tiles = new List<Position>();
            double lengthCenterToCenter = Pythagoras(myTileSize, myTileSize);
            double lengthCenterToEdge = lengthCenterToCenter / 2;

            double minPosX = currentX - lengthCenterToCenter;
            double minPosY = currentY - lengthCenterToCenter;
            double maxPosX = currentX;
            double maxPosY = currentY;
            double xPos = minPosX;
            double yPos = maxPosY;
            int column = 1;

            for (int i = 1; i <= 9; i++)
            {
                if (centerTileIncluded || (!(System.Math.Round(xPos, MidpointRounding.AwayFromZero) == currentX && System.Math.Round(yPos, MidpointRounding.AwayFromZero) == currentY)))
                    tiles.Add(new Position((int)System.Math.Round(xPos, MidpointRounding.AwayFromZero), (int)System.Math.Round(yPos, MidpointRounding.AwayFromZero)));

                if (System.Math.Round(yPos, MidpointRounding.AwayFromZero) == System.Math.Round(minPosY, MidpointRounding.AwayFromZero))
                {
                    minPosY += lengthCenterToEdge;
                    xPos -= lengthCenterToEdge;
                    yPos = currentY + (column * lengthCenterToEdge);
                    column++;
                }
                else
                {
                    xPos += lengthCenterToEdge;
                    yPos -= lengthCenterToEdge;
                }
            }
            return tiles;
        }

        /// <summary>
        /// Get all surrounding walktiles. (THE METHOD SHOULD BE UPDATED AND HAVE THE SAME INPUTS AS GetAllSurroundingPositionsPythagoras)
        /// </summary>
        /// <param name="currentX"></param>
        /// <param name="currentY"></param>
        /// <returns></returns>
        public static List<WalkTile> GetAllSurroundingWalktilesPythagoras(int currentX, int currentY)
        {
            var tiles = new List<WalkTile>();
            double lengthCenterToCenter = Pythagoras(walkTileSize, walkTileSize);
            double lengthCenterToEdge = lengthCenterToCenter / 2;

            double minPosX = currentX - lengthCenterToCenter;
            double minPosY = currentY - lengthCenterToCenter;
            double maxPosX = currentX;
            double maxPosY = currentY;
            double xPos = minPosX;
            double yPos = maxPosY;
            int column = 1;

            for (int i = 1; i <= 9; i++)
            {
                //if (!(xPos == currentX && yPos == currentY))
                //{
                tiles.Add(new WalkTile((int)System.Math.Round((xPos / walkTileSize), MidpointRounding.AwayFromZero), (int)System.Math.Round((yPos / walkTileSize), MidpointRounding.AwayFromZero)));
                //}

                if (yPos == minPosY)
                {
                    minPosY += lengthCenterToEdge;
                    xPos -= lengthCenterToEdge;
                    yPos = currentY + (column * lengthCenterToEdge);
                    column++;
                }
                else
                {
                    xPos += lengthCenterToEdge;
                    yPos -= lengthCenterToEdge;
                }
            }
            return tiles;
        }


        /// <summary>
        /// Check for if the units hitpoints or shield is reduced.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static Boolean IsInjured(Unit unit)
        {
            if (unit.getHitPoints() < unit.getType().maxHitPoints() || unit.getShields() < unit.getType().maxShields())
                return true;
            return false;
        }

        /// <summary>
        /// Check how much hitpoints and shield the unit has left.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static double UnitHitpointsLeftInPercentage(Unit unit)
        {
            if (unit != null)
            {
                if (!IsInjured(unit))
                    return 100;
                return (unit.getHitPoints() + unit.getShields()) / (unit.getType().maxHitPoints() + unit.getType().maxShields());
            }
            throw new ArgumentNullException("Unit is null in UnitHitpointsLeftInPercentage");
        }


        public static Boolean IsAGoodLeaderCandidate(Unit unit)
        {
            if (unit.getType().isWorker()  || unit.getType().getID() == bwapi.UnitTypes_Terran_Medic.getID() || 
                unit.getType().getID() == bwapi.UnitTypes_Terran_Siege_Tank_Siege_Mode.getID() || 
                unit.getType().getID() == bwapi.UnitTypes_Terran_Siege_Tank_Tank_Mode.getID() ||
                unit.getType().getID() == bwapi.UnitTypes_Zerg_Overlord.getID())
                return false;
            return true;
        }

        ///// <summary>
        ///// Find and set the best leader candidate (most health/armor and not Tank, SCV or Medic).
        ///// ONLY WORKS FOR TERRAN.
        ///// </summary>
        ///// <returns></returns>
        //public static UnitAgent FindBestLeaderCandidate(ref List<UnitAgent> squad)
        //{
        //    int bestHealth = 0;
        //    int currentHealth = -1;
        //    UnitAgent bestUnitAgent = null;
        //    if (squad != null && squad.Count > 0)
        //    {
        //        bestUnitAgent = squad[0];

        //        foreach (UnitAgent unitagent in squad)
        //        {
        //            if (IsAGoodLeaderCandidate(unitagent.MyUnit))
        //            {
        //                currentHealth = unitagent.MyUnit.HitPoints + unitagent.MyUnit.Shields;
        //                if (currentHealth > bestHealth)
        //                {
        //                    bestHealth = currentHealth;
        //                    bestUnitAgent = unitagent;
        //                }
        //            }
        //        }
        //        bestUnitAgent.LeadingStatus = UnitAgent.LeadingStatusEnum.GroupLeader;
        //    }
        //    else
        //    {
        //        Console.WriteLine("Parameter squad null in FindBestLeaderCandidate in SCMath.");
        //        // Logger.Logger.AddAndPrint("Parameter squad null in FindBestLeaderCandidate in MyMath.");
        //    }

        //    return bestUnitAgent;
        //}

        /// <summary>
        /// Find the enemy unit that has lost most hitpoints and shield in percentage 
        /// in a user specified radius from the specified center position.
        /// </summary>
        /// <param name="position">center position</param>
        /// <param name="radius">the radius that should be scanned for injured or damaged enemy units.</param>
        /// <returns></returns>
        public static Unit GetEnemyUnitWithLowestHitpoints(Position position, int radius)//radius = 5
        {
            Unit mostInjuredEnemyUnit = null;

            double lowestHealth = 1;

            if (SWIG.BWAPI.bwapi.Broodwar.enemy().getUnits() != null)//Game.PlayerEnemy.GetUnits() != null)
            {
                foreach (Unit enemyUnit in bwapi.Broodwar.enemy().getUnits())
                {
                    if (enemyUnit.isVisible() && enemyUnit.getPosition().getDistance(position) < radius * TileSize)//Only look at possible injured units in a radius of 5 tiles.
                    {
                        double currentEnemyUnitHealth = SCMath.UnitHitpointsLeftInPercentage(enemyUnit);
                        if (currentEnemyUnitHealth < lowestHealth)
                        {
                            lowestHealth = currentEnemyUnitHealth;
                            mostInjuredEnemyUnit = enemyUnit;
                        }
                    }
                }
            }
            else
            {
                //Logger.Logger.AddAndPrint("ERROR in GetEnemyUnitWithLowestHitpoints, the enemyUnits list is NULL");
                //Util.Logger.Instance.Log("ERROR in GetEnemyUnitWithLowestHitpoints, the enemyUnits list is NULL"););
            }
            return mostInjuredEnemyUnit;
        }





        public static List<Unit> GetAllMeleeUnitsInArea(Position position, int radius)
        {
            var ownUnitsInArea = new List<Unit>();
            if (bwapi.Broodwar.self().getUnits() != null)
            {
                foreach (Unit ownUnit in bwapi.Broodwar.self().getUnits())
                {
                    if (ownUnit.getPosition().getDistance(position) < radius * TileSize)//Only look at units in the specified radius in build tiles.
                        ownUnitsInArea.Add(ownUnit);
                }
            }
            else
            {
                //Logger.Logger.AddAndPrint("ERROR in GetAllMeleeUnitsInArea, the PlayerSelf.GetUnits() list is NULL");
                //Util.Logger.Instance.Log("ERROR in GetAllMeleeUnitsInArea, the PlayerSelf.GetUnits() list is NULL");
            }
            return ownUnitsInArea;
        }

        /// <summary>
        /// Checks if a specific unit list contains any military units.
        /// </summary>
        /// <param name="units"></param>
        /// <returns></returns>
        public static Boolean AnyMilitaryUnitsLeft(IEnumerable<Unit> units)
        {
            foreach (var unit in units)
            {
                if (!unit.getType().isWorker())//The unit is a military unit, return true
                    return true;
                if (unit.isRepairing())//If the unit is a worker and is repairing it will be considered to be a military unit, then return true
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the enemy unit who is closest to my current unit.
        /// </summary>
        /// <param name="myUnit"></param>
        /// <returns>Unit closestEnemyUnit</returns>
        public static Unit GetClosestEnemyUnit(Unit myUnit)
        {
            Unit closestEnemyUnit = null;
            double shortestDistance = GetMaxDistance() * tileSize; //getMaxDistance() * tileSize;

            if (SWIG.BWAPI.bwapi.Broodwar.enemy().getUnits() != null && SWIG.BWAPI.bwapi.Broodwar.enemy().getUnits().Count > 0)//Game.PlayerEnemy.GetUnits() != null)
            {
                Boolean militaryUnitsLeft = false;
                if (AnyMilitaryUnitsLeft(bwapi.Broodwar.enemy().getUnits()))
                    militaryUnitsLeft = true;

                foreach (Unit enemyUnit in bwapi.Broodwar.enemy().getUnits())
                {
                    if ((militaryUnitsLeft && !enemyUnit.getType().isWorker()) || !militaryUnitsLeft)//Only attack workers if they are not repairing or if no military units are left.
                    {
                        double currentDistance = myUnit.getDistance(enemyUnit);
                        if (currentDistance < shortestDistance)
                        {
                            shortestDistance = currentDistance;
                            closestEnemyUnit = enemyUnit;
                        }
                    }
                }
            }
            else
            {
                //Logger.Logger.AddAndPrint("ERROR in GetClosestEnemy, the enemyUnits list is NULL");
            }
                

            return closestEnemyUnit;
        }

        /// <summary>
        /// Checks if there are any enemy units in sight.
        /// Only works if Complete Map Information is disabled, else it will always return true.
        /// </summary>
        /// <returns>True if there are enemy units in sight.</returns>
        public Boolean IsEnemyInSight()
        {
            return (SWIG.BWAPI.bwapi.Broodwar.enemy().getUnits().Count > 0);//Game.PlayerEnemy.GetUnits().Count > 0);
        }

        /// <summary>
        /// Calculates the maximum distance from corner to corner in the current map (In tiles).
        /// </summary>
        /// <returns></returns>
        private static int CalculateMaxDistance()
        {
            _maxDistance = SCMath.PythagorasInt(bwapi.Broodwar.mapHeight(), bwapi.Broodwar.mapWidth());//Max Potential Field Value
            return _maxDistance;
        }

        /// <summary>
        /// Get the maximum distance from corner to corner in the current map (In tiles).
        /// If _maxDistance is already calculated, then just return it. Else if it's first time, 
        /// then calculated maxDistance and set _maxDistance equal to it.
        /// </summary>
        /// <returns></returns>
        public static int GetMaxDistance()
        {
            return _maxDistance > 0 ? _maxDistance : CalculateMaxDistance();
        }

        /// <summary>
        /// Calculates the specified percentage of the max distance (in pixels).
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public static int GetPercentageOfMaxDistancePixels(int percentage)
        {
            return GetMaxDistance() * TileSize * percentage / 100;
        }

        private static Position CalculateMapCenterPosition()
        {
            _mapCenter = new Position(bwapi.Broodwar.mapHeight() * TileSize / 2, bwapi.Broodwar.mapWidth() * TileSize / 2);
            return _mapCenter;
        }

        /// <summary>
        /// Get the center of the map position.
        /// </summary>
        /// <returns></returns>
        public static Position GetMapCenterPosition()
        {
            // _mapCenter = _mapCenter, unless _mapCenter is null, in which case _mapCenter = CalculateMapCenterPosition().
            return _mapCenter ?? CalculateMapCenterPosition();
        }

        /// <summary>
        /// Calculates the specified percentage of the max distance (in tiles).
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public static int GetPercentageOfMaxDistanceTileSize(int percentage)
        {
            return GetMaxDistance() * percentage / 100;
        }

        /************************************************************
         * All the properties and variables for static MyMath class *
         ************************************************************/
        private static int _maxDistance = -1;
        private static Position _mapCenter;
        private const int tileSize = 32;
        private const int walkTileSize = 8;

        public static int WalkTileSize { get { return walkTileSize; } }
        public static int TileSize { get { return tileSize; } }
    }
}
