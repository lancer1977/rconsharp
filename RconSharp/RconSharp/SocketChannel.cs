using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RconSharp;

/// <summary>
/// Tcp implementation of <see cref="IChannel"/> interface
/// </summary>
public class SocketChannel : IChannel
{
    private Socket Socket;
    private readonly string Host;
    private readonly int Port;
    private readonly int ReadTimeout;
    private readonly int WriteTimeout;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="host">Remote host address</param>
    /// <param name="port">Remote host port</param>
    /// <param name="readTimeout">Read timeout in millis. Default 5000</param>
    /// <param name="writeTimeout">Write timeout in millis. Default 5000</param>
    /// <exception cref="ArgumentException">Is thrown when host is null or empty</exception>
    /// <exception cref="ArgumentException">Is thrown when port is less or equal than 0</exception>
    /// <exception cref="ArgumentException">Is thrown when readTimeout is less than 0</exception>
    /// <exception cref="ArgumentException">Is thrown when writeTimeout is less than 0</exception>
    public SocketChannel(
      string host,
      int port,
      int readTimeout = 5000,
      int writeTimeout = 5000)
    {
        if (string.IsNullOrEmpty(host)) throw new ArgumentException("Invalid host name: must be a non null non empty string containing the host's address");

        if (port < 1) throw new ArgumentException("Port parameter must be a positive value");

        if (readTimeout < 0) throw new ArgumentException("Read timeout parameter must be a positive value");
        if (writeTimeout < 0) throw new ArgumentException("Write timeout parameter must be a positive value");

        Host = host;
        Port = port;
        ReadTimeout = readTimeout;
        WriteTimeout = writeTimeout;
    }

    /// <summary>
    /// Connect the socket to the remote endpoint
    /// </summary>
    /// <returns>True if the connection was successful; False if the connection is already established</returns>
    public async Task ConnectAsync()
    {
        if (Socket != null)
            return;

        Socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Socket.ReceiveTimeout = 5000;
        Socket.SendTimeout = 5000;
        Socket.NoDelay = true;

        await Socket.ConnectAsync(Host, Port).ConfigureAwait(false);
    }

    /// <summary>
    /// Write data on the the channel
    /// </summary>
    /// <param name="payload">Payload to be written</param>
    /// <returns>Operation's Task</returns>
    public async Task SendAsync(ReadOnlyMemory<byte> payload)
    {
        await Socket.SendAsync(payload, SocketFlags.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Read data from the channel
    /// </summary>
    /// <param name="responseBuffer">Buffer to be filled</param>
    /// <returns>Number of bytes read</returns>
    public async Task<int> ReceiveAsync(Memory<byte> responseBuffer)
    {
        return await Socket.ReceiveAsync(responseBuffer, SocketFlags.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Disconnect the channel
    /// </summary>
    public void Disconnect()
    {
        Socket?.Close();
        Socket?.Dispose();
        Socket = null;
    }

    /// <summary>
    /// Get whether the channel is connected or not
    /// </summary>
    public bool IsConnected => Socket?.Connected ?? false;
}
