﻿// Copyright (c) 2014 the CubeHack authors. All rights reserved.
// Licensed under a BSD 2-clause license, see LICENSE.txt for details.

using CubeHack.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CubeHack.Tcp
{
    class TcpChannel : IChannel
    {
        readonly object _mutex = new object();
        readonly TcpClient _tcpClient;

        Stream _stream;
        bool _isConnected;

        public TcpChannel(string host, int port)
        {
            _tcpClient = new TcpClient();
            Task.Run(() => RunChannel(host, port));
        }

        public ModData ModData { get; private set; }

        Func<GameEvent, Task> _onGameEventAsync;
        public Func<GameEvent, Task> OnGameEventAsync
        {
            get
            {
                lock (_mutex)
                {
                    return _onGameEventAsync;
                }
            }

            set
            {
                lock (_mutex)
                {
                    _onGameEventAsync = value;
                }
            }
        }

        public void SendPlayerEvent(PlayerEvent playerEvent)
        {
            lock (_mutex)
            {
                if (!_isConnected)
                {
                    return;
                }

                _stream.WriteObjectAsync(playerEvent).Wait();
            }
        }

        private async Task RunChannel(string host, int port)
        {
            try
            {
                _tcpClient.NoDelay = true;
                await _tcpClient.ConnectAsync(host, port);

                var stream = _tcpClient.GetStream();
                await stream.WriteAsync(TcpConstants.MAGIC_COOKIE, 0, TcpConstants.MAGIC_COOKIE.Length);
                await stream.FlushAsync();

                ModData = await stream.ReadObjectAsync<ModData>();

                lock (_mutex)
                {
                    _isConnected = true;
                    _stream = stream;
                }

                while (true)
                {
                    var gameEvent = await stream.ReadObjectAsync<GameEvent>();

                    Func<GameEvent, Task> onGameEventAsync;
                    lock (_mutex)
                    {
                        onGameEventAsync = _onGameEventAsync;
                    }

                    if (onGameEventAsync != null)
                    {
                        await onGameEventAsync(gameEvent);
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Drop to the main menu or something. For now, any disconnect terminates the application.
                Environment.Exit(0);
            }
        }
    }
}
