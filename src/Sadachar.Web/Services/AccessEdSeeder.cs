using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Infrastructure.Data;

namespace Sadachar.Web.Services;

public static class AccessEdSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Roles
        string[] roles = [AppRoles.Student, AppRoles.Teacher, AppRoles.Administrator, "Admin", "Educator"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Users
        var studentUser = await userManager.FindByEmailAsync("student@accessed.local");
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = "student@accessed.local",
                Email = "student@accessed.local",
                EmailConfirmed = true,
                FullName = "Aarav Sharma",
                Role = UserRole.Student,
                AccessibilityNeed = "Low vision",
                Department = "BCA Semester 5",
                Disability = DisabilityType.Visual,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-3)
            };
            var result = await userManager.CreateAsync(studentUser, "Access@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, AppRoles.Student);
            }
        }

        var teacherUser = await userManager.FindByEmailAsync("teacher@accessed.local");
        if (teacherUser == null)
        {
            teacherUser = new ApplicationUser
            {
                UserName = "teacher@accessed.local",
                Email = "teacher@accessed.local",
                EmailConfirmed = true,
                FullName = "Dr. Meera Iyer",
                Role = UserRole.Teacher,
                AccessibilityNeed = "None",
                Department = "Computer Science",
                Disability = DisabilityType.None,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddYears(-1)
            };
            var result = await userManager.CreateAsync(teacherUser, "Access@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(teacherUser, AppRoles.Teacher);
                await userManager.AddToRoleAsync(teacherUser, "Educator");
            }
        }

        var adminUser = await userManager.FindByEmailAsync("admin@accessed.local");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@accessed.local",
                Email = "admin@accessed.local",
                EmailConfirmed = true,
                FullName = "Rohan Kapoor",
                Role = UserRole.Administrator,
                AccessibilityNeed = "None",
                Department = "Administration",
                Disability = DisabilityType.None,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddYears(-2)
            };
            var result = await userManager.CreateAsync(adminUser, "Access@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Accessibility Profiles
        if (studentUser != null && !await db.AccessibilityProfiles.AnyAsync(p => p.UserId == studentUser.Id))
        {
            db.AccessibilityProfiles.Add(new AccessibilityProfile
            {
                UserId = studentUser.Id,
                FontSize = 120,
                Theme = "paper",
                TtsSpeed = 1.0,
                Typeface = "dyslexia",
                LineSpacing = "1.8",
                ReduceMotion = false,
                VisibleFocus = true,
                UnderlineLinks = true
            });
        }

        if (teacherUser != null && !await db.AccessibilityProfiles.AnyAsync(p => p.UserId == teacherUser.Id))
        {
            db.AccessibilityProfiles.Add(new AccessibilityProfile
            {
                UserId = teacherUser.Id,
                FontSize = 100,
                Theme = "paper",
                TtsSpeed = 1.0,
                Typeface = "default",
                LineSpacing = "1.4"
            });
        }

        if (adminUser != null && !await db.AccessibilityProfiles.AnyAsync(p => p.UserId == adminUser.Id))
        {
            db.AccessibilityProfiles.Add(new AccessibilityProfile
            {
                UserId = adminUser.Id,
                FontSize = 100,
                Theme = "paper",
                TtsSpeed = 1.0,
                Typeface = "default",
                LineSpacing = "1.4"
            });
        }

        await db.SaveChangesAsync();

        // 4. Courses
        if (!await db.Courses.AnyAsync(c => c.Title == "Data Structures"))
        {
            var courses = new List<Course>
            {
                new Course
                {
                    Title = "Data Structures",
                    Subject = "Computer Science",
                    Department = "Computer Science",
                    TeacherId = teacherUser?.Id,
                    AccessibilityScore = 96,
                    ActiveTerm = "Autumn term",
                    Description = "Foundational algorithms, pointers, arrays, stacks, queues, and linked data structures.",
                    IsPublished = true
                },
                new Course
                {
                    Title = "Digital Literacy",
                    Subject = "Computer Science",
                    Department = "Computer Science",
                    TeacherId = teacherUser?.Id,
                    AccessibilityScore = 91,
                    ActiveTerm = "Autumn term",
                    Description = "Essential digital skills, internet safety, assistive software, and accessibility standards.",
                    IsPublished = true
                },
                new Course
                {
                    Title = "Web Technologies",
                    Subject = "Computer Science",
                    Department = "Computer Science",
                    TeacherId = teacherUser?.Id,
                    AccessibilityScore = 78,
                    ActiveTerm = "Autumn term",
                    Description = "HTML5 semantics, modern CSS, JavaScript, responsive frameworks, and WCAG AA guidelines.",
                    IsPublished = true
                },
                new Course
                {
                    Title = "Database Systems",
                    Subject = "Computer Science",
                    Department = "Computer Science",
                    TeacherId = teacherUser?.Id,
                    AccessibilityScore = 88,
                    ActiveTerm = "Autumn term",
                    Description = "Relational database concepts, SQL, schema normalization, indexing, and transaction management.",
                    IsPublished = true
                },
                new Course
                {
                    Title = "Communication Skills",
                    Subject = "Communication",
                    Department = "Humanities",
                    TeacherId = teacherUser?.Id,
                    AccessibilityScore = 85,
                    ActiveTerm = "Autumn term",
                    Description = "Effective verbal, non-verbal, and accessible written communication across professional contexts.",
                    IsPublished = true
                }
            };
            db.Courses.AddRange(courses);
            await db.SaveChangesAsync();

            // Enroll Aarav Sharma
            if (studentUser != null)
            {
                foreach (var c in courses)
                {
                    db.Enrollments.Add(new Enrollment
                    {
                        UserId = studentUser.Id,
                        CourseId = c.Id,
                        EnrolledAtUtc = DateTime.UtcNow.AddMonths(-2)
                    });
                }
                await db.SaveChangesAsync();
            }
        }

        // 5. Materials and Formats
        var dsCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == "Data Structures");
        var dlCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == "Digital Literacy");
        var wtCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == "Web Technologies");
        var dbCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == "Database Systems");
        var commCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == "Communication Skills");

        if (!await db.Materials.AnyAsync())
        {
            var mat1 = new Material
            {
                CourseId = dsCourse?.Id ?? Guid.NewGuid(),
                Title = "Linked Lists: Insertion",
                Module = "Computer Science › Module 3 › Lesson 2",
                Subject = "Computer Science",
                EstimatedMinutes = 12,
                AccessibilityScore = 96,
                ContentText = "A linked list stores items in nodes. Each node holds a value and a reference to the next node. To insert at the head, create a new node and point it to the old head. Finally, update the head reference so the list starts from the new node. Inserting at the tail needs a traversal unless a tail pointer is kept. Both approaches are compared in the practice section. Accessible design starts with choices. Learners should decide how fast, how loud and in which voice their materials are read. Each sentence is highlighted as it is spoken so that reading and listening stay in sync. You can pause at any point."
            };
            db.Materials.Add(mat1);

            var mat2 = new Material
            {
                CourseId = dlCourse?.Id ?? Guid.NewGuid(),
                Title = "Intro to Assistive Tech",
                Module = "Digital Literacy › Module 1 › Lesson 1",
                Subject = "Digital Literacy",
                EstimatedMinutes = 12,
                AccessibilityScore = 98,
                ContentText = "Assistive technology encompasses assistive, adaptive, and rehabilitative devices for people with disabilities and also includes the process used in selecting, locating, and using them. Screen readers, refreshable braille displays, voice recognition software, and screen magnifiers empower learners to access information on equal terms. Web Content Accessibility Guidelines provide the universal blueprint for accessible content."
            };
            db.Materials.Add(mat2);

            var mat3 = new Material
            {
                CourseId = dsCourse?.Id ?? Guid.NewGuid(),
                Title = "Binary Trees Explained",
                Module = "Computer Science › Module 4 › Lesson 1",
                Subject = "Computer Science",
                EstimatedMinutes = 12,
                AccessibilityScore = 92,
                ContentText = "A binary tree is a tree data structure in which each node has at most two children, referred to as the left child and the right child. Traversal techniques include in-order, pre-order, and post-order traversals."
            };
            db.Materials.Add(mat3);

            var mat4 = new Material
            {
                CourseId = commCourse?.Id ?? Guid.NewGuid(),
                Title = "Sign Language Basics",
                Module = "Communication › Module 1 › Lesson 1",
                Subject = "Communication",
                EstimatedMinutes = 12,
                AccessibilityScore = 95,
                ContentText = "Indian Sign Language (ISL) uses visual-manual modality to convey meaning. It involves combining hand shapes, orientation and movement of the hands, arms or body, and facial expressions to fluidly express a speaker's thoughts."
            };
            db.Materials.Add(mat4);

            var mat5 = new Material
            {
                CourseId = dbCourse?.Id ?? Guid.NewGuid(),
                Title = "Database Normalisation",
                Module = "Information Systems › Module 2 › Lesson 3",
                Subject = "Information Systems",
                EstimatedMinutes = 12,
                AccessibilityScore = 88,
                ContentText = "Database normalization is the process of structuring a relational database in accordance with a series of normal forms to reduce data redundancy and improve data integrity."
            };
            db.Materials.Add(mat5);

            var mat6 = new Material
            {
                CourseId = wtCourse?.Id ?? Guid.NewGuid(),
                Title = "Web Accessibility 101",
                Module = "Web Technologies › Module 1 › Lesson 1",
                Subject = "Web Technologies",
                EstimatedMinutes = 12,
                AccessibilityScore = 94,
                ContentText = "Web accessibility means that websites, tools, and technologies are designed and developed so that people with disabilities can use them. Specifically, people can perceive, understand, navigate, and interact with the Web."
            };
            db.Materials.Add(mat6);

            await db.SaveChangesAsync();

            // Formats for mat1 (Linked Lists)
            db.MaterialFormats.AddRange(
                new MaterialFormat { MaterialId = mat1.MaterialId, FormatType = "Audio", FilePath = "/media/linked-lists.mp3", DurationMinutes = 12, HasAltText = true, HasCaptions = true },
                new MaterialFormat { MaterialId = mat1.MaterialId, FormatType = "Text", FilePath = "/docs/linked-lists.txt", DurationMinutes = 10, HasAltText = true, HasCaptions = false },
                new MaterialFormat { MaterialId = mat1.MaterialId, FormatType = "Large print", FilePath = "/docs/linked-lists-large.pdf", DurationMinutes = 12, HasAltText = true, HasCaptions = false },
                new MaterialFormat { MaterialId = mat1.MaterialId, FormatType = "Braille-ready", FilePath = "/docs/linked-lists.brf", DurationMinutes = 15, HasAltText = true, HasCaptions = false },
                new MaterialFormat { MaterialId = mat1.MaterialId, FormatType = "Captioned video", FilePath = "/media/linked-lists.mp4", DurationMinutes = 12, HasAltText = true, HasCaptions = true },
                new MaterialFormat { MaterialId = mat1.MaterialId, FormatType = "Easy-read", FilePath = "/docs/linked-lists-easy.pdf", DurationMinutes = 8, HasAltText = true, HasCaptions = false },

                // Formats for mat2 (Assistive Tech)
                new MaterialFormat { MaterialId = mat2.MaterialId, FormatType = "Easy-read", FilePath = "/docs/assistive-tech-easy.pdf", DurationMinutes = 12, HasAltText = true, HasCaptions = false },
                new MaterialFormat { MaterialId = mat2.MaterialId, FormatType = "Audio", FilePath = "/media/assistive-tech.mp3", DurationMinutes = 12, HasAltText = true, HasCaptions = true },

                // Formats for mat3 (Binary Trees)
                new MaterialFormat { MaterialId = mat3.MaterialId, FormatType = "Captioned", FilePath = "/media/binary-trees.mp4", DurationMinutes = 12, HasAltText = true, HasCaptions = true },
                new MaterialFormat { MaterialId = mat3.MaterialId, FormatType = "Audio", FilePath = "/media/binary-trees.mp3", DurationMinutes = 12, HasAltText = true, HasCaptions = false },

                // Formats for mat4 (Sign Language)
                new MaterialFormat { MaterialId = mat4.MaterialId, FormatType = "Sign video", FilePath = "/media/sign-language.mp4", DurationMinutes = 12, HasAltText = true, HasCaptions = true },
                new MaterialFormat { MaterialId = mat4.MaterialId, FormatType = "Captioned", FilePath = "/media/sign-language-cap.mp4", DurationMinutes = 12, HasAltText = true, HasCaptions = true },

                // Formats for mat5 (Database Normalisation)
                new MaterialFormat { MaterialId = mat5.MaterialId, FormatType = "Large print", FilePath = "/docs/db-norm-large.pdf", DurationMinutes = 12, HasAltText = true, HasCaptions = false },
                new MaterialFormat { MaterialId = mat5.MaterialId, FormatType = "Audio", FilePath = "/media/db-norm.mp3", DurationMinutes = 12, HasAltText = true, HasCaptions = true },

                // Formats for mat6 (Web Accessibility 101)
                new MaterialFormat { MaterialId = mat6.MaterialId, FormatType = "Braille-ready", FilePath = "/docs/web-acc-101.brf", DurationMinutes = 12, HasAltText = true, HasCaptions = false },
                new MaterialFormat { MaterialId = mat6.MaterialId, FormatType = "Captioned", FilePath = "/media/web-acc-101.mp4", DurationMinutes = 12, HasAltText = true, HasCaptions = true }
            );
            await db.SaveChangesAsync();
        }

        // 6. Compliance Audits
        if (!await db.ComplianceAudits.AnyAsync())
        {
            db.ComplianceAudits.AddRange(
                new ComplianceAudit { Standard = "WCAG 2.2 AA checks", Department = "Campus-wide", Score = 91, AuditedOn = DateTime.UtcNow.AddDays(-2), Status = "Compliant", AuditorName = "Compliance Daemon" },
                new ComplianceAudit { Standard = "Institutional policy", Department = "Campus-wide", Score = 84, AuditedOn = DateTime.UtcNow.AddDays(-5), Status = "Compliant", AuditorName = "Rohan Kapoor" },
                new ComplianceAudit { Standard = "Data protection", Department = "Campus-wide", Score = 79, AuditedOn = DateTime.UtcNow.AddDays(-7), Status = "Action needed", AuditorName = "Statutory Inspector" },

                new ComplianceAudit { Department = "Computer Science", Standard = "WCAG 2.2 Level AA", Score = 92, AuditedOn = new DateTime(2026, 10, 20), Status = "Compliant", FlaggedItemsCount = 2, AuditorName = "Dr. Meera Iyer" },
                new ComplianceAudit { Department = "Commerce", Standard = "WCAG 2.2 Level AA", Score = 81, AuditedOn = new DateTime(2026, 10, 18), Status = "Action needed", FlaggedItemsCount = 5, AuditorName = "External Auditor" },
                new ComplianceAudit { Department = "Humanities", Standard = "WCAG 2.2 Level AA", Score = 74, AuditedOn = new DateTime(2026, 10, 11), Status = "At risk", FlaggedItemsCount = 9, AuditorName = "Ombudsman Desk" },
                new ComplianceAudit { Department = "Sciences", Standard = "WCAG 2.2 Level AA", Score = 88, AuditedOn = new DateTime(2026, 10, 15), Status = "Compliant", FlaggedItemsCount = 3, AuditorName = "Internal Committee" }
            );
            await db.SaveChangesAsync();
        }

        // 7. Tickets
        if (!await db.Tickets.AnyAsync())
        {
            var studentId = studentUser?.Id ?? "student-user";
            var teacherId = teacherUser?.Id ?? "teacher-user";

            db.Tickets.AddRange(
                new Ticket
                {
                    TicketNumber = "#ACC-1042",
                    UserId = studentId,
                    Category = "Inaccessible material",
                    Priority = "High",
                    Status = "Open",
                    IsConfidential = false,
                    Subject = "PDF not screen-reader friendly",
                    Description = "The PDF for Module 3 cannot be read by my screen reader. Heading levels appear flattened and math diagrams lack descriptive alt text.",
                    AssigneeId = teacherId,
                    AssigneeInitials = "MI",
                    SlaDays = 2,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    SlaExpiryUtc = DateTime.UtcNow.AddDays(1)
                },
                new Ticket
                {
                    TicketNumber = "#ACC-1039",
                    UserId = studentId,
                    Category = "Media",
                    Priority = "Medium",
                    Status = "Open",
                    IsConfidential = false,
                    Subject = "Missing captions: Week 5",
                    Description = "Week 5 video recording on Binary Search does not have synchronized English captions available.",
                    AssigneeId = teacherId,
                    AssigneeInitials = "MI",
                    SlaDays = 2,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    SlaExpiryUtc = DateTime.UtcNow.AddHours(14)
                },
                new Ticket
                {
                    TicketNumber = "#ACC-1031",
                    UserId = studentId,
                    Category = "Facilities",
                    Priority = "High",
                    Status = "In review",
                    IsConfidential = false,
                    Subject = "Ramp access to Lab 204",
                    Description = "Wheelchair ramp to building 2 Computer Lab 204 is blocked by construction scaffolding.",
                    AssigneeId = teacherId,
                    AssigneeInitials = "MI",
                    SlaDays = 2,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                    SlaExpiryUtc = DateTime.UtcNow.AddHours(2)
                },
                new Ticket
                {
                    TicketNumber = "#ACC-0987",
                    UserId = studentId,
                    Category = "Media",
                    Priority = "Low",
                    Status = "Resolved",
                    IsConfidential = false,
                    Subject = "Captions out of sync",
                    Description = "Subtitles in Lesson 2 video lag behind speaker by 3 seconds.",
                    AssigneeId = teacherId,
                    AssigneeInitials = "MI",
                    SlaDays = 2,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                    SlaExpiryUtc = DateTime.UtcNow.AddDays(-8)
                },
                new Ticket
                {
                    TicketNumber = "#ACC-0954",
                    UserId = studentId,
                    Category = "Documents",
                    Priority = "Low",
                    Status = "Resolved",
                    IsConfidential = false,
                    Subject = "Contrast on quiz page",
                    Description = "Grey text on light grey background failed contrast ratio requirement.",
                    AssigneeId = teacherId,
                    AssigneeInitials = "MI",
                    SlaDays = 2,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-14),
                    SlaExpiryUtc = DateTime.UtcNow.AddDays(-12)
                }
            );
            await db.SaveChangesAsync();
        }

        // 8. Audit Logs
        if (!await db.AuditLogs.AnyAsync())
        {
            db.AuditLogs.AddRange(
                new AuditLog { ActorId = teacherUser?.Id ?? "teacher", ActorName = "Dr. Meera Iyer", Action = "Web Technologies audit completed", Details = "Dr. Iyer · 2 items flagged", Timestamp = DateTime.UtcNow.AddHours(-3) },
                new AuditLog { ActorId = adminUser?.Id ?? "admin", ActorName = "Support team", Action = "Issue #ACC-0987 closed", Details = "Support team · within SLA", Timestamp = DateTime.UtcNow.AddHours(-8) },
                new AuditLog { ActorId = adminUser?.Id ?? "admin", ActorName = "Rohan Kapoor", Action = "Accessibility policy review published", Details = "Campus compliance score increased to 87%", Timestamp = DateTime.UtcNow.AddDays(-1) },
                new AuditLog { ActorId = teacherUser?.Id ?? "teacher", ActorName = "Dr. Meera Iyer", Action = "Material uploaded", Details = "Data Structures: Linked Lists (Multi-format)", Timestamp = DateTime.UtcNow.AddDays(-2) }
            );
            await db.SaveChangesAsync();
        }
    }
}

