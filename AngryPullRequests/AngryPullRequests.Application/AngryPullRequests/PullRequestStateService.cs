using AngryPullRequests.Application.AngryPullRequests.Common.Interfaces;
using AngryPullRequests.Domain.Entities;
using AngryPullRequests.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AngryPullRequests.Application.AngryPullRequests
{
    public enum ExperienceLabel
    {
        Newcomer,
        Inexperienced,
        ModeratelyExperienced,
        Experienced,
        VeryExperienced
    }

    public class PullRequestStateService : IPullRequestStateService
    {
        private const string DirtyConstant = "dirty";
        private readonly RepositoryCharacteristics repositoryCharacteristics;

        public PullRequestStateService(RepositoryCharacteristics repositoryCharacteristics)
        {
            this.repositoryCharacteristics = repositoryCharacteristics;
        }

        public bool IsPullRequestApproved(PullRequest pullRequest, PullRequestReview[] reviews, User[] requestedReviewers)
        {
            if (reviews == null || reviews.Length == 0)
            {
                return false;
            }

            var lastReviews = GetLastReviews(reviews);
            var nonReRequestedReviews = FilterNonReRequestedUserReviews(lastReviews, requestedReviewers);

            return nonReRequestedReviews.All(r => r.Value.State != PullRequestReviewStates.ChangesRequested)
                && nonReRequestedReviews.Any(r => r.Value.State == PullRequestReviewStates.Approved);
        }

        private static Dictionary<User, PullRequestReview> FilterNonReRequestedUserReviews(
            Dictionary<User, PullRequestReview> userReviews,
            User[] requestedReviewers
        )
        {
            return userReviews.Where(r => !requestedReviewers.Select(a => a.Id).Contains(r.Key.Id)).ToDictionary(r => r.Key, r => r.Value);
        }

        private static Dictionary<User, PullRequestReview> GetLastReviews(PullRequestReview[] reviews)
        {
            return reviews
                .GroupBy(a => a.User.Id, b => b)
                .Select(group => new { ReviewerId = group.Key, Review = group.OrderBy(r => r.SubmittedAt).Last() })
                .ToDictionary(r => r.Review.User, r => r.Review);
        }

        public bool IsHuge(PullRequest pullRequest) => pullRequest.Additions + pullRequest.Deletions > repositoryCharacteristics.LargePrChangeCount;

        public bool FollowsNamingConventions(PullRequest pullRequest)
        {
            if (string.IsNullOrEmpty(repositoryCharacteristics.PullRequestNameRegex))
            {
                return true;
            }

            return new Regex(repositoryCharacteristics.PullRequestNameRegex).IsMatch(pullRequest.Title);
        }

        public bool HasReviewer(PullRequest pullRequest) => pullRequest.RequestedReviewers.Any();

        public bool IsOld(PullRequest pullRequest)
        {
            var age = DateTimeOffset.Now - pullRequest.CreatedAt;

            return age >= TimeSpan.FromDays(repositoryCharacteristics.OldPrAgeInDays);
        }

        public bool IsSmall(PullRequest pullRequest) => pullRequest.Additions + pullRequest.Deletions <= repositoryCharacteristics.SmallPrChangeCount;

        public bool IsInactive(PullRequest pullRequest)
        {
            var age = DateTimeOffset.Now - pullRequest.UpdatedAt;

            return age >= TimeSpan.FromDays(repositoryCharacteristics.InactivePrAgeInDays);
        }

        public bool IsDeleteHeavy(PullRequest pullRequest) =>
            pullRequest.Additions / (float)pullRequest.Deletions <= repositoryCharacteristics.DeleteHeavyRatio;

        public bool IsInProgress(PullRequest pullRequest)
        {
            if (string.IsNullOrEmpty(repositoryCharacteristics.InProgressLabel))
            {
                return false;
            }

            return pullRequest.Labels.Any(l => repositoryCharacteristics.InProgressLabel.Equals(l.Name));
        }

        public bool HasReleaseTag(PullRequest pullRequest)
        {
            if (string.IsNullOrEmpty(repositoryCharacteristics.ReleaseTagRegex))
            {
                return true;
            }

            return pullRequest.Labels.Any(l => new Regex(repositoryCharacteristics.ReleaseTagRegex).IsMatch(l.Name));
        }

        public string GetReleaseTag(PullRequest pullRequest)
        {
            if (!HasReleaseTag(pullRequest))
            {
                return null;
            }

            return pullRequest.Labels.First(l => new Regex(repositoryCharacteristics.ReleaseTagRegex).IsMatch(l.Name)).Name;
        }

        public bool HasJiraTicket(PullRequest pullRequest)
        {
            if (string.IsNullOrEmpty(repositoryCharacteristics.IssueRegex))
            {
                return false;
            }

            var match = new Regex(repositoryCharacteristics.IssueRegex).Match(pullRequest.Title);

            if (match.Success && match.Groups.Count == 2)
            {
                return true;
            }

            return false;
        }

        public Dictionary<string, ExperienceLabel> GetUserExperienceLabels(Dictionary<string, UserExperience> userExperience)
        {
            Dictionary<string, double> experienceLevels = new Dictionary<string, double>();
            Dictionary<string, double> filteredExperienceLevels = new Dictionary<string, double>();
            foreach (var developer in userExperience.Keys)
            {
                double experienceLevel =
                    userExperience[developer].PullRequestsAuthored / (double)userExperience[developer].TimeSinceFirstAuthoring.Days;
                experienceLevels.Add(developer, experienceLevel);

                if (userExperience[developer].TimeSinceFirstAuthoring.Days >= 90)
                {
                    filteredExperienceLevels.Add(developer, experienceLevel);
                }
            }

            double[] percentiles = new double[] { 0.25, 0.50, 0.75 };
            double[] thresholds = percentiles
                .Select(p => filteredExperienceLevels.Values.OrderBy(x => x).ElementAt((int)(p * (filteredExperienceLevels.Count - 1))))
                .ToArray();

            Dictionary<string, ExperienceLabel> experienceLabels = new Dictionary<string, ExperienceLabel>();
            foreach (var developer in experienceLevels.Keys)
            {
                ExperienceLabel label;

                if (userExperience[developer].TimeSinceFirstAuthoring.Days < 90)
                {
                    label = ExperienceLabel.Newcomer;
                }
                else if (experienceLevels[developer] <= thresholds[0])
                {
                    label = ExperienceLabel.Inexperienced;
                }
                else if (experienceLevels[developer] <= thresholds[1])
                {
                    label = ExperienceLabel.ModeratelyExperienced;
                }
                else if (experienceLevels[developer] <= thresholds[2])
                {
                    label = ExperienceLabel.Experienced;
                }
                else
                {
                    label = ExperienceLabel.VeryExperienced;
                }

                experienceLabels.Add(developer, label);
            }

            return experienceLabels;
        }

        public static bool IsUserActive(UserExperience userExperience)
        {
            return userExperience.TimeSinceFirstAuthoring > TimeSpan.FromDays(60);
        }

        public string GetJiraTicket(PullRequest pullRequest)
        {
            if (!HasJiraTicket(pullRequest))
            {
                return null;
            }

            var match = new Regex(repositoryCharacteristics.IssueRegex).Match(pullRequest.Title);

            var value = match.Groups[1].Value.Trim();

            return value;
        }

        public bool DoesLikelyHaveConflicts(PullRequest pullRequest) => DirtyConstant.Equals(pullRequest.MergeableState);

        public string GetNameWithoutJiraTicket(PullRequest pullRequest)
        {
            if (string.IsNullOrEmpty(repositoryCharacteristics.PullRequestNameCaptureRegex))
            {
                return null;
            }

            if (!HasJiraTicket(pullRequest))
            {
                return null;
            }

            var match = new Regex(repositoryCharacteristics.PullRequestNameCaptureRegex).Match(pullRequest.Title);

            if (!match.Success || match.Groups.Count != 2)
            {
                return null;
            }

            var value = match.Groups[1].Value.Trim();

            return value;
        }
    }
}