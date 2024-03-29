﻿using AngryPullRequests.Application.Github;
using AngryPullRequests.Application.Persistence;
using AngryPullRequests.Domain.Entities;
using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Features.OwnedInstances;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace AngryPullRequests.Infrastructure.Github
{
    public class PullRequestServiceFactory : IPullRequestServiceFactory
    {
        private readonly ILifetimeScope lifetimeScope;

        public PullRequestServiceFactory(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public async Task<IPullRequestService> Create(Domain.Entities.Repository repository)
        {
            var mapper = lifetimeScope.Resolve<IMapper>();

            var gitHubClient = new GitHubClient(new ProductHeaderValue("AngryPullRequests"))
            {
                Credentials = new Credentials(repository.AngryUser.UserName, repository.AngryUser.GithubPat)
            };

            return await Task.FromResult(new PullRequestService(mapper, gitHubClient));
        }

        public async Task<IPullRequestService> Create(string pat)
        {
            var mapper = lifetimeScope.Resolve<IMapper>();

            var gitHubClient = new GitHubClient(new ProductHeaderValue("AngryPullRequests")) { Credentials = new Credentials(pat) };

            return await Task.FromResult(new PullRequestService(mapper, gitHubClient));
        }
    }

    public class PullRequestService : IPullRequestService
    {
        private readonly IMapper mapper;

        private readonly GitHubClient gitHubClient;

        public PullRequestService(IMapper mapper, GitHubClient gitHubClient)
        {
            this.mapper = mapper;
            this.gitHubClient = gitHubClient;
        }

        public async Task<Domain.Models.PullRequest[]> GetPullRequests(
            string owner,
            string repository,
            bool getAll,
            int pageCount,
            int pageSize,
            int startPage
        )
        {
            var pullRequests = await gitHubClient.PullRequest.GetAllForRepository(
                owner,
                repository,
                new PullRequestRequest { State = getAll ? ItemStateFilter.All : ItemStateFilter.Open, SortProperty = PullRequestSort.Created },
                new ApiOptions
                {
                    PageCount = pageCount,
                    PageSize = pageSize,
                    StartPage = startPage
                }
            );

            return mapper.Map<Domain.Models.PullRequest[]>(pullRequests.ToArray());
        }

        public async Task<Domain.Models.PullRequestReview[]> GetPullRequsetReviews(string owner, string repository, int pullRequestNumber)
        {
            var reviews = await gitHubClient.PullRequest.Review.GetAll(owner, repository, pullRequestNumber);

            return mapper.Map<Domain.Models.PullRequestReview[]>(reviews.ToArray());
        }

        public async Task<Domain.Models.User[]> GetRequestedReviewersUsers(string owner, string repository, int pullRequestNumber)
        {
            var requestedReviewersUsers = await gitHubClient.PullRequest.ReviewRequest.Get(owner, repository, pullRequestNumber);

            return mapper.Map<Domain.Models.User[]>(requestedReviewersUsers.Users.ToArray());
        }

        public async Task<Domain.Models.PullRequest> GetPullRequestDetails(string owner, string repository, int pullRequestNumber)
        {
            var pullRequest = await gitHubClient.PullRequest.Get(owner, repository, pullRequestNumber);

            return mapper.Map<Domain.Models.PullRequest>(pullRequest);
        }

        public async Task<Domain.Entities.Repository[]> GetCurrentUserRepositories()
        {
            var repos = await gitHubClient.Repository.GetAllForCurrent();

            return repos.Select(repo => new Domain.Entities.Repository { Name = repo.Name, Owner = repo.Owner.Login }).ToArray();
        }
    }
}
