﻿using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Enums;
using BotFramework.Utils;
using WinTenDev.ZiziBot.Alpha1.Handlers.Core;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.MemberChanges
{
    /// <summary>
    /// Handle left chat Member
    /// </summary>
    public class LeftChatMemberHandler : ZiziEventHandler
    {
        /// <summary>
        ///
        /// </summary>
        public LeftChatMemberHandler()
        {

        }

        // [Message(InChat.All)]
        /// <summary>
        ///
        /// </summary>
        [Message(MessageFlag.HasLeftChatMember)]
        public async Task OnLeftChatMember()
        {
            var htmlMsg = new HtmlString()
                .TextBr("Sampai jumpa lagi!");

            await SendMessageTextAsync(htmlMsg);
        }

    }
}