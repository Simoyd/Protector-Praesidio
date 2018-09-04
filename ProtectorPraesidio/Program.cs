using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProtectorPraesidio
{
    class Program
    {
        #region Constants

        /// <summary>
        /// Name of the bot on discord
        /// </summary>
        private const string BotName = "Protector Praesidio";

        /// <summary>
        /// ID for the main boundless server
        /// </summary>
        private const ulong MainBoundlessServer = 119962974533320704;

        /// <summary>
        /// ID for a server used only for the emotes
        /// </summary>
        private const ulong SimoydsPrivateServerForEmotes = 231603109976211457;

        /// <summary>
        /// Command that applies the citizen role
        /// </summary>
        private const string CitizenCommand = "!citizen";

        /// <summary>
        /// Role to apply for the citizen command
        /// </summary>
        private const string CitizenRole = "Citizen";

        /// <summary>
        /// Roles that have permission to execute !citizen
        /// </summary>
        private static readonly HashSet<string> CitizenRolePermissions = new HashSet<string>(new string[] { "Admins", "Moderators", "Developers" });

        #endregion

        #region Initialization

        /// <summary>
        /// Main application entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            new Thread(() => new Program().MainAsync().GetAwaiter().GetResult()).Start();
        }

        /// <summary>
        /// Main async thread entry point
        /// </summary>
        public async Task MainAsync()
        {
            // Configure the discord connection
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance,
            });

            // Subscribe to the events that we want
            _client.Log += Log;
            _client.Ready += Ready;
            _client.MessageReceived += MessageReceived;

            // Login to the discord server
            string token = "THIS IS PRIVATE"; // Remember to keep this private!
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        #endregion

        #region Private fields

        /// <summary>
        /// The client we are using to communicate with the discord server
        /// </summary>
        private DiscordSocketClient _client;

        /// <summary>
        /// The main boundless server
        /// </summary>
        SocketGuild _boundlessServer;

        /// <summary>
        /// "nope" emote for reacting to messages when permission is denied
        /// </summary>
        private IEmote _nopeEmote;

        #endregion

        #region Discord Callbacks

        /// <summary>
        /// Callback for log messages from the discord client
        /// </summary>
        /// <param name="arg">The log message</param>
        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the discord client is connected and ready
        /// </summary>
        private Task Ready()
        {
            // Ensure name is correct
            if (_client.CurrentUser.Username != BotName)
            {
                _client.CurrentUser.ModifyAsync(cur => cur.Username = BotName);
            }

            foreach (SocketGuild curGuild in _client.Guilds)
            {
                if (curGuild.CurrentUser.Nickname != BotName)
                {
                    curGuild.CurrentUser.ModifyAsync(cur => cur.Nickname = BotName);
                }
            }

            // Cache the boundless server object
            _boundlessServer = _client.GetGuild(MainBoundlessServer);

            // Cache the "nope" emote object
            SocketGuild emoteServer = _client.GetGuild(SimoydsPrivateServerForEmotes);
            _nopeEmote = emoteServer.Emotes.First(e => e.Name == "nope");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a message is recieved either in a channel the bot is in, or in DM
        /// </summary>
        /// <param name="arg">The received message</param>
        private async Task MessageReceived(SocketMessage arg)
        {
            if (arg.Content == "!ping")
            {
                // Basic ping to check if bot is alive
                await arg.Channel.SendMessageAsync("pong!");
            }
            else if (arg.Content.StartsWith(CitizenCommand))
            {
                // Determine if the issuing user has permission to run this command
                bool allowed = _boundlessServer.GetUser(arg.Author.Id)?.Roles.Where(cur => CitizenRolePermissions.Contains(cur.Name)).Any() ?? false;

                if (!allowed)
                {
                    await Nope(arg);
                    return;
                }

                // Get the target user ID
                Regex citizenCommandPattern = new Regex("^!citizen (<@!?([0-9]*)>|([0-9]*))$");
                Match match = citizenCommandPattern.Match(arg.Content);

                if (!match.Success)
                {
                    await Nope(arg);
                    return;
                }

                ulong userId = 0;

                if (match.Groups[2].Captures.Count > 0)
                {
                    userId = Convert.ToUInt64(match.Groups[2].Captures[0].Value);
                }
                else if (match.Groups[3].Captures.Count > 0)
                {
                    userId = Convert.ToUInt64(match.Groups[3].Captures[0].Value);
                }

                if (userId == 0)
                {
                    await Nope(arg);
                    return;
                }

                // Get the server user
                SocketGuildUser targetUser = _boundlessServer.GetUser(userId);

                if (targetUser == null)
                {
                    await Nope(arg);
                    return;
                }

                // Determine if user already has the role
                string[] targetRoles = targetUser.Roles.Select(cur => cur.Name).ToArray();

                if (targetRoles.Contains(CitizenRole))
                {
                    await arg.Channel.SendMessageAsync($"`{targetUser.Nickname ?? targetUser.Username}` already has the `{CitizenRole}` role.");
                    return;
                }

                // Get the citizen role we're trying to add
                SocketRole role = _boundlessServer.Roles.Where(cur => cur.Name == CitizenRole).FirstOrDefault();

                if (role == null)
                {
                    await Nope(arg);
                    return;
                }

                // Add the role and inform the user
                await targetUser.AddRoleAsync(role);
                await arg.Channel.SendMessageAsync($"`{targetUser.Nickname ?? targetUser.Username}` has been given the `{CitizenRole}` role! Congratulations!");
            }
        }

        #endregion

        /// <summary>
        /// Attaches the "nope" emoji to the specified message
        /// </summary>
        /// <param name="arg">The received message</param>
        private async Task Nope(SocketMessage arg)
        {
            await ((SocketUserMessage)arg).AddReactionAsync(_nopeEmote);
        }
    }
}
