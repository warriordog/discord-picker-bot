using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordPickerBot
{
    public class PickerBotMain
    {
        private readonly DiscordClient _discord;
        private readonly ILogger<PickerBotMain> _logger;
        private readonly Random _random;

        public PickerBotMain(ILoggerFactory loggerFactory, IOptions<BotOptions> botOptions, ILogger<PickerBotMain> logger)
        {
            _logger = logger;

            _random = new Random();

            // Create discord client
            _discord = new DiscordClient(
                new DiscordConfiguration
                {
                    Token = botOptions.Value.DiscordToken,
                    TokenType = TokenType.Bot,
                    LoggerFactory = loggerFactory,
                    Intents = DiscordIntents.DirectMessages | DiscordIntents.GuildMessages | DiscordIntents.Guilds | DiscordIntents.GuildMembers
                }
            );

            // Register event handler
            _discord.MessageCreated += HandleMessage;

            // Log when bot joins a server.
            _discord.GuildCreated += async (_, e) => 
            {
                // Log when we join a new server
                _logger.LogInformation($"Joined new server {e.Guild.Id} ({e.Guild.Name}).");
                
                // Preload the member list
                await e.Guild.RequestMembersAsync();
            };

            _discord.GuildAvailable += async (_, e) =>
            {
                // Log when we join an existing server
                _logger.LogInformation($"Logged into existing server {e.Guild.Id} ({e.Guild.Name}).");
                
                // Preload the member list
                await e.Guild.RequestMembersAsync();
            };
        }

        private async Task HandleMessage(DiscordClient client, MessageCreateEventArgs e)
        {
            using (_logger.BeginScope("HandleMessage"))
            {
                try
                {
                    // Don't reply to ourself
                    if (e.Author.Equals(client.CurrentUser)) return;
                    
                    // Don't run in bots
                    if (e.Channel.IsPrivate) return;
                    
                    // Don't reply to bots
                    if (e.Message.Author.IsBot) return;
                    
                    // Check for trigger message
                    if (!e.Message.Content.ToLower().Contains("pick!")) return;
                    
                    _logger.LogDebug("Invoked by [{user}]", e.Message.Author);
                
                    // Get current member (user from current channel)
                    var currentMember = e.Channel.Users.FirstOrDefault(mbr => mbr.Equals(client.CurrentUser));
                
                    // Check for chat permissions
                    if (currentMember == null || !e.Channel.PermissionsFor(currentMember).HasPermission(Permissions.SendMessages))
                    {
                        _logger.LogDebug("Skipping for lack of permissions");
                        return;
                    }
                    
                    // Get random response
                    var responseMessage = GetResponseMessage(e.Channel.Guild);

                    // Send response
                    await e.Message.RespondAsync(responseMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in message handler");
                }   
            }
        }

        private string GetResponseMessage(DiscordGuild guild)
        {
            // Get all members
            var members = guild.Members.Values.ToList();
            if (!members.Any())
            {
                _logger.LogError("Member list is empty!");
                return "What? There is no one here!";
            }

            // Pick a random member
            var member = members[_random.Next(members.Count)];
            
            // Create response
            return $"{member.DisplayName} ({member.Username}#{member.Discriminator})";
        }
        
        public async Task StartAsync()
        {
            _logger.LogInformation($"PickerBot {GetType().Assembly.GetName().Version} starting");
            await _discord.ConnectAsync();
        }

        public Task StopAsync()
        {
            _logger.LogInformation("PickerBot stopping");
            return _discord.DisconnectAsync();
        }
    }
}