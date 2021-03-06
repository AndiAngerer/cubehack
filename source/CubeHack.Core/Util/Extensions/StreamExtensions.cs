﻿// Copyright (c) the CubeHack authors. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the project root.

using ProtoBuf;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

internal static class StreamExtensions
{
    private const int _maxSize = 2 * 1024 * 1024; // We need to limit the number of chunks per packet and make this a lot smaller...

    public static async Task WriteObjectAsync<T>(this Stream stream, T instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        using (var buffer = new MemoryStream())
        {
            // Reserve space for the length
            buffer.Position = 4;

            Serializer.Serialize(buffer, instance);
            int length = (int)buffer.Position - 4;

            if (length < 0 || length > _maxSize)
            {
                throw new SerializationException("Invalid object size.");
            }

            // Write the length prefix
            buffer.Position = 0;
            buffer.WriteByte((byte)length);
            buffer.WriteByte((byte)(length >> 8));
            buffer.WriteByte((byte)(length >> 16));
            buffer.WriteByte((byte)(length >> 24));

            // Copy the whole buffer
            buffer.Position = 0;
            await buffer.CopyToAsync(stream);

            await stream.FlushAsync();
        }
    }

    public static async Task<T> ReadObjectAsync<T>(this Stream stream)
    {
        int length = await ReadInt32Async(stream);

        if (length < 0 || length > _maxSize)
        {
            throw new SerializationException("Invalid object size.");
        }

        byte[] dataBytes = new byte[length];
        await ReadArrayAsync(stream, dataBytes);

        using (var buffer = new MemoryStream(dataBytes))
        {
            return Serializer.Deserialize<T>(buffer);
        }
    }

    public static async Task<int> ReadInt32Async(this Stream stream)
    {
        byte[] bytes = new byte[4];
        await ReadArrayAsync(stream, bytes);
        return bytes[0] | ((int)bytes[1] << 8) | ((int)bytes[2] << 16) | ((int)bytes[3] << 24);
    }

    public static Task WriteInt32Async(this Stream stream, int value)
    {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)value;
        bytes[1] = (byte)(value >> 8);
        bytes[2] = (byte)(value >> 16);
        bytes[3] = (byte)(value >> 24);
        return stream.WriteAsync(bytes, 0, 4);
    }

    public static async Task ReadArrayAsync(this Stream stream, byte[] array)
    {
        int totalBytesRead = 0;
        int length = array.Length;
        while (totalBytesRead < length)
        {
            int bytesRead = await stream.ReadAsync(array, totalBytesRead, length - totalBytesRead);
            if (bytesRead == 0)
            {
                throw new IOException();
            }

            totalBytesRead += bytesRead;
        }
    }
}
