using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unsw.Haptics.Common;
using Unsw.Haptics.Listener;
using Unsw.Haptics.Zaber.App;
using Unsw.Haptics.Zaber.App.Configuration;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging(o =>
        {
            o.AddSimpleConsole(c =>
            {
                c.SingleLine = true;
                c.TimestampFormat = "[dd-MM-yyyy HH:mm:ss] ";
            });
        });
        var config = hostContext.Configuration.Get<AppConfiguration>();
        
        services.AddSingleton<ZaberController>();
        services.Configure<ZaberControllerOptions>(o =>
        {
            o.PortName = "63b498f9-9f8f-4b8b-8e7c-aa8ecc8665e0";
            o.MovementRanges = config.Zaber.ToDictionary(z => z.DeviceId, z => new ZaberMovementRange
            {
                Minimum = z.MinPosition,
                Maximum = z.MaxPosition
            });
        });
        
        services.AddListener<HapticPacket>(options =>
        {
            options.Port = 9050;
            options.PollingInterval = TimeSpan.FromMilliseconds(100);
        });
        
        services.AddPacketHandler<ZaberPacketHandler>();
        services.Configure<ZaberPacketHandlerOptions>(o =>
        {
            o.LabelTensionMappings = config.Tensions;
            o.ZaberDevices = config.Zaber.ToDictionary(z => (z.Handedness, z.Finger), z => z.DeviceId);
        });
        
    })
    .Build();

host.Run();