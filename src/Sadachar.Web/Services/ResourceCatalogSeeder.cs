using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Infrastructure.Data;

namespace Sadachar.Web.Services;

public static class ResourceCatalogSeeder
{
    private static readonly string[] Categories =
    [
        "Mathematics", "Science", "English and Communication", "Computer Science and Programming",
        "General Knowledge", "Competitive Exam Preparation", "Digital Literacy", "Life Skills",
        "Career Guidance", "Special Education"
    ];

    private static readonly (string Title, string Description, string Type, string Category, string Subject, string Url, string? Transcript, string Difficulty, int Minutes)[] Resources =
    [
        ("NCERT Textbooks", "Official school textbooks from India's National Council of Educational Research and Training.", "PDF study material", "Mathematics", "Mathematics", "https://ncert.nic.in/textbook.php", null, "Beginner", 45),
        ("Khan Academy Mathematics", "Free video lessons and practice covering mathematics from foundational to advanced topics.", "Video tutorial", "Mathematics", "Mathematics", "https://www.khanacademy.org/math", null, "Beginner", 30),
        ("Khan Academy Science", "Accessible science lessons, articles, and practice activities for independent learners.", "Interactive exercise", "Science", "Science", "https://www.khanacademy.org/science", null, "Beginner", 30),
        ("NCERT Science Textbooks", "Official science textbooks with structured explanations and activities.", "PDF study material", "Science", "Science", "https://ncert.nic.in/textbook.php", null, "Intermediate", 45),
        ("Khan Academy Grammar", "Grammar and communication lessons with videos and practice exercises.", "Interactive exercise", "English and Communication", "English", "https://www.khanacademy.org/humanities/grammar", null, "Beginner", 25),
        ("SWAYAM Language Courses", "Government-supported courses from Indian universities and institutions.", "External website", "English and Communication", "Communication", "https://swayam.gov.in/", null, "Intermediate", 60),
        ("NPTEL Programming Courses", "University-level programming and computer science courses from NPTEL.", "Video tutorial", "Computer Science and Programming", "Programming", "https://nptel.ac.in/courses", null, "Intermediate", 60),
        ("Code.org Learn", "Hands-on coding activities designed to make programming approachable for every learner.", "Interactive exercise", "Computer Science and Programming", "Programming", "https://code.org/learn", null, "Beginner", 30),
        ("National Digital Library of India", "A trusted discovery portal for books, articles, and learning material.", "External website", "General Knowledge", "General Knowledge", "https://ndl.iitkgp.ac.in/", null, "Intermediate", 30),
        ("SWAYAM Courses", "Searchable courses from Indian universities and public institutions.", "External website", "Competitive Exam Preparation", "Exam Preparation", "https://swayam.gov.in/", null, "Advanced", 60),
        ("NPTEL Learning Portal", "Open online courses and lectures useful for technical and competitive preparation.", "Video tutorial", "Competitive Exam Preparation", "Exam Preparation", "https://nptel.ac.in/", null, "Advanced", 60),
        ("Digital India", "Official digital services and citizen digital literacy information.", "External website", "Digital Literacy", "Digital Literacy", "https://www.digitalindia.gov.in/", null, "Beginner", 20),
        ("DIKSHA Learning Platform", "National digital infrastructure for school learning resources and courses.", "External website", "Digital Literacy", "Digital Literacy", "https://diksha.gov.in/", null, "Beginner", 30),
        ("Khan Academy Life Skills", "Practical lessons and tools to support independent learning and everyday decision-making.", "Video tutorial", "Life Skills", "Life Skills", "https://www.khanacademy.org/", null, "Beginner", 20),
        ("National Career Service", "Government career information, counselling, jobs, and employability services.", "External website", "Career Guidance", "Career Guidance", "https://www.ncs.gov.in/", null, "Beginner", 30),
        ("SWAYAM Career Courses", "Career-focused learning from recognised Indian institutions.", "External website", "Career Guidance", "Career Guidance", "https://swayam.gov.in/", null, "Intermediate", 45),
        ("NCERT Inclusive Education Resources", "Official education resources supporting inclusive classrooms and diverse learners.", "External website", "Special Education", "Inclusive Education", "https://ncert.nic.in/division/ie.php", null, "Intermediate", 30),
        ("Khan Academy Accessibility", "Accessible learning tools and support information for learners and educators.", "External website", "Special Education", "Inclusive Education", "https://www.khanacademy.org/about/accessibility", null, "Beginner", 20),
        ("NCERT Educational Resources", "Official learning resources for students, teachers, and families.", "External website", "General Knowledge", "General Knowledge", "https://ncert.nic.in/", null, "Beginner", 30),
        ("NPTEL Soft Skills Courses", "Communication, workplace, and professional development courses from NPTEL.", "Video tutorial", "Life Skills", "Soft Skills", "https://nptel.ac.in/courses", null, "Intermediate", 45)
    ];

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var name in Categories)
        {
            if (!await db.ResourceCategories.AnyAsync(c => c.Name == name, cancellationToken))
                db.ResourceCategories.Add(new ResourceCategory { Name = name, Description = $"Accessible {name.ToLowerInvariant()} resources." });
        }

        foreach (var item in Resources)
        {
            if (await db.LearningResources.AnyAsync(r => r.Title == item.Title && r.FileUrl == item.Url, cancellationToken)) continue;
            db.LearningResources.Add(new LearningResource
            {
                Title = item.Title, Description = item.Description, Type = item.Type, Category = item.Category,
                Subject = item.Subject, FileUrl = item.Url, TranscriptUrl = item.Transcript, Difficulty = item.Difficulty,
                EstimatedMinutes = item.Minutes, AccessibilityNotes = "Use captions, transcripts, browser zoom, and screen-reader controls where provided by the source.", IsPublished = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
