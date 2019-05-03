using System;
using System.Threading.Tasks;

using Discord;

using Modix.Data.Models.Core;
using Modix.Services.Core;

namespace Modix.Services.Mentions
{
    public interface IMentionService
    {
        /// <summary>
        /// Mentions the given role in the given channel
        /// </summary>
        /// <param name="role">The role to mention</param>
        /// <param name="channel">The channel to mention in</param>
        /// <param name="message">The message to send alongside the mention</param>
        Task<bool> MentionRoleAsync(IRole role, IMessageChannel channel, string message = null, Embed embed = null);
    }

    /// <inheritdoc />
    internal class MentionService : IMentionService
    {
        public MentionService(
            IDiscordClient discordClient,
            IAuthorizationService authorizationService,
            IDesignatedRoleService designatedRoleService)
        {
            DiscordClient = discordClient;
            AuthorizationService = authorizationService;
            DesignatedRoleService = designatedRoleService;
        }

        /// <inheritdoc />
        public async Task<bool> MentionRoleAsync(IRole role, IMessageChannel channel, string message = null, Embed embed = null)
        {
            if (role is null)
                throw new ArgumentNullException(nameof(role));

            if (channel is null)
                throw new ArgumentNullException(nameof(channel));

            if (message is null)
                message = string.Empty;

            if (role.IsMentionable)
            {
                await channel.SendMessageAsync($"You can do that yourself - but fine: {role.Mention}");
                return true;
            }

            AuthorizationService.RequireClaims(AuthorizationClaim.MentionRestrictedRole);

            if (await DesignatedRoleService.RoleHasDesignationAsync(role.Guild.Id, role.Id, DesignatedRoleType.RestrictedMentionability) == false)
            {
                await channel.SendMessageAsync($"Sorry, **{role.Name}** hasn't been designated as mentionable.");
                return false;
            }

            //Set the role to mentionable, immediately mention it, then set it
            //to unmentionable again

            await role.ModifyAsync(x => x.Mentionable = true);

            //Make sure we set the role to unmentionable again no matter what
            try
            {
                // This is a temporary hot-fix pending the creation of an EmbedBuilderService
                // and formatting all bot responses as embeds, where appropriate.
                if (embed is null)
                    await channel.SendMessageAsync($"{role.Mention} {message}");
                else
                    await channel.SendMessageAsync($"{role.Mention}", embed: embed);
            }
            finally
            {
                await role.ModifyAsync(x => x.Mentionable = false);
            }

            return true;
        }

        /// <summary>
        /// An <see cref="IDiscordClient"/> for interacting with the Discord API.
        /// </summary>
        internal protected IDiscordClient DiscordClient { get; }

        /// <summary>
        /// An <see cref="IAuthorizationService"/> to be used to interact with frontend authentication system, and perform authorization.
        /// </summary>
        internal protected IAuthorizationService AuthorizationService { get; }

        /// <summary>
        /// An <see cref="IDesignatedRoleService"/> to be used to check if a role is mentionable
        /// </summary>
        public IDesignatedRoleService DesignatedRoleService { get; }
    }
}
