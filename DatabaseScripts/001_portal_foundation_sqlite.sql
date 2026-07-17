-- المرحلة الأولى: جداول بوابة الداعمين وبوابة الشركات والجهات - SQLite
-- نفذ هذا الملف فقط إذا لم تستخدم EF Core Migrations.

CREATE TABLE IF NOT EXISTS "DonorAccounts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DonorAccounts" PRIMARY KEY AUTOINCREMENT,
    "FullName" TEXT NOT NULL,
    "DonorType" TEXT NOT NULL DEFAULT 'فرد',
    "OrganizationName" TEXT NULL,
    "Email" TEXT NULL,
    "Phone" TEXT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PasswordSalt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    "LastLoginAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_DonorAccounts_Email" ON "DonorAccounts" ("Email");

CREATE TABLE IF NOT EXISTS "DonorContributions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DonorContributions" PRIMARY KEY AUTOINCREMENT,
    "DonorAccountId" INTEGER NOT NULL,
    "ProgramProjectId" INTEGER NULL,
    "Title" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "ProgressPercent" INTEGER NOT NULL,
    "TotalAmount" TEXT NOT NULL,
    "SpentAmount" TEXT NOT NULL,
    "RemainingAmount" TEXT NOT NULL,
    "BeneficiariesCount" INTEGER NOT NULL,
    "ImpactSummary" TEXT NULL,
    "HasSurplus" INTEGER NOT NULL DEFAULT 0,
    "IsSurplusLocked" INTEGER NOT NULL DEFAULT 1,
    "StartedAt" TEXT NULL,
    "CompletedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DonorContributions_DonorAccounts_DonorAccountId" FOREIGN KEY ("DonorAccountId") REFERENCES "DonorAccounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DonorContributions_ProgramProjects_ProgramProjectId" FOREIGN KEY ("ProgramProjectId") REFERENCES "ProgramProjects" ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_DonorContributions_DonorAccountId" ON "DonorContributions" ("DonorAccountId");
CREATE INDEX IF NOT EXISTS "IX_DonorContributions_ProgramProjectId" ON "DonorContributions" ("ProgramProjectId");

CREATE TABLE IF NOT EXISTS "DonorContributionUpdates" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DonorContributionUpdates" PRIMARY KEY AUTOINCREMENT,
    "DonorContributionId" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "Details" TEXT NULL,
    "ProgressPercent" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DonorContributionUpdates_DonorContributions_DonorContributionId" FOREIGN KEY ("DonorContributionId") REFERENCES "DonorContributions" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_DonorContributionUpdates_DonorContributionId" ON "DonorContributionUpdates" ("DonorContributionId");

CREATE TABLE IF NOT EXISTS "DonorReports" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DonorReports" PRIMARY KEY AUTOINCREMENT,
    "DonorContributionId" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "Summary" TEXT NULL,
    "FilePath" TEXT NULL,
    "ReportType" TEXT NOT NULL,
    "ReportDate" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DonorReports_DonorContributions_DonorContributionId" FOREIGN KEY ("DonorContributionId") REFERENCES "DonorContributions" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_DonorReports_DonorContributionId" ON "DonorReports" ("DonorContributionId");

CREATE TABLE IF NOT EXISTS "DonorSurplusDecisions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DonorSurplusDecisions" PRIMARY KEY AUTOINCREMENT,
    "DonorContributionId" INTEGER NOT NULL,
    "SurplusAmount" TEXT NOT NULL,
    "DecisionType" TEXT NOT NULL,
    "Notes" TEXT NULL,
    "Status" TEXT NOT NULL,
    "IpAddress" TEXT NULL,
    "ApprovedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DonorSurplusDecisions_DonorContributions_DonorContributionId" FOREIGN KEY ("DonorContributionId") REFERENCES "DonorContributions" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_DonorSurplusDecisions_DonorContributionId" ON "DonorSurplusDecisions" ("DonorContributionId");

CREATE TABLE IF NOT EXISTS "DonorNotifications" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DonorNotifications" PRIMARY KEY AUTOINCREMENT,
    "DonorAccountId" INTEGER NOT NULL,
    "DonorContributionId" INTEGER NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "IsRead" INTEGER NOT NULL DEFAULT 0,
    "SentBySms" INTEGER NOT NULL DEFAULT 0,
    "SentByEmail" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DonorNotifications_DonorAccounts_DonorAccountId" FOREIGN KEY ("DonorAccountId") REFERENCES "DonorAccounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DonorNotifications_DonorContributions_DonorContributionId" FOREIGN KEY ("DonorContributionId") REFERENCES "DonorContributions" ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_DonorNotifications_DonorAccountId" ON "DonorNotifications" ("DonorAccountId");
CREATE INDEX IF NOT EXISTS "IX_DonorNotifications_DonorContributionId" ON "DonorNotifications" ("DonorContributionId");

CREATE TABLE IF NOT EXISTS "OrganizationAccounts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_OrganizationAccounts" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "LogoPath" TEXT NULL,
    "Activity" TEXT NULL,
    "City" TEXT NULL,
    "ContactName" TEXT NULL,
    "Email" TEXT NULL,
    "Phone" TEXT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PasswordSalt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    "LastLoginAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrganizationAccounts_Email" ON "OrganizationAccounts" ("Email");

CREATE TABLE IF NOT EXISTS "OpportunityRequests" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_OpportunityRequests" PRIMARY KEY AUTOINCREMENT,
    "OrganizationAccountId" INTEGER NOT NULL,
    "OpportunityType" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "AvailableCount" INTEGER NOT NULL,
    "City" TEXT NULL,
    "WorkLocation" TEXT NULL,
    "Qualifications" TEXT NULL,
    "Skills" TEXT NULL,
    "SuitableDisabilityTypes" TEXT NULL,
    "WorkNature" TEXT NOT NULL,
    "WorkHours" TEXT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_OpportunityRequests_OrganizationAccounts_OrganizationAccountId" FOREIGN KEY ("OrganizationAccountId") REFERENCES "OrganizationAccounts" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_OpportunityRequests_OrganizationAccountId" ON "OpportunityRequests" ("OrganizationAccountId");

CREATE TABLE IF NOT EXISTS "OpportunityCandidates" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_OpportunityCandidates" PRIMARY KEY AUTOINCREMENT,
    "OpportunityRequestId" INTEGER NOT NULL,
    "CandidateName" TEXT NOT NULL,
    "CvFilePath" TEXT NULL,
    "Qualifications" TEXT NULL,
    "Skills" TEXT NULL,
    "OrganizationNotes" TEXT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_OpportunityCandidates_OpportunityRequests_OpportunityRequestId" FOREIGN KEY ("OpportunityRequestId") REFERENCES "OpportunityRequests" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_OpportunityCandidates_OpportunityRequestId" ON "OpportunityCandidates" ("OpportunityRequestId");

CREATE TABLE IF NOT EXISTS "OrganizationEvaluations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_OrganizationEvaluations" PRIMARY KEY AUTOINCREMENT,
    "OrganizationAccountId" INTEGER NOT NULL,
    "OpportunityRequestId" INTEGER NULL,
    "CandidateQualityRate" INTEGER NOT NULL,
    "ServiceRate" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_OrganizationEvaluations_OrganizationAccounts_OrganizationAccountId" FOREIGN KEY ("OrganizationAccountId") REFERENCES "OrganizationAccounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrganizationEvaluations_OpportunityRequests_OpportunityRequestId" FOREIGN KEY ("OpportunityRequestId") REFERENCES "OpportunityRequests" ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrganizationEvaluations_OrganizationAccountId" ON "OrganizationEvaluations" ("OrganizationAccountId");
CREATE INDEX IF NOT EXISTS "IX_OrganizationEvaluations_OpportunityRequestId" ON "OrganizationEvaluations" ("OpportunityRequestId");

CREATE TABLE IF NOT EXISTS "OrganizationNotifications" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_OrganizationNotifications" PRIMARY KEY AUTOINCREMENT,
    "OrganizationAccountId" INTEGER NOT NULL,
    "OpportunityRequestId" INTEGER NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "IsRead" INTEGER NOT NULL DEFAULT 0,
    "SentBySms" INTEGER NOT NULL DEFAULT 0,
    "SentByEmail" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_OrganizationNotifications_OrganizationAccounts_OrganizationAccountId" FOREIGN KEY ("OrganizationAccountId") REFERENCES "OrganizationAccounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrganizationNotifications_OpportunityRequests_OpportunityRequestId" FOREIGN KEY ("OpportunityRequestId") REFERENCES "OpportunityRequests" ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_OrganizationNotifications_OrganizationAccountId" ON "OrganizationNotifications" ("OrganizationAccountId");
CREATE INDEX IF NOT EXISTS "IX_OrganizationNotifications_OpportunityRequestId" ON "OrganizationNotifications" ("OpportunityRequestId");

INSERT INTO "DonorAccounts" ("FullName", "DonorType", "Email", "Phone", "PasswordHash", "PasswordSalt", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'داعم تجريبي', 'فرد', 'donor@kafo.local', '0500000001', 'Vnm35uU4NGyTof1Z7pubVKZ1ZDKatR7HQ8r5NirPj6k=', 'x/lMSUy0QwQKYBDYABDdDg==', 1, datetime('now'), datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM "DonorAccounts" WHERE "Email" = 'donor@kafo.local');

INSERT INTO "OrganizationAccounts" ("Name", "Activity", "City", "ContactName", "Email", "Phone", "PasswordHash", "PasswordSalt", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'جهة تجريبية', 'الموارد البشرية والتوظيف', 'الرياض', 'مسؤول الموارد البشرية', 'org@kafo.local', '0500000002', 'Rtdule6bvtT0ASXRK4jB6WF3s7zxS6H8gNgtgjXLoXc=', '3FbeDfY/PmKzLroPKnEXdQ==', 1, datetime('now'), datetime('now')
WHERE NOT EXISTS (SELECT 1 FROM "OrganizationAccounts" WHERE "Email" = 'org@kafo.local');
