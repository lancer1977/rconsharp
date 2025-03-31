using System;
using System.Threading.Tasks;

namespace RconSharp;

/// <summary>
/// Shared interface for the Network Socket
/// </summary>
public interface IChannel
{
  /// <summary>
  /// Connect the socket to the remote endpoint
  /// </summary>
  /// <returns>True if the connection was successfully; False if the connection is already estabilished</returns>
  Task ConnectAsync();

  /// <summary>
  /// Disconnect the channel
  /// </summary>
  void Disconnect();

  /// <summary>
  /// Write data on the the channel
  /// </summary>
  /// <param name="payload">Payload to be written</param>
  /// <returns>Operation's Task</returns>
  Task SendAsync(ReadOnlyMemory<byte> payload);

  /// <summary>
  /// Read data from the channel
  /// </summary>
  /// <param name="responseBuffer">Buffer to be filled</param>
  /// <returns>Number of bytes read</returns>
  Task<int> ReceiveAsync(Memory<byte> responseBuffer);

  /// <summary>
  /// Get whether the channel is connected or not
  /// </summary>
  bool IsConnected { get; }
}
