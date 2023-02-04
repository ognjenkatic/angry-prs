﻿using AutoMapper;
using Octokit;
using System.Linq;
using System.Threading.Tasks;

namespace AngryPullRequests.Infrastructure.Services
{
    public class PullRequestService : IPullRequestService
    {
        private readonly IGitHubClient gitHubClient;
        private readonly IMapper mapper;

        public PullRequestService(IGitHubClient gitHubClient, IMapper mapper)
        {
            this.gitHubClient = gitHubClient;
            this.mapper = mapper;
        }

        public async Task<Domain.Models.PullRequest[]> GetPullRequests(string owner, string repository)
        {
            var pullRequests = await gitHubClient.PullRequest.GetAllForRepository(owner, repository);

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
    }
}