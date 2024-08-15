using AngryPullRequests.Application.AngryPullRequests.Commands;
using AngryPullRequests.Application.AngryPullRequests.Common.Exceptions;
using AngryPullRequests.Application.AngryPullRequests.Queries;
using AspNetCoreHero.ToastNotification.Abstractions;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Octokit;
using SlackNet;
using System.ComponentModel.DataAnnotations;

namespace AngryPullRequests.Web.Pages
{
    [Authorize]
    [BindProperties]
    public class NewRepositoryModel : PageModel
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly INotyfService toastNotification;

        public class Model
        {
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string Name { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string Owner { get; set; }
            public required TimeOnly TimeOfDay { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string PullRequestNameRegex { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string PullRequestNameCaptureRegex { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string ReleaseTagRegex { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string InProgressLabel { get; set; }
            [Required]
            [Range(1, int.MaxValue)]
            public int SmallPrChangeCount { get; set; }
            [Required]
            [Range(1, int.MaxValue)]
            public int LargePrChangeCount { get; set; }
            [Required]
            [Range(1, int.MaxValue)]
            public int OldPrAgeInDays { get; set; }
            [Required]
            [Range(1, int.MaxValue)]
            public int InactivePrAgeInDays { get; set; }
            [Required]
            [Range(0.01f, int.MaxValue)]
            public float DeleteHeavyRatio { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string IssueBaseUrl { get; set; }
            [Required]
            [StringLength(maximumLength: 150, MinimumLength = 3)]
            public required string SlackApiToken { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string IssueRegex { get; set; }
            [Required]
            [StringLength(maximumLength: 50, MinimumLength = 3)]
            public required string SlackNotificationChannel { get; set; }
        }

        public Model Repository { get; set; }

        public NewRepositoryModel(IMediator mediator, IMapper mapper, INotyfService toastNotification)
        {
            this.mediator = mediator;
            this.mapper = mapper;
            this.toastNotification = toastNotification;

            Repository = new Model
            {
                DeleteHeavyRatio = 0.1f,
                InactivePrAgeInDays = 10,
                InProgressLabel = "in progress",
                Name = "",
                IssueBaseUrl = "https://my.ticketing-system.com/tickets/",
                IssueRegex = "^(.*)$",
                LargePrChangeCount = 500,
                OldPrAgeInDays = 10,
                Owner = "",
                PullRequestNameCaptureRegex = "^(.*)$",
                PullRequestNameRegex = "^.*$",
                ReleaseTagRegex = "Q[0-9]+\\.[0-9]+\\.[0-9]+",
                SlackApiToken = "",
                SlackNotificationChannel = "",
                SmallPrChangeCount = 100,
                TimeOfDay = new TimeOnly(7, 30)
            };
        }

        public async Task OnGet([FromQuery]Guid? clone) 
        {
            if (clone is not null)
            {
                try
                {
                    var repository = await mediator.Send(new GetRepositoryQuery { Id = (Guid)clone });

                    repository.Name = "";
                    repository.Owner = "";

                    mapper.Map(repository, Repository);
                }
                catch (Exception)
                {
                    toastNotification.Error("Could not clone target repository", 5);
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var createCommand = mapper.Map<CreateRepositoryCommand>(Repository);

                await mediator.Send(createCommand);

                return RedirectToPage($"/MyRepositories");
            }
            catch (SlackException ex)
            {
                if (ex.ErrorCode == "invalid_auth")
                {
                    toastNotification.Error("Authorization failed, could not send slack message", 5);
                }
                else if (ex.ErrorCode == "channel_not_found")
                {
                    toastNotification.Error("Channel not found, could not send slack message", 5);
                }
                else
                {
                    toastNotification.Error($"Generic slack error, could not send slack message", 5);
                }
            }
            catch (AuthorizationException)
            {
                toastNotification.Error(
                    "Authorization failed, Github token is invalid or does not have the required permissions",
                    5
                );
            }
            catch (RepositoryExists)
            {
                toastNotification.Error("Repository with the same name is already registered by some user, cannot create duplicates", 5);
            }
            catch (NotFoundException)
            {
                toastNotification.Error($"Repository {Repository.Name} owned by {Repository.Owner} not found on GitHub", 5);
            }
            catch (Exception)
            {
                toastNotification.Error($"Unknown error while registering, contact administrator", 5);
            }

            return Page();
        }
    }
}