using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AngryPullRequests.Infrastructure.Persistence
{
    public static class MigrationUtil
    {
        public static void MigrateDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AngryPullRequestsContext>();
            context.Database.Migrate();
        }
    }
}