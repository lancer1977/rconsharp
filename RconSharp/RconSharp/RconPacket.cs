﻿using System;
using System.Text;

namespace RconSharp;

/// <summary>
/// RCON Packet implementation
/// </summary>
public class RconPacket
{

    /// <summary>
    /// Create a new RCON packet for a given type, with a content
    /// </summary>
    /// <param name="type">Packet Type</param>
    /// <param name="content">Packet content</param>
    /// <remarks>Packet Id is assigned automatically</remarks>
    /// <returns>RCON Packet</returns>
    public static RconPacket Create(PacketType type, string content)
    {
        return new RconPacket
        {
            Type = type,
            Body = string.IsNullOrEmpty(content) ? "\0" : content,
            Id = ProgressiveId.Next(),
        };
    }

    /// <summary>
    /// Create a new RCON packet for a given type, with a content and forcing a specific Id
    /// </summary>
    /// <param name="id">Packet Id</param>
    /// <param name="type">Packet Type</param>
    /// <param name="content">Packet content</param>
    /// <returns>RCON Packet</returns>
    public static RconPacket Create(int id, PacketType type, string content)
    {
        return new RconPacket
        {
            Type = type,
            Body = string.IsNullOrEmpty(content) ? "\0" : content,
            Id = id,
        };
    }

    public static Lazy<RconPacket> Dummy { get; } = new Lazy<RconPacket>(() => new RconPacket { Id = 0, Type = PacketType.Response });

    private RconPacket() { }

    /// <summary>
    /// Gets the packet size according to RCON Protocol
    /// </summary>
    /// <remarks>This value is equal to 10 (fixed bytes) + body length. The 4 bytes for the Size field are not added</remarks>
    public int Size => Math.Max(10, Body.Length + 9);

    /// <summary>
    /// Gets or Sets the packet id
    /// </summary>
    /// <remarks>This value can be set to any integer. By default is set to the current Environment.TickCount property</remarks>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the Packet Type
    /// </summary>
    public PacketType Type { get; private set; }

    /// <summary>
    /// Gets the Packet body
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Gets the bytes composing this Packet as defined in RCON Protocol using the default UTF8 encoding
    /// </summary>
    /// <returns>Packet bytes array</returns>
    public byte[] ToBytes() => ToBytes(Encoding.UTF8);

    /// <summary>
    /// Gets the bytes composing this Packet as defined in RCON Protocol using a custom encoding
    /// </summary>
    /// <param name="encoding">Encoding</param>
    /// <returns>Packet bytes array</returns>
    public byte[] ToBytes(Encoding encoding)
    {
        var body = encoding.GetBytes(Body + "\0"); // add null string terminator
        var buffer = new byte[12 + body.Length + 1]; // 12 bytes for Length, Id and Type
        var span = buffer.AsSpan();
        body.CopyTo(span[12..]);
        BitConverter.GetBytes(body.Length + 9).CopyTo(span[0..4]);
        BitConverter.GetBytes(Id).CopyTo(span[4..8]);
        BitConverter.GetBytes((int)Type).CopyTo(span[8..12]);
        return buffer;
    }

    /// <summary>
    /// Create a new RconPacket from a byte array using the default UTF8 encoding
    /// </summary>
    /// <param name="buffer">Input buffer</param>
    /// <returns>Parsed RconPacket</returns>
    /// <exception cref="System.ArgumentException">When buffer is null or its size is less than 14</exception>
    public static RconPacket FromBytes(byte[] buffer) => FromBytes(buffer, Encoding.UTF8);

    /// <summary>
    /// Create a new RconPacket from a byte array using a custom encoding
    /// </summary>
    /// <param name="buffer">Input buffer</param>
    /// <param name="encoding">Encoding</param>
    /// <returns>Parsed RconPacket</returns>
    /// <exception cref="System.ArgumentException">When buffer is null or its size is less than 14</exception>
    public static RconPacket FromBytes(byte[] buffer, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Length < 4)
            throw new ArgumentException("Input buffer parameter length is less than minimum required of 4 bytes");

        var size = BitConverter.ToInt32(buffer.AsSpan()[..4]);
        if (size > buffer.Length - 4)
            throw new ArgumentOutOfRangeException($"Declared packet size [{size}] is larger than actual buffer length");

        return new RconPacket()
        {
            Type = (PacketType)BitConverter.ToInt32(buffer.AsSpan()[8..12]),
            Body = encoding.GetString(buffer[12..]).Replace("\0", ""),
            Id = BitConverter.ToInt32(buffer.AsSpan()[4..8])
        };
    }

    /// <summary>
    /// Get whether this packet is a Dummy one (ie. the one sent as response after a multi packet response)
    /// </summary>
    public bool IsDummy => Body.Equals("\u0001") && Id == 0;
}
