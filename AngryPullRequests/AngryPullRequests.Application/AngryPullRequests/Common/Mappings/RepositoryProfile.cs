using AngryPullRequests.Application.AngryPullRequests.Commands;
using AngryPullRequests.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.AngryPullRequests.Common.Mappings
{
    public class RepositoryProfile : Profile
    {
        public RepositoryProfile()
        {
            CreateMap<CreateRepositoryCommand, Repository>()
                .ForMember(
                    dst => dst.Characteristics,
                    opt =>
                        opt.MapFrom(
                            src =>
                                new RepositoryCharacteristics
                                {
                                    OldPrAgeInDays = src.OldPrAgeInDays,
                                    LargePrChangeCount = src.LargePrChangeCount,
                                    DeleteHeavyRatio = src.DeleteHeavyRatio,
                                    InactivePrAgeInDays = src.InactivePrAgeInDays,
                                    InProgressLabel = src.InProgressLabel,
                                    IssueBaseUrl = src.IssueBaseUrl,
                                    IssueRegex = src.IssueRegex,
                                    PullRequestNameCaptureRegex = src.PullRequestNameCaptureRegex,
                                    PullRequestNameRegex = src.PullRequestNameRegex,
                                    ReleaseTagRegex = src.ReleaseTagRegex,
                                    SlackApiToken = src.SlackApiToken,
                                    SlackNotificationChannel = src.SlackNotificationChannel,
                                    SmallPrChangeCount = src.SmallPrChangeCount
                                }
                        )
                )
                .ForMember(
                    dst => dst.RunSchedule,
                    opt => opt.MapFrom(src => new RunSchedule { DaysOfWeek = new int[] { 1, 2, 3, 4, 5 }, TimeOfDay = src.TimeOfDay })
                );

            CreateMap<UpdateRepositoryCommand, Repository>()
                .ForMember(
                    dst => dst.Characteristics,
                    opt =>
                        opt.MapFrom(
                            src =>
                                new RepositoryCharacteristics
                                {
                                    OldPrAgeInDays = src.OldPrAgeInDays,
                                    LargePrChangeCount = src.LargePrChangeCount,
                                    DeleteHeavyRatio = src.DeleteHeavyRatio,
                                    InactivePrAgeInDays = src.InactivePrAgeInDays,
                                    InProgressLabel = src.InProgressLabel,
                                    IssueBaseUrl = src.IssueBaseUrl,
                                    IssueRegex = src.IssueRegex,
                                    PullRequestNameCaptureRegex = src.PullRequestNameCaptureRegex,
                                    PullRequestNameRegex = src.PullRequestNameRegex,
                                    ReleaseTagRegex = src.ReleaseTagRegex,
                                    SlackApiToken = src.SlackApiToken,
                                    SlackNotificationChannel = src.SlackNotificationChannel,
                                    SmallPrChangeCount = src.SmallPrChangeCount
                                }
                        )
                )
                .ForMember(
                    dst => dst.RunSchedule,
                    opt => opt.MapFrom(src => new RunSchedule { DaysOfWeek = new int[] { 1, 2, 3, 4, 5 }, TimeOfDay = src.TimeOfDay })
                );
        }
    }
}