using AngryPullRequests.Application.AngryPullRequests.Common.Interfaces;
using AngryPullRequests.Application.Github;
using AngryPullRequests.Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.AngryPullRequests
{
    public class MetricService : IMetricService
    {
        private static int lastPageFetched = 1;
        private static int lastPrProcessed = 0;
        private static int totalPrs = 0;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private static Dictionary<string, UserExperience> authorExperienceMap = new Dictionary<string, UserExperience>();
        private readonly IPullRequestService pullRequestService;

        public MetricService(IPullRequestService pullRequestService)
        {
            this.pullRequestService = pullRequestService;
        }

        public static int GetNumberOfPullRequests() => totalPrs;

        public async Task<Dictionary<string, UserExperience>> GetAuthorExperience(string repository, string owner, string author)
        {
            await _semaphore.WaitAsync();
            var goNext = false;
            do
            {
                var swatch = new Stopwatch();

                swatch.Start();

                var allPullRequests = await pullRequestService.GetPullRequests(owner, repository, true, 1, 100, lastPageFetched);

                swatch.Stop();

                var pullRequests = allPullRequests.Skip(lastPrProcessed).ToArray();

                totalPrs += pullRequests.Length;

                lastPrProcessed = (lastPrProcessed + pullRequests.Length) % 100;

                if (pullRequests.Length >= 100)
                {
                    goNext = true;
                    lastPageFetched++;
                }
                else
                {
                    goNext = false;
                }

                foreach (var pr in pullRequests)
                {
                    ProcessPullRequest(pr);
                }
            } while (goNext);

            _semaphore.Release();
            return authorExperienceMap;
        }

        private static void ProcessPullRequest(PullRequest pullRequest)
        {
            var author = pullRequest.User.Login;

            if (!authorExperienceMap.TryGetValue(author, out var userExperience))
            {
                userExperience = new UserExperience();
                authorExperienceMap[author] = userExperience;
            }

            if (pullRequest.Merged)
            {
                userExperience.PullRequestsAuthored++;

                if (pullRequest.CreatedAt < userExperience.FirstAuthoring)
                {
                    userExperience.FirstAuthoring = pullRequest.CreatedAt.UtcDateTime;
                }
                if (pullRequest.CreatedAt > userExperience.LastAuthoring)
                {
                    userExperience.LastAuthoring = pullRequest.CreatedAt.UtcDateTime;
                }
            }
        }
    }
}