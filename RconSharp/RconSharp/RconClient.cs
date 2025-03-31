using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace RconSharp;

/// <summary>
/// Rcon protocol messages handler
/// </summary>
public class RconClient
{
  public string Host { get; private set; }
  public int Port { get; private set; }

  public event Action ConnectionClosed;

  private readonly IChannel Channel;
  private readonly Pipe Pipe = new();

  private Encoding Encoding;

  private readonly Queue<Operation> Operations = new();
  private Task CommunicationTask;

  public static RconClient Create(string host, int port) => new(new SocketChannel(host, port));
  public static RconClient Create(IChannel channel) => new(channel);

  /// <summary>
  /// Class constructor
  /// </summary>
  /// <param name="channel">a NetworkSocket implementation</param>
  private RconClient(IChannel channel)
  {
    Channel = channel 
      ?? throw new NullReferenceException("channel parameter must be an instance of a class implementing INetworkSocket interface");
    Encoding = Encoding.UTF8;
  }

  public RconClient WithEncoding(Encoding encoding)
  {
    Encoding = encoding;
    return this;
  }

  /// <summary>
  /// Connect the socket to the remote endpoint
  /// </summary>
  /// <returns>True if the connection was successful; False if a connection was already established</returns>
  public async Task ConnectAsync()
  {
    if (Channel.IsConnected) return;
    await Channel.ConnectAsync();
    var readingTask = ReadFromPipeAsync(Pipe.Reader);
    var writingTask = WriteToPipeAsync(Pipe.Writer);
    CommunicationTask = Task.WhenAll(readingTask, writingTask).ContinueWith(t =>
    {
      Pipe.Reset();
      while (Operations.TryDequeue(out var result))
      {
        result.TaskCompletionSource.SetCanceled();
      }
      ConnectionClosed?.Invoke();
    });
  }

  /// <summary>
  /// Disconnect the channel
  /// </summary>
  /// <remarks>Will cancel all pending operations</remarks>
  public void Disconnect() => Channel.Disconnect();

  /// <summary>
  /// Write data on the the channel
  /// </summary>
  /// <param name="writer">PipeWriter to write data to</param>
  /// <returns>Operation's Task</returns>
  /// <remarks>Will throw an exception if the connection is not established</remarks>
  private async Task WriteToPipeAsync(PipeWriter writer)
  {
    while (true)
    {
      var buffer = writer.GetMemory(14);
      try
      {
        var bytesCount = await Channel.ReceiveAsync(buffer);
        if (bytesCount == 0) break;

        writer.Advance(bytesCount);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"[{nameof(WriteToPipeAsync)}] Exception: {ex.Message}");
        break;
      }

      var flushResult = await writer.FlushAsync();
      if (flushResult.IsCompleted) break;

    }
    await writer.CompleteAsync();
  }

  /// <summary>
  /// Read data from the channel
  /// </summary>
  /// <param name="reader">PipeReader to read data from</param>
  /// <returns><see cref="Task"></returns>
  /// <remarks>Will throw an exception if the connection is not established</remarks>
  private async Task ReadFromPipeAsync(PipeReader reader)
  {
    while (true)
    {
      var readResult = await reader.ReadAsync();
      var buffer = readResult.Buffer;
      var startPosition = buffer.Start;
      if (buffer.Length < 4) // not enough bytes to get the packet length, need to read more
      {
        if (readResult.IsCompleted) break;

        reader.AdvanceTo(startPosition, buffer.End);
        continue;
      }

      var packetSize = BitConverter.ToInt32(buffer.Slice(startPosition, 4).ToArray());
      if (buffer.Length >= packetSize + 4)
      {
        var endPosition = buffer.GetPosition(packetSize + 4, startPosition);
        var rconPacket = RconPacket.FromBytes(buffer.Slice(startPosition, endPosition).ToArray(), Encoding);
        if (!rconPacket.IsDummy)
        {
          var currentOperation = Operations.Peek();
          currentOperation.Add(rconPacket);
          if (currentOperation.OriginalRequest.Type == PacketType.Auth && rconPacket.Type == PacketType.Response)
          {
            // According to RCON documentation an empty RESPONSE packet must be sent after the auth request, but at the moment only CS:GO does so ..
          }
          else if (currentOperation.IsMultiPacketResponse && !string.IsNullOrEmpty(rconPacket.Body))
          {
            // Accumulate and move on
          }
          else
          {
            Operations.Dequeue();
            if (rconPacket.Id == -1) currentOperation.TaskCompletionSource.SetException(new AuthenticationException("Invalid password"));
            else currentOperation.TaskCompletionSource.SetResult(currentOperation.Body);
          }
        }

        reader.AdvanceTo(endPosition);
      }
      else
      {
        reader.AdvanceTo(startPosition, buffer.End);
      }

      if (buffer.IsEmpty && readResult.IsCompleted) break;
    }

    await reader.CompleteAsync();
  }

  /// <summary>
  /// Send the proper authentication packet and parse the response
  /// </summary>
  /// <param name="password">Current server password</param>
  /// <returns>True if the connection has been authenticated; False elsewhere</returns>
  /// <remarks>This method must be called prior to sending any other command</remarks>
  /// <exception cref="ArgumentException">Is thrown if <paramref name="password"/> parameter is null or empty</exception>
  public async Task<bool> AuthenticateAsync(string password)
  {
    if (string.IsNullOrEmpty(password))
      throw new ArgumentException("password parameter must be a non null non empty string");

    var authPacket = RconPacket.Create(PacketType.Auth, password);
    try
    {
      var response = await SendPacketAsync(authPacket);
    }
    catch (AuthenticationException)
    {
      return false;
    }
    return true;
  }

  public Task<string> ExecuteCommandAsync(string command, bool isMultiPacketResponse = false)
  {
    return SendPacketAsync(RconPacket.Create(PacketType.ExecCommand, command), isMultiPacketResponse);
  }

  /// <summary>
  /// Send a message encapsulated into an Rcon packet and get the response
  /// </summary>
  /// <param name="packet">Packet to be sent</param>
  /// <returns>The response to this command</returns>
  private async Task<string> SendPacketAsync(RconPacket packet, bool isMultiPacketResponse = false)
  {
    var packetDescription = new Operation(packet, isMultiPacketResponse);
    Operations.Enqueue(packetDescription);
    await Channel.SendAsync(packet.ToBytes(Encoding));
    if (isMultiPacketResponse)
    {
      await Channel.SendAsync(RconPacket.Dummy.Value.ToBytes(Encoding));
    }

    return await packetDescription.TaskCompletionSource.Task;
  }

  private class Operation(RconPacket originalRequest, bool isMultiPacketResponse)
  {
    public TaskCompletionSource<string> TaskCompletionSource { get; } = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    
    public RconPacket OriginalRequest { get; } = originalRequest;
    
    public bool IsMultiPacketResponse { get; } = isMultiPacketResponse;
    
    private readonly List<RconPacket> PacketsBuffer = [];
    
    public void Add(RconPacket packet) => PacketsBuffer.Add(packet);
    
    public string Body => string.Concat(PacketsBuffer.Select(b => b.Body));
  }
}
