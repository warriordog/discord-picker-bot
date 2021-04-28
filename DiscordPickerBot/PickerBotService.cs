using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordPickerBot
{
    public class PickerBotService : IHostedService
    {
        private readonly PickerBotMain _pickerBotMain;

        public PickerBotService(IServiceScopeFactory scopeFactory)
        {
            var scope = scopeFactory.CreateScope();
            _pickerBotMain = scope.ServiceProvider.GetRequiredService<PickerBotMain>();
        }

        public Task StartAsync(CancellationToken _) => _pickerBotMain.StartAsync();
        public Task StopAsync(CancellationToken _) => _pickerBotMain.StopAsync();
    }
}