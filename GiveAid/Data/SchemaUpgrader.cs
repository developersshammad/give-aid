using Microsoft.EntityFrameworkCore;

namespace GiveAid.Data
{
    /// <summary>
    /// Brings an OLDER GiveAidDB up to date without deleting any data.
    ///
    /// Why: Database.EnsureCreated() only creates the database when it does not exist yet.
    /// If GiveAidDB was created by an earlier version of this project, the new AI Programme
    /// Planner tables/columns would be missing and pages would fail with "Invalid column name".
    /// Every statement below checks first, so it is safe to run on every start-up
    /// (on a brand-new database it simply does nothing).
    /// </summary>
    public static class SchemaUpgrader
    {
        public static void Upgrade(GiveAidContext db)
        {
            db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('dbo.Programmes', 'IsPublished') IS NULL
    ALTER TABLE dbo.Programmes ADD IsPublished BIT NOT NULL CONSTRAINT DF_Programmes_IsPublished DEFAULT (1);

IF COL_LENGTH('dbo.Programmes', 'Beneficiaries') IS NULL
    ALTER TABLE dbo.Programmes ADD Beneficiaries INT NULL;

IF OBJECT_ID('dbo.ProgrammeCostLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProgrammeCostLines (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProgrammeCostLines PRIMARY KEY,
        ProgrammeId  INT            NOT NULL,
        Item         NVARCHAR(100)  NOT NULL,
        Amount       DECIMAL(18,2)  NOT NULL,
        CONSTRAINT FK_ProgrammeCostLines_Programmes_ProgrammeId FOREIGN KEY (ProgrammeId) REFERENCES dbo.Programmes(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ProgrammeCostLines_ProgrammeId ON dbo.ProgrammeCostLines(ProgrammeId);
END

IF OBJECT_ID('dbo.PlannerSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlannerSessions (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlannerSessions PRIMARY KEY,
        Goal         NVARCHAR(1000) NOT NULL,
        Status       NVARCHAR(30)   NOT NULL,
        Mode         NVARCHAR(20)   NOT NULL,
        Summary      NVARCHAR(4000) NULL,
        ProgrammeId  INT            NULL,
        CreatedBy    NVARCHAR(100)  NOT NULL,
        CreatedAt    DATETIME2      NOT NULL,
        CompletedAt  DATETIME2      NULL,
        CONSTRAINT FK_PlannerSessions_Programmes_ProgrammeId FOREIGN KEY (ProgrammeId) REFERENCES dbo.Programmes(Id)
    );
    CREATE INDEX IX_PlannerSessions_ProgrammeId ON dbo.PlannerSessions(ProgrammeId);
END

IF OBJECT_ID('dbo.PlannerSteps', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlannerSteps (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlannerSteps PRIMARY KEY,
        SessionId         INT            NOT NULL,
        [Order]           INT            NOT NULL,
        Tool              NVARCHAR(50)   NOT NULL,
        Title             NVARCHAR(150)  NOT NULL,
        Explanation       NVARCHAR(2000) NULL,
        InputJson         NVARCHAR(MAX)  NOT NULL,
        ResultJson        NVARCHAR(MAX)  NULL,
        Sources           NVARCHAR(300)  NULL,
        RequiresApproval  BIT            NOT NULL,
        Status            NVARCHAR(20)   NOT NULL,
        DecidedBy         NVARCHAR(100)  NULL,
        DecidedAt         DATETIME2      NULL,
        Note              NVARCHAR(1000) NULL,
        CreatedAt         DATETIME2      NOT NULL,
        CONSTRAINT FK_PlannerSteps_PlannerSessions_SessionId FOREIGN KEY (SessionId) REFERENCES dbo.PlannerSessions(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_PlannerSteps_SessionId ON dbo.PlannerSteps(SessionId);
END

-- The sample pictures changed from .svg drawings to compressed .webp photos.
-- Point older rows at the new files (only the built-in sample paths are touched).
UPDATE dbo.Programmes SET ImagePath = REPLACE(ImagePath, '.svg', '.webp')
 WHERE ImagePath IN ('/images/programmes/education.svg','/images/programmes/health.svg','/images/programmes/special.svg',
                     '/images/programmes/women.svg','/images/programmes/youth.svg','/images/programmes/elderly.svg','/images/programmes/children.svg');
UPDATE dbo.GalleryImages SET ImagePath = REPLACE(ImagePath, '.svg', '.webp')
 WHERE ImagePath LIKE '/images/gallery/g[1-8].svg';
");
        }
    }
}
