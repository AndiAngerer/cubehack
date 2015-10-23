﻿// Copyright (c) the CubeHack authors. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the project root.

using CubeHack.Data;
using CubeHack.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CubeHack.Game
{
    public sealed class GameClient : IDisposable
    {
        public PositionData PositionData = new PositionData();
        public PhysicsValues PhysicsValues = new PhysicsValues();
        public List<PositionData> EntityPositions = new List<PositionData>();
        private readonly PriorityMutex _mutex = new PriorityMutex();
        private readonly IChannel _channel;

        private readonly double _inverseSqrt2 = Math.Sqrt(0.5);

        private readonly IGameController _controller;
        private GameDuration _miningTime = GameDuration.Zero;
        private GameDuration _placementCooldown = GameDuration.Zero;

        private GameTime _frameTime;

        private List<CubeUpdateData> _cubeUpdates = new List<CubeUpdateData>();
        private int _playerEventQueued;

        public GameClient(IGameController controller, IChannel channel)
        {
            World = new World(null);

            _controller = controller;

            _channel = channel;
            channel.OnGameEventAsync = HandleGameEventAsync;
            QueueSendingPlayerEvent();
        }

        public World World { get; private set; }

        public RayCastResult HighlightedCube { get; private set; }

        public IDisposable TakeRenderLock()
        {
            var unlocker = _mutex.TakePriorityLock();
            return unlocker;
        }

        public void MouseLook(float dx, float dy)
        {
            PositionData.HAngle -= 0.1f * dx;
            if (PositionData.HAngle > 180)
            {
                PositionData.HAngle -= 360;
            }
            if (PositionData.HAngle < -180)
            {
                PositionData.HAngle += 360;
            }

            PositionData.VAngle += 0.1f * dy;
            if (PositionData.VAngle > 90)
            {
                PositionData.VAngle = 90;
            }
            if (PositionData.VAngle < -90)
            {
                PositionData.VAngle = -90;
            }
        }

        public void UpdateState()
        {
            var elapsedDuration = GameTime.Update(ref _frameTime);
            MovePlayer(elapsedDuration);

            foreach (var entityPosition in EntityPositions)
            {
                Movement.MoveEntity(PhysicsValues, World, entityPosition, elapsedDuration, entityPosition.Velocity.X, entityPosition.Velocity.Y, entityPosition.Velocity.Z);
            }

            UpdateBuildAction(elapsedDuration);

            QueueSendingPlayerEvent();
        }

        public void Dispose()
        {
            _channel.Dispose();
        }

        private void QueueSendingPlayerEvent()
        {
            /* Only queue sending of the player event if we haven't already queued a send operation
             * to avoid overflowing our buffers with stale player events. We do this check lock-free. */
            if (Interlocked.Exchange(ref _playerEventQueued, 1) == 0)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        PlayerEvent playerEvent;
                        using (_mutex.TakeLock())
                        {
                            playerEvent = CreatePlayerEvent();
                        }

                        await _channel.SendPlayerEventAsync(playerEvent);
                    }
                    finally
                    {
                        /* Remove the "queued" flag. */
                        Volatile.Write(ref _playerEventQueued, 0);
                    }
                });
            }
        }

        private Task HandleGameEventAsync(GameEvent gameEvent)
        {
            using (_mutex.TakeLock())
            {
                if (gameEvent.EntityPositions != null)
                {
                    EntityPositions = gameEvent.EntityPositions;
                }

                if (gameEvent.PhysicsValues != null)
                {
                    PhysicsValues = gameEvent.PhysicsValues;
                }

                if (gameEvent.ChunkDataList != null)
                {
                    foreach (var chunkData in gameEvent.ChunkDataList)
                    {
                        World.PasteChunkData(chunkData);
                    }
                }

                if (gameEvent.CubeUpdates != null)
                {
                    foreach (var chunkUpdate in gameEvent.CubeUpdates)
                    {
                        World[chunkUpdate.X, chunkUpdate.Y, chunkUpdate.Z] = chunkUpdate.Material;
                    }
                }
            }

            QueueSendingPlayerEvent();

            return Task.CompletedTask;
        }

        private PlayerEvent CreatePlayerEvent()
        {
            var playerEvent = new PlayerEvent();

            playerEvent.PositionData = new PositionData
            {
                Position = PositionData.Position,
                CollisionPosition = PositionData.CollisionPosition,
                Velocity = PositionData.Velocity,
                HAngle = PositionData.HAngle,
                VAngle = PositionData.VAngle,
                IsFalling = PositionData.IsFalling,
            };

            if (_cubeUpdates.Count > 0)
            {
                playerEvent.CubeUpdates = _cubeUpdates;
                _cubeUpdates = new List<CubeUpdateData>();
            }
            return playerEvent;
        }

        private void UpdateBuildAction(GameDuration elapsedDuration)
        {
            if (_placementCooldown > GameDuration.Zero)
            {
                _placementCooldown -= elapsedDuration;
            }

            double f = Math.PI / 180.0;
            double lookZ = -Math.Cos(PositionData.HAngle * f) * Math.Cos(-PositionData.VAngle * f);
            double lookX = -Math.Sin(PositionData.HAngle * f) * Math.Cos(-PositionData.VAngle * f);
            double lookY = Math.Sin(-PositionData.VAngle * f);

            var result = World.CastRay(PositionData.Position + new Offset(0, PhysicsValues.PlayerEyeHeight, 0), new Offset(lookX, lookY, lookZ), PhysicsValues.MiningDistance);
            if (result == null)
            {
                HighlightedCube = null;
                _miningTime = GameDuration.Zero;
            }
            else
            {
                if (RayCastResult.FaceEquals(HighlightedCube, result)
                    && _controller.IsKeyPressed(GameKey.Primary))
                {
                    _miningTime += elapsedDuration;

                    if (_miningTime.Seconds >= PhysicsValues.MiningTime)
                    {
                        _cubeUpdates.Add(new CubeUpdateData
                        {
                            X = result.CubeX,
                            Y = result.CubeY,
                            Z = result.CubeZ,
                            Material = 0,
                        });
                    }
                }
                else
                {
                    _miningTime = GameDuration.Zero;

                    if (_placementCooldown <= GameDuration.Zero && _controller.IsKeyPressed(GameKey.Secondary))
                    {
                        _placementCooldown.Seconds = PhysicsValues.PlacementCooldown;
                        _cubeUpdates.Add(new CubeUpdateData
                        {
                            X = result.CubeX + result.NormalX,
                            Y = result.CubeY + result.NormalY,
                            Z = result.CubeZ + result.NormalZ,
                            Material = 1,
                        });
                    }
                }

                HighlightedCube = result;
            }
        }

        private void MovePlayer(GameDuration elapsedDuration)
        {
            double vx = 0, vz = 0, vy = PositionData.Velocity.Y;

            double f = Math.PI / 180.0;
            double lookZ = -PhysicsValues.PlayerMovementSpeed * Math.Cos(PositionData.HAngle * f);
            double lookX = -PhysicsValues.PlayerMovementSpeed * Math.Sin(PositionData.HAngle * f);

            bool isMovingAlong = false, isMovingSideways = false;
            if (_controller.IsKeyPressed(GameKey.Forwards))
            {
                isMovingAlong = true;
                vx += lookX;
                vz += lookZ;
            }

            if (_controller.IsKeyPressed(GameKey.Left))
            {
                isMovingSideways = true;
                vx += lookZ;
                vz -= lookX;
            }

            if (_controller.IsKeyPressed(GameKey.Backwards))
            {
                isMovingAlong = true;
                vx -= lookX;
                vz -= lookZ;
            }

            if (_controller.IsKeyPressed(GameKey.Right))
            {
                isMovingSideways = true;
                vx -= lookZ;
                vz += lookX;
            }

            if (isMovingAlong && isMovingSideways)
            {
                vx *= _inverseSqrt2;
                vz *= _inverseSqrt2;
            }

            if (!PositionData.IsFalling && _controller.IsKeyPressed(GameKey.Jump))
            {
                vy += GetJumpingSpeed();
            }

            Movement.MoveEntity(PhysicsValues, World, PositionData, elapsedDuration, vx, vy, vz);
        }

        private double GetJumpingSpeed()
        {
            return Math.Sqrt(2 * PhysicsValues.Gravity * PhysicsValues.PlayerJumpHeight);
        }

        private int GetCurrentCubeCoordinate(long position, long direction)
        {
            return Position.GetCubeCoordinate(position + (1L << 31) * direction);
        }
    }
}
