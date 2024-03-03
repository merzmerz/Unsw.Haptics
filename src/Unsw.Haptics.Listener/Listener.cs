using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unsw.Haptics.Common;

namespace Unsw.Haptics.Listener;

public class Listener : IHostedService
{
    private readonly ILogger<Listener> _logger;
    private readonly NetManager _netManager;
    private Timer? _pollTimer;
    private readonly NetPacketProcessor _netPacketProcessor = new();
    private readonly ListenerOptions _options;

    public Listener(IEnumerable<IPacketHandler> packetHandlers, IOptions<ListenerOptions> options, 
        ILogger<Listener> logger)
    {
        _logger = logger;

        var listener = new EventBasedNetListener();
        
        _netManager = new NetManager(listener)
        {
            BroadcastReceiveEnabled = true,
            IPv6Enabled = IPv6Mode.Disabled
        };

        SetupListener(listener);
    
        _options = options.Value;

        async void OnPacketReceived(HapticPacket packet, NetPeer peer)
        {
            var tasks = packetHandlers.Select(p => p.HandleAsync(packet));
            await Task.WhenAll(tasks);
        }
        _netPacketProcessor.SubscribeReusable<HapticPacket, NetPeer>(OnPacketReceived);
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _netManager.Start(_options.Port);
        _logger.LogInformation("netmanager {Port}", _netManager.Start(_options.Port));
        
        _pollTimer = new Timer(_ => _netManager.PollEvents(), null, TimeSpan.Zero, _options.PollingInterval);
        
        _logger.LogInformation("Listening on *:{Port}", _options.Port);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pollTimer?.Dispose();
        _netManager.Stop();

        return Task.CompletedTask;
    }

    private void SetupListener(EventBasedNetListener listener)
    {
        listener.ConnectionRequestEvent += request =>
           request.Accept();

        listener.PeerConnectedEvent += peer => 
            _logger.LogInformation("Peer {Peer} connected", peer.EndPoint);
        
        listener.PeerDisconnectedEvent += (peer, info) =>
            _logger.LogInformation("Peer {Peer} disconnected {@DisconnectInfo}", peer.EndPoint, info);
        
        listener.NetworkErrorEvent += (endPoint, error) =>
            _logger.LogError("A network error: {Error} occured on {EndPoint}", endPoint, error);
        
        listener.NetworkReceiveEvent += (peer, reader, _) => 
            _netPacketProcessor.ReadAllPackets(reader, peer);

        listener.NetworkReceiveUnconnectedEvent += (endPoint, _, messageType) =>
        {
            _logger.LogError("type {type}", messageType);
            _netManager.SendUnconnectedMessage(NetDataWriter.FromString("DISCOVERY RESPONSE"), endPoint);
        };
          

        listener.NetworkLatencyUpdateEvent += (peer, latency) =>
            _logger.LogInformation("Peer {Peer} latency: {Latency}ms", peer.EndPoint, latency);
    }
}