﻿#region File Description & Usings
//-----------------------------------------------------------------------------
// Movement.cs
//
// Created by Poplicola
//-----------------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
#endregion

namespace UltimaXNA.GameObjects
{
    public enum Direction : byte
    {
        North = 0x0,
        Right = 0x1,
        East = 0x2,
        Down = 0x3,
        South = 0x4,
        Left = 0x5,
        West = 0x6,
        Up = 0x7,
        Mask = 0x7,
        Running = 0x80,
        ValueMask = 0x87
    }

    public class Movement
    {
        public TileEngine.IWorld World;
        public bool RequiresUpdate = false;

        private TilePosition m_LastTile;
        private TilePosition m_CurrentTile;
        private TilePosition m_NextTile;
        private TilePosition m_GoalTile;
        public Direction Facing = Direction.Up;
        public float MoveSequence;
        public float TimeToCompleteMove;

        public int TileX { get { return (int)m_LastTile.Location.X; } }
        public int TileY { get { return (int)m_LastTile.Location.Y; } }

        private DrawPosition m_DrawPosition;
        public DrawPosition DrawPosition { get { return m_DrawPosition; } }
        private bool m_IsClientPlayer = false;
        private MoveEvent m_MoveEvent;

        private int m_GUID;

        private static TimeSpan m_WalkFoot = TimeSpan.FromSeconds(0.4);
        private static TimeSpan m_RunFoot = TimeSpan.FromSeconds(0.2);
        private static TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
        private static TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);

        public static float WalkFoot { get { return (float)m_WalkFoot.Milliseconds / 1000f; } }
        public static float RunFoot { get { return (float)m_RunFoot.Milliseconds / 1000f; } }
        public static float WalkMount { get { return (float)m_WalkMount.Milliseconds / 1000f; } }
        public static float RunMount { get { return (float)m_RunMount.Milliseconds / 1000f; } }

        public int DrawFacing
        {
            get
            {
                int iFacing = (int)(this.Facing & Direction.Mask);
                if (iFacing >= 3)
                    return iFacing - 3;
                else
                    return iFacing + 5;
            }
        }

        public Movement(int nGUID)
        {
            m_GUID = nGUID;
        }

        public void DesignateClientPlayer()
        {
            m_IsClientPlayer = true;
            m_MoveEvent = new MoveEvent();
        }

        public void NewFacingToServer(Direction nDirection)
        {
            Facing = nDirection;
            m_MoveEvent.NewEvent(this.Facing);
        }

        public bool GetMoveEvent(ref int direction, ref int sequence, ref int fastwalkkey)
        {
            if (m_IsClientPlayer == false)
                return false;
            return m_MoveEvent.GetMoveEvent(ref direction, ref sequence, ref fastwalkkey);
        }

        public void MoveEventAck(int nSequence)
        {
            // do nothing
        }

        public void MoveEventRej(int nSequence, int nX, int nY, int nZ, int nDirection)
        {
            // immediately return to the designated tile.
            this.SetPositionInstant(nX, nY, nZ);
            this.Facing = (Direction)nDirection;
            m_MoveEvent.ResetMoveSequence();
        }

        public bool IsMoving
        {
            get
            {
                if (m_CurrentTile.Location == m_GoalTile.Location)
                    return false;
                return true;
            }
        }

        public void SetGoalTile(float nX, float nY, float nZ)
        {
            m_GoalTile = new TilePosition(nX, nY, nZ);
        }

        public void SetPositionInstant(int nX, int nY, int nZ)
        {
            mFlushDrawObjects();
            m_GoalTile = m_LastTile = m_NextTile = m_CurrentTile = new TilePosition(nX, nY, nZ);
            m_DrawPosition = new DrawPosition(m_CurrentTile);
        }

        public void Update(GameTime gameTime)
        {
            mFlushDrawObjects();
          
            if (this.IsMoving)
            {
                if (m_CurrentTile.Location == m_NextTile.Location)
                {
                    Direction iFacing;
                    m_NextTile = mGetNextTile(m_CurrentTile.Location, m_GoalTile.Location, out iFacing);
                    if (m_NextTile != null)
                    {
                        // If we are the player, set our move event so that the game
                        // knows to send a move request to the server ...
                        if (m_IsClientPlayer)
                        {
                            // Special exception for the player: if we are facing a new direction, we
                            // need to pause for a brief moment and let the server know that.
                            m_MoveEvent.NewEvent(iFacing);
                            if (this.Facing != iFacing)
                            {
                                this.Facing = iFacing;
                                m_NextTile.Location = m_CurrentTile.Location;
                                return;
                            }
                        }
                        else
                        {
                            // if we are not the player, then we can just change the facing.
                            this.Facing = iFacing;
                        }
                        TimeToCompleteMove = WalkFoot;
                        m_LastTile = new TilePosition(m_CurrentTile);
                        MoveSequence = 0f;
                    }
                    else
                    {
                        // They are no longer on our map.
                        this.SetPositionInstant(
                            (int)m_CurrentTile.Location.X,
                            (int)m_CurrentTile.Location.Y,
                            (int)m_CurrentTile.Location.Z);
                        return;
                    }
                }

                MoveSequence += ((1f / 60f) / TimeToCompleteMove);
                if (MoveSequence >= 1f)
                {
                    m_CurrentTile.Location = m_NextTile.Location;
                }
                else
                {
                    m_CurrentTile.Location = m_LastTile.Location + (m_NextTile.Location - m_LastTile.Location) * MoveSequence;
                }
            }

            m_DrawPosition = new DrawPosition(m_CurrentTile);
        }

        private void mFlushDrawObjects()
        {
            if (DrawPosition == null)
                return;
            TileEngine.MapCell iLastMapCell = World.Map.GetMapCell(DrawPosition.TileX, DrawPosition.TileY);
            if (iLastMapCell != null)
                iLastMapCell.FlushObjectsByGUID(m_GUID);
        }

        private TilePosition mGetNextTile(Vector3 nCurrentLocation, Vector3 nGoalLocation, out Direction nFacing)
        {
            Vector3 iDifference = m_CurrentTile.Location;

            if (nGoalLocation.X < nCurrentLocation.X)
            {
                iDifference.X -= 1;
                if (nGoalLocation.Y < nCurrentLocation.Y)
                {
                    iDifference.Y -= 1;
                    nFacing = Direction.Up;
                }
                else if (nGoalLocation.Y > nCurrentLocation.Y)
                {
                    iDifference.Y += 1;
                    nFacing = Direction.Left;
                }
                else
                {
                    nFacing = Direction.West;
                }
            }
            else if (nGoalLocation.X > nCurrentLocation.X)
            {
                iDifference.X += 1;
                if (nGoalLocation.Y < nCurrentLocation.Y)
                {
                    iDifference.Y -= 1;
                    nFacing = Direction.Right;
                }
                else if (nGoalLocation.Y > nCurrentLocation.Y)
                {
                    iDifference.Y += 1;
                    nFacing = Direction.Down;
                }
                else
                {
                    nFacing = Direction.East;
                }
            }
            else
            {
                if (nGoalLocation.Y < nCurrentLocation.Y)
                {
                    iDifference.Y -= 1;
                    nFacing = Direction.North;
                }
                else if (nGoalLocation.Y > nCurrentLocation.Y)
                {
                    iDifference.Y += 1;
                    nFacing = Direction.South;
                }
                else
                {
                    // We should never reach this.
                    nFacing = Direction.Running;
                }
            }
            TileEngine.MapCell iCell = World.Map.GetMapCell(
                (int)iDifference.X, 
                (int)iDifference.Y);
            if (iCell != null)
            {
                int iNextTileAltitude = iCell.GroundTile.Z;
                return new TilePosition((int)iDifference.X, (int)iDifference.Y, iNextTileAltitude);
            }
            else
            {
                return null;
            }
        }
    }

    public class DrawPosition
    {
        public int TileX, TileY, TileZ;
        public float OffsetX, OffsetY, OffsetZ;

        public Vector3 PositionV3
        {
            get
            {
                return new Vector3(TileX, TileY, TileZ);
            }
        }

        public Vector3 OffsetV3
        {
            get
            {
                return new Vector3(OffsetX, OffsetY, OffsetZ);
            }
        }

        public DrawPosition(TilePosition nPosition)
        {
            TileX = (int)Math.Ceiling(nPosition.Location.X);
            TileY = (int)Math.Ceiling(nPosition.Location.Y);
            TileZ = (int)Math.Ceiling(nPosition.Location.Z);
            OffsetX = ((nPosition.Location.X - (float)TileX));
            OffsetY = ((nPosition.Location.Y - (float)TileY));
            OffsetZ = ((nPosition.Location.Z - (float)TileZ));
        }

        public override string ToString()
        {
            return "X:" + TileX.ToString() + " Y:" + TileY.ToString();
        }

        public override bool Equals(object o)
        {
            if (o == null) return false;
            if (o.GetType() != typeof(DrawPosition)) return false;
            if (this.TileX != ((DrawPosition)o).TileX) return false;
            if (this.TileY != ((DrawPosition)o).TileY) return false;
            return true;
        }

        public override int GetHashCode()
        {
           return 0;
        }

    }

    public class TilePosition
    {
        // public int Map = 0;
        internal Vector3 Location;

        public TilePosition(float nTileX, float nTileY, float nTileZ)
        {
            // Map = 0;
            Location = new Vector3(nTileX, nTileY, nTileZ);
        }

        public TilePosition(TilePosition nCopyPosition)
        {
            // Map = nCopyPosition.Map;
            Location = new Vector3(nCopyPosition.Location.X, nCopyPosition.Location.Y, nCopyPosition.Location.Z);
        }
    }

    // This event handles all the move sequences
    public class MoveEvent
    {
        private bool m_Event;
        private int m_Sequence;
        private Direction m_Direction;
        private int m_FastWalkKey;

        public MoveEvent()
        {
            m_Event = false;
            m_Direction = Direction.Up;
            m_FastWalkKey = new Random().Next(int.MinValue, int.MaxValue);
            this.ResetMoveSequence();
        }

        public void ResetMoveSequence()
        {
            m_Sequence = -1;
        }

        public void NewEvent(Direction nDirection)
        {
            m_Event = true;
            m_Sequence++;
            if (m_Sequence > byte.MaxValue)
                m_Sequence = 1;
            m_Direction = nDirection;

            if (m_FastWalkKey == int.MaxValue)
                m_FastWalkKey = new Random().Next(int.MinValue, int.MaxValue);
            else
                m_FastWalkKey++;
            
        }

        public bool GetMoveEvent(ref int direction, ref int sequence, ref int fastwalkkey)
        {
            if (m_Event == false)
            {
                return false;
            }
            else
            {
                m_Event = false;
                sequence = m_Sequence;
                fastwalkkey = m_FastWalkKey;
                direction = (int)m_Direction;
                return true;
            }
        }
    }
}