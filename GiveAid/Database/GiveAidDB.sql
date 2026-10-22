/* =====================================================================
   Give-AID NGO Website - SQL Server database script
   ---------------------------------------------------------------------
   NOTE: You normally do NOT need to run this script. When the website
   starts for the first time, Entity Framework Core creates this same
   database automatically (Program.cs -> EnsureCreated) and fills it with
   sample data (Data/DbSeeder.cs).

   Use this script if you want to create the database by hand in SQL Server
   Management Studio (SSMS). Then run the website once: the tables will be
   empty, so the seeder will insert the default admin and sample data.
   ===================================================================== */

IF DB_ID(N'GiveAidDB') IS NULL
    CREATE DATABASE GiveAidDB;
GO
USE GiveAidDB;
GO

/* ---------- Admins : administrator accounts ---------- */
CREATE TABLE Admins (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Admins PRIMARY KEY,
    FullName      NVARCHAR(100) NOT NULL,
    Username      NVARCHAR(50)  NOT NULL,
    Email         NVARCHAR(150) NOT NULL,
    PasswordHash  NVARCHAR(200) NOT NULL,     -- PBKDF2 hash, never plain text
    CreatedAt     DATETIME2     NOT NULL
);
CREATE UNIQUE INDEX IX_Admins_Username ON Admins(Username);
GO

/* ---------- Members : registered users (personal + professional data) ---------- */
CREATE TABLE Members (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Members PRIMARY KEY,
    FullName         NVARCHAR(100) NOT NULL,
    Email            NVARCHAR(150) NOT NULL,
    PasswordHash     NVARCHAR(200) NOT NULL,
    Phone            NVARCHAR(20)  NULL,
    Gender           NVARCHAR(10)  NULL,
    DateOfBirth      DATETIME2     NULL,
    Address          NVARCHAR(250) NULL,
    City             NVARCHAR(60)  NULL,
    Country          NVARCHAR(60)  NULL,
    Occupation       NVARCHAR(100) NULL,
    Organization     NVARCHAR(150) NULL,
    Qualification    NVARCHAR(100) NULL,
    AreasOfInterest  NVARCHAR(300) NULL,
    RegisteredAt     DATETIME2     NOT NULL,
    IsActive         BIT           NOT NULL
);
CREATE UNIQUE INDEX IX_Members_Email ON Members(Email);
GO

/* ---------- Causes : donation categories ---------- */
CREATE TABLE Causes (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Causes PRIMARY KEY,
    Name          NVARCHAR(80)   NOT NULL,
    Description   NVARCHAR(500)  NOT NULL,
    Icon          NVARCHAR(50)   NOT NULL,
    GoalAmount    DECIMAL(18,2)  NOT NULL,
    DisplayOrder  INT            NOT NULL,
    IsActive      BIT            NOT NULL
);
GO

/* ---------- Donations : dummy payment transactions ---------- */
CREATE TABLE Donations (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Donations PRIMARY KEY,
    TransactionRef  NVARCHAR(30)  NOT NULL,
    CauseId         INT           NOT NULL,
    MemberId        INT           NULL,          -- NULL = guest donation
    DonorName       NVARCHAR(100) NOT NULL,
    DonorEmail      NVARCHAR(150) NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    CardType        NVARCHAR(10)  NOT NULL,      -- Credit / Debit
    CardBrand       NVARCHAR(20)  NOT NULL,      -- Visa / MasterCard / ...
    CardHolder      NVARCHAR(100) NOT NULL,
    CardLast4       NVARCHAR(4)   NOT NULL,      -- only the last 4 digits are stored
    Status          NVARCHAR(20)  NOT NULL,
    Message         NVARCHAR(300) NULL,
    IsAnonymous     BIT           NOT NULL,
    DonatedAt       DATETIME2     NOT NULL,
    CONSTRAINT FK_Donations_Causes_CauseId   FOREIGN KEY (CauseId)  REFERENCES Causes(Id),
    CONSTRAINT FK_Donations_Members_MemberId FOREIGN KEY (MemberId) REFERENCES Members(Id)
);
CREATE UNIQUE INDEX IX_Donations_TransactionRef ON Donations(TransactionRef);
CREATE INDEX IX_Donations_CauseId  ON Donations(CauseId);
CREATE INDEX IX_Donations_MemberId ON Donations(MemberId);
GO

/* ---------- Partners : companies associated with Give-AID ---------- */
CREATE TABLE Partners (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Partners PRIMARY KEY,
    Name          NVARCHAR(120) NOT NULL,
    PartnerType   NVARCHAR(40)  NOT NULL,
    Description   NVARCHAR(400) NULL,
    Website       NVARCHAR(200) NULL,
    LogoPath      NVARCHAR(250) NULL,
    DisplayOrder  INT           NOT NULL
);
GO

/* ---------- Ngos : associated NGOs ---------- */
CREATE TABLE Ngos (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ngos PRIMARY KEY,
    Name             NVARCHAR(150)  NOT NULL,
    RegistrationNo   NVARCHAR(50)   NULL,
    FocusArea        NVARCHAR(100)  NOT NULL,
    City             NVARCHAR(60)   NULL,
    EstablishedYear  INT            NULL,
    ContactPerson    NVARCHAR(100)  NULL,
    Email            NVARCHAR(150)  NULL,
    Phone            NVARCHAR(20)   NULL,
    Website          NVARCHAR(200)  NULL,
    Description      NVARCHAR(1500) NOT NULL,
    LogoPath         NVARCHAR(250)  NULL,
    AddedAt          DATETIME2      NOT NULL
);
GO

/* ---------- Programmes : events run by the NGOs ---------- */
CREATE TABLE Programmes (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Programmes PRIMARY KEY,
    Title      NVARCHAR(150)  NOT NULL,
    Category   NVARCHAR(40)   NOT NULL,
    NgoId      INT            NULL,
    Venue      NVARCHAR(150)  NOT NULL,
    StartDate  DATETIME2      NOT NULL,
    EndDate    DATETIME2      NULL,
    Summary    NVARCHAR(300)  NOT NULL,
    Details    NVARCHAR(4000) NOT NULL,
    ImagePath  NVARCHAR(250)  NULL,
    Beneficiaries INT         NULL,             -- people served (used for budget estimates)
    IsPublished   BIT         NOT NULL CONSTRAINT DF_Programmes_IsPublished DEFAULT (1),  -- 0 = hidden draft
    CONSTRAINT FK_Programmes_Ngos_NgoId FOREIGN KEY (NgoId) REFERENCES Ngos(Id)
);
CREATE INDEX IX_Programmes_NgoId ON Programmes(NgoId);
GO

/* ---------- ProgrammeInterests : users who want to take part ---------- */
CREATE TABLE ProgrammeInterests (
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProgrammeInterests PRIMARY KEY,
    ProgrammeId        INT           NOT NULL,
    MemberId           INT           NULL,
    Name               NVARCHAR(100) NOT NULL,
    Email              NVARCHAR(150) NOT NULL,
    Phone              NVARCHAR(20)  NULL,
    ParticipationType  NVARCHAR(40)  NOT NULL,   -- Volunteer / Participant / Sponsor
    Message            NVARCHAR(500) NULL,
    SubmittedAt        DATETIME2     NOT NULL,
    CONSTRAINT FK_ProgrammeInterests_Programmes_ProgrammeId FOREIGN KEY (ProgrammeId) REFERENCES Programmes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProgrammeInterests_Members_MemberId       FOREIGN KEY (MemberId)    REFERENCES Members(Id)
);
CREATE INDEX IX_ProgrammeInterests_ProgrammeId ON ProgrammeInterests(ProgrammeId);
CREATE INDEX IX_ProgrammeInterests_MemberId    ON ProgrammeInterests(MemberId);
GO

/* ---------- GalleryImages : photos of programmes ---------- */
CREATE TABLE GalleryImages (
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GalleryImages PRIMARY KEY,
    Title        NVARCHAR(150) NOT NULL,
    Caption      NVARCHAR(300) NULL,
    ImagePath    NVARCHAR(250) NOT NULL,
    ProgrammeId  INT           NULL,
    UploadedAt   DATETIME2     NOT NULL,
    CONSTRAINT FK_GalleryImages_Programmes_ProgrammeId FOREIGN KEY (ProgrammeId) REFERENCES Programmes(Id)
);
CREATE INDEX IX_GalleryImages_ProgrammeId ON GalleryImages(ProgrammeId);
GO

/* ---------- Queries : questions from users, answered by admin ---------- */
CREATE TABLE Queries (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Queries PRIMARY KEY,
    MemberId   INT            NULL,
    Name       NVARCHAR(100)  NOT NULL,
    Email      NVARCHAR(150)  NOT NULL,
    Subject    NVARCHAR(150)  NOT NULL,
    Message    NVARCHAR(2000) NOT NULL,
    Reply      NVARCHAR(2000) NULL,
    Status     NVARCHAR(20)   NOT NULL,          -- Open / Answered
    CreatedAt  DATETIME2      NOT NULL,
    RepliedAt  DATETIME2      NULL,
    CONSTRAINT FK_Queries_Members_MemberId FOREIGN KEY (MemberId) REFERENCES Members(Id)
);
CREATE INDEX IX_Queries_MemberId ON Queries(MemberId);
GO

/* ---------- Invitations : friends invited by members ---------- */
CREATE TABLE Invitations (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invitations PRIMARY KEY,
    MemberId         INT            NOT NULL,
    FriendName       NVARCHAR(100)  NOT NULL,
    FriendEmail      NVARCHAR(150)  NOT NULL,
    PersonalMessage  NVARCHAR(1000) NULL,
    EmailSent        BIT            NOT NULL,
    SentAt           DATETIME2      NOT NULL,
    CONSTRAINT FK_Invitations_Members_MemberId FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE
);
CREATE INDEX IX_Invitations_MemberId ON Invitations(MemberId);
GO

/* ---------- ContentPages : editable About Us / Help / Contact / Home text ---------- */
CREATE TABLE ContentPages (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContentPages PRIMARY KEY,
    Slug          NVARCHAR(60)  NOT NULL,
    Section       NVARCHAR(30)  NOT NULL,
    Title         NVARCHAR(120) NOT NULL,
    Subtitle      NVARCHAR(250) NULL,
    Body          NVARCHAR(MAX) NOT NULL,
    DisplayOrder  INT           NOT NULL,
    UpdatedAt     DATETIME2     NOT NULL
);
CREATE UNIQUE INDEX IX_ContentPages_Slug ON ContentPages(Slug);
GO

/* ---------- ProgrammeCostLines : budget / spending lines of each programme ---------- */
CREATE TABLE ProgrammeCostLines (
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProgrammeCostLines PRIMARY KEY,
    ProgrammeId  INT            NOT NULL,
    Item         NVARCHAR(100)  NOT NULL,
    Amount       DECIMAL(18,2)  NOT NULL,
    CONSTRAINT FK_ProgrammeCostLines_Programmes_ProgrammeId FOREIGN KEY (ProgrammeId) REFERENCES Programmes(Id) ON DELETE CASCADE
);
CREATE INDEX IX_ProgrammeCostLines_ProgrammeId ON ProgrammeCostLines(ProgrammeId);
GO

/* ---------- PlannerSessions : one run of the AI Programme Planner ---------- */
CREATE TABLE PlannerSessions (
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlannerSessions PRIMARY KEY,
    Goal         NVARCHAR(1000) NOT NULL,        -- the admin's goal in plain words
    Status       NVARCHAR(30)   NOT NULL,        -- Planning / AwaitingApproval / Completed / Cancelled / Failed
    Mode         NVARCHAR(20)   NOT NULL,        -- Demo / Claude / OpenAI
    Summary      NVARCHAR(4000) NULL,
    ProgrammeId  INT            NULL,            -- draft programme created after approval
    CreatedBy    NVARCHAR(100)  NOT NULL,
    CreatedAt    DATETIME2      NOT NULL,
    CompletedAt  DATETIME2      NULL,
    CONSTRAINT FK_PlannerSessions_Programmes_ProgrammeId FOREIGN KEY (ProgrammeId) REFERENCES Programmes(Id)
);
CREATE INDEX IX_PlannerSessions_ProgrammeId ON PlannerSessions(ProgrammeId);
GO

/* ---------- PlannerSteps : every action the planner took or proposed (audit trail) ---------- */
CREATE TABLE PlannerSteps (
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlannerSteps PRIMARY KEY,
    SessionId         INT            NOT NULL,
    [Order]           INT            NOT NULL,
    Tool              NVARCHAR(50)   NOT NULL,   -- e.g. find_ngos, propose_draft_programme
    Title             NVARCHAR(150)  NOT NULL,
    Explanation       NVARCHAR(2000) NULL,
    InputJson         NVARCHAR(MAX)  NOT NULL,
    ResultJson        NVARCHAR(MAX)  NULL,
    Sources           NVARCHAR(300)  NULL,
    RequiresApproval  BIT            NOT NULL,   -- 1 = write action, waits for an admin
    Status            NVARCHAR(20)   NOT NULL,   -- Done / Pending / Approved / Rejected / Failed / NeedsReview
    DecidedBy         NVARCHAR(100)  NULL,
    DecidedAt         DATETIME2      NULL,
    Note              NVARCHAR(1000) NULL,
    CreatedAt         DATETIME2      NOT NULL,
    CONSTRAINT FK_PlannerSteps_PlannerSessions_SessionId FOREIGN KEY (SessionId) REFERENCES PlannerSessions(Id) ON DELETE CASCADE
);
CREATE INDEX IX_PlannerSteps_SessionId ON PlannerSteps(SessionId);
GO

/* =====================================================================
   Useful report queries (for viva / testing)
   ===================================================================== */

-- Total raised per cause
-- SELECT c.Name, COUNT(d.Id) AS Donations, ISNULL(SUM(d.Amount),0) AS Raised, c.GoalAmount
-- FROM Causes c LEFT JOIN Donations d ON d.CauseId = c.Id
-- GROUP BY c.Name, c.GoalAmount ORDER BY Raised DESC;

-- Open queries waiting for a reply
-- SELECT Name, Email, Subject, CreatedAt FROM Queries WHERE Status = 'Open' ORDER BY CreatedAt;

-- Take a backup for the eProject submission
-- BACKUP DATABASE GiveAidDB TO DISK = 'C:\Backup\GiveAidDB.bak' WITH FORMAT, INIT;
