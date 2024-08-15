using AngryPullRequests.Application.AngryPullRequests.Common.Models;
using AngryPullRequests.Domain.Entities;
using SlackNet.Blocks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.Slack.Formatters
{
    public abstract class BaseSlackMessageFormatter : ISlackMessageFormatter
    {
        protected static PlainText CreatePe(string text) => new() { Text = text, Emoji = true };

        protected static Markdown CreateMd(string text) => new() { Text = text };

        public abstract Task<List<Block>> GetBlocks(PullRequestNotificationGroup[] pullRequestNotificationGroups, Repository repository);
    }
}