using AngryPullRequests.Application.AngryPullRequests.Common.Exceptions;
using AngryPullRequests.Application.AngryPullRequests.Common.Interfaces;
using AngryPullRequests.Application.Github;
using AngryPullRequests.Application.Persistence;
using AngryPullRequests.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.AngryPullRequests.Commands
{
    public class CreateRepositoryCommand : IRequest<Repository>
    {
        public required string Name { get; set; }
        public required string Owner { get; set; }
        public required TimeOnly TimeOfDay { get; set; }
        public required string PullRequestNameRegex { get; set; }
        public required string PullRequestNameCaptureRegex { get; set; }
        public required string ReleaseTagRegex { get; set; }
        public required string InProgressLabel { get; set; }
        public required int SmallPrChangeCount { get; set; }
        public required int LargePrChangeCount { get; set; }
        public required int OldPrAgeInDays { get; set; }
        public required int InactivePrAgeInDays { get; set; }
        public required float DeleteHeavyRatio { get; set; }
        public required string SlackAccessToken { get; set; }
        public required string IssueBaseUrl { get; set; }
        public required string SlackApiToken { get; set; }
        public required string IssueRegex { get; set; }
        public required string SlackNotificationChannel { get; set; }
        public class Handler : IRequestHandler<CreateRepositoryCommand, Repository>
        {
            private readonly IAngryPullRequestsContext _dbContext;
            private readonly IMapper mapper;
            private readonly IUserService userService;
            private readonly IPullRequestServiceFactory pullRequestServiceFactory;
            private readonly IMediator mediator;

            public Handler(IAngryPullRequestsContext dbContext, IMapper mapper, IUserService userService, IPullRequestServiceFactory pullRequestServiceFactory, IMediator mediator)
            {
                _dbContext = dbContext;
                this.mapper = mapper;
                this.userService = userService;
                this.pullRequestServiceFactory = pullRequestServiceFactory;
                this.mediator = mediator;
            }

            public async Task<Repository> Handle(CreateRepositoryCommand request, CancellationToken cancellationToken)
            {
                var currentUser = await userService.GetCurrentUser();

                var repository = mapper.Map<Repository>(request);

                // TODO: Move validation to fluent

                var conflictingRepoExists = await _dbContext.Repositories.FirstOrDefaultAsync(r => r.Name == request.Name && r.Owner == request.Owner, cancellationToken: cancellationToken);

                if (conflictingRepoExists is not null)
                {
                    throw new RepositoryExists("Repository already exists");
                }

                await mediator.Send(new FetchTestGithubCommand { RepositoryName = request.Name, RepositoryOwner = request.Owner }, cancellationToken);
                await mediator.Send(new SendTestSlackMessageCommand { ApiToken = request.SlackApiToken, SlackNotificationChannel = request.SlackNotificationChannel }, cancellationToken);

                repository.AngryUser = currentUser;

                await _dbContext.Repositories.AddAsync(repository, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return repository;
            }
        }
    }
}