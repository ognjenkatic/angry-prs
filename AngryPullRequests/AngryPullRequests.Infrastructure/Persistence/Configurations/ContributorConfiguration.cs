﻿using AngryPullRequests.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryPullRequests.Infrastructure.Persistence.Configurations
{
    public class ContributorConfiguration : IEntityTypeConfiguration<Contributor>
    {
        public void Configure(EntityTypeBuilder<Contributor> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.GithubUsername).IsRequired();
            builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
        }
    }
}
