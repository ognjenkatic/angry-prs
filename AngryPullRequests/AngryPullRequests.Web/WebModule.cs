using AngryPullRequests.Application.AngryPullRequests;
using AngryPullRequests.Application.AngryPullRequests.Common.Interfaces;
using AngryPullRequests.Application.Completion;
using AngryPullRequests.Application.Github;
using AngryPullRequests.Application.Persistence;
using AngryPullRequests.Application.Slack.Formatters;
using AngryPullRequests.Application.Slack.Services;
using AngryPullRequests.Infrastructure.Common;
using AngryPullRequests.Infrastructure.Github;
using AngryPullRequests.Infrastructure.OpenAi;
using AngryPullRequests.Web.Services;
using Autofac;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Octokit;
using SlackNet.Autofac;
using System.Security.Claims;

namespace AngryPullRequests.Web
{
    public class WebModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<RunnerHostedService>().As<IHostedService>();
            builder.RegisterType<AngryPullRequestsService>().As<IAngryPullRequestsService>();
            builder.RegisterType<SlackNotifierService>().As<IUserNotifierService>();
            builder.RegisterType<OpenAiCompletionService>().As<ICompletionService>();
            builder.RegisterType<MetricService>().As<IMetricService>();
            builder.RegisterType<PullRequestServiceFactory>().As<IPullRequestServiceFactory>();

            builder.RegisterType<ForgottenPullRequestsMessageFormatter>().As<ISlackMessageFormatter>();
            builder.RegisterType<DeveloperLoadMessageFormatter>().As<ISlackMessageFormatter>();

            builder.RegisterType<UserService>().As<IUserService>();
        }
    }
}