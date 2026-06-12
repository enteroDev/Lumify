/* TestDbFactory
 * Creates a throwaway LumifyDbContext for tests.
 * This is NOT a mock - it is a real LumifyDbContext, but its data lives only in RAM
 * (EF Core in-memory provider). The real SQLite database is never touched.
 */

using lumify.api.Models.Context;
using Microsoft.EntityFrameworkCore;

namespace lumify.tests.Helper
{
    public static class TestDbFactory
    {
        // -------------- //
        // --- Create --- //
        // -------------- //

        // Creates a fresh, empty in-memory LumifyDbContext.
        // A unique database name per call keeps every test fully isolated from the others.
        public static LumifyDbContext Create()
        {
            DbContextOptions<LumifyDbContext> options = new DbContextOptionsBuilder<LumifyDbContext>()
                .UseInMemoryDatabase("lumify-tests-" + Guid.NewGuid().ToString())
                .Options;

            LumifyDbContext context = new LumifyDbContext(options);

            return context;
        }
    }
}
