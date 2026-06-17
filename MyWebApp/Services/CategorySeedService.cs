using Microsoft.EntityFrameworkCore;
using MyWebApp.Constants;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Services
{
    public class CategorySeedService
    {
        private readonly ApplicationDbContext _context;

        public CategorySeedService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedFixedCategoriesAsync()
        {
            var existingNames = await _context.Projects
                .Select(p => p.Name)
                .ToListAsync();

            var added = false;
            foreach (var name in CategoryConstants.FixedCategories)
            {
                if (existingNames.Contains(name))
                    continue;

                _context.Projects.Add(new Project
                {
                    Name = name,
                    Deadline = DateTime.Today.AddDays(30),
                    Status = "Open"
                });
                added = true;
            }

            if (added)
                await _context.SaveChangesAsync();
        }

        public async Task<Project> ResolveCategoryAsync(string? categoryName)
        {
            var name = CategoryConstants.ResolveName(categoryName);
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Name == name);

            if (project != null)
                return project;

            await SeedFixedCategoriesAsync();
            project = await _context.Projects.FirstOrDefaultAsync(p => p.Name == name);

            if (project == null)
                throw new InvalidOperationException($"Category '{name}' could not be resolved.");

            return project;
        }
    }
}
