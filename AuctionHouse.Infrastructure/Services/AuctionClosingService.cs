using AuctionHouse.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuctionHouse.Infrastructure.Services;

/// <summary>
/// Süresi dolan açık artırmaları periyodik olarak tarayıp kapatan arka plan servisi.
/// IAuctionCloser scoped (DbContext) olduğu için her tarama kendi scope'unda çalışır.
/// </summary>
public class AuctionClosingService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionClosingService> _logger;

    public AuctionClosingService(IServiceScopeFactory scopeFactory, ILogger<AuctionClosingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Açık artırma kapanış servisi başladı (her {Sec} sn).", Interval.TotalSeconds);

        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var closer = scope.ServiceProvider.GetRequiredService<IAuctionCloser>();
                var closed = await closer.CloseExpiredAuctionsAsync(stoppingToken);

                if (closed > 0)
                    _logger.LogInformation("{Count} açık artırma kapatıldı.", closed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // normal kapanış
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Açık artırma kapanış taraması başarısız.");
            }
        }

        _logger.LogInformation("Açık artırma kapanış servisi durdu.");
    }
}
