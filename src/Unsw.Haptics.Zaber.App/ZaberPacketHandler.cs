using Microsoft.Extensions.Options;
using Unsw.Haptics.Common;
using Unsw.Haptics.Listener;

namespace Unsw.Haptics.Zaber.App
{
    public class ZaberPacketHandler : IPacketHandler
    {
        private readonly ZaberPacketHandlerOptions _options;
        private readonly ZaberController _zaberController;
        
        public ZaberPacketHandler(IOptions<ZaberPacketHandlerOptions> options, ZaberController zaberController)
        {
            _options = options.Value;
            _zaberController = zaberController;
        }

        public async Task HandleAsync(HapticPacket hapticPacket)
        {
            var zaberDeviceId = GetZaberDeviceId(hapticPacket);
            
            if (!zaberDeviceId.HasValue)
                return;
            
            if (hapticPacket.HapticEvent == HapticEvent.EnterObject)
            {
                var tension = GetTension(hapticPacket);
                await _zaberController.ChangeTensionAsync(zaberDeviceId.Value, tension);
            }
            else
            {
                await _zaberController.RevertTensionAsync(zaberDeviceId.Value);
            }
        }
        
        private int? GetZaberDeviceId(HapticPacket packet)
        {
            if (_options.ZaberDevices.TryGetValue((packet.Handedness, packet.Finger), out var deviceId))
                return deviceId;
        
            return default;
        }
        
        private double GetTension(HapticPacket packet)
        {
            if (packet.Label != null && _options.LabelTensionMappings.TryGetValue(packet.Label, out var tension))
                return tension;
        
            return 0;
        }
    }
}
