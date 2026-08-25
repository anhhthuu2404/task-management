using Microsoft.EntityFrameworkCore;
using System;
using TaskManagement.Books;
using TaskManagement.Categories;
using TaskManagement.Departments;
using TaskManagement.Employees;
using TaskManagement.LocalizationManagement.Languages;
using TaskManagement.LocalizationManagement.LanguageTexts;
using TaskManagement.SysMasterLists;
using TaskManagement.Tags;
using TaskManagement.Tasks;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace TaskManagement.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) :
    AbpDbContext<TaskManagementDbContext>(options),
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Aggregate Roots / Entities */
    public DbSet<Book> Books { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<LanguageText> LanguageTexts { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<SysMasterList> SysMasterLists { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<UserDepartment> UserDepartments { get; set; }

    // Module Tasks
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskAttachment> TaskAttachments { get; set; }
    public DbSet<SubTask> SubTasks { get; set; }
    public DbSet<TaskChecklistItem> TaskChecklistItems { get; set; }
    public DbSet<TaskActivityLog> TaskActivityLogs { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }

    #region Entities from ABP Modules

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* ABP Framework Modules */
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Application Entities Configuration */
        builder.Entity<Book>(b =>
        {
            b.ToTable(TaskManagementConsts.DbTablePrefix + "Books", TaskManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });

        builder.Entity<Category>(b =>
        {
            b.ToTable("Categories");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.ColorCode).HasMaxLength(32);
        });

        builder.Entity<Tag>(b =>
        {
            b.ToTable("Tags");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(64);
        });

        builder.Entity<Department>(b =>
        {
            b.ToTable("Departments");
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(32);
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.HasOne<Department>()
             .WithMany()
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserDepartment>(b =>
        {
            b.ToTable("UserDepartments");
            b.ConfigureByConvention();
            b.HasKey(x => new { x.UserId, x.DepartmentId });
        });

        builder.Entity<TaskItem>(b =>
        {
            b.ToTable("Tasks");
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);

            b.HasMany(x => x.Attachments)
             .WithOne()
             .HasForeignKey(x => x.TaskId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskAttachment>(b =>
        {
            b.ToTable("TaskAttachments");
            b.ConfigureByConvention();
            b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(512);
        });

        builder.ApplyConfiguration(new LanguageEfCoreMapping());
        builder.ApplyConfiguration(new LanguageTextEfCoreMapping());
        builder.ApplyConfiguration(new EmployeeEfCoreMapping());
        builder.ApplyConfiguration(new SysMasterListEfCoreMapping());

        builder.Entity<SubTask>(b =>
        {
            b.ToTable("SubTasks");
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
        });

        builder.Entity<TaskChecklistItem>(b =>
        {
            b.ToTable("TaskChecklistItems");
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
        });

        builder.Entity<TaskActivityLog>(b =>
        {
            b.ToTable("TaskActivityLogs");
            b.ConfigureByConvention();
            b.Property(x => x.Action).IsRequired().HasMaxLength(500);
            b.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
        });

        builder.Entity<TaskComment>(b =>
        {
            b.ToTable("AppTaskComments");
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired().HasMaxLength(2000);

            // Cấu hình OwnsMany tương thích với Guid Id
            b.OwnsMany(x => x.Attachments, a =>
            {
                a.ToTable("TaskCommentAttachments");
                a.WithOwner().HasForeignKey("TaskCommentId");
                a.Property<Guid>("Id"); // Đã đổi int -> Guid
                a.HasKey("Id");
                a.Property(x => x.FileName).IsRequired().HasMaxLength(256);
                a.Property(x => x.FileUrl).HasMaxLength(512);
            });
 
    });
    }
}