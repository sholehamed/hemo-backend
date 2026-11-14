/* HEMO - Physical Database Baseline for Microsoft SQL Server 2019/2022
   Source: private hemo-raw/main, analyzed 2026-10-30.
   IMPORTANT: proposed production-ready baseline based on prototype; clinical rules need validation.
   All dates/times are stored Gregorian and UTC (DATETIME2(3)); local Jalali conversion belongs to UI.
   Never store plaintext credentials. This script creates structures only; no user or patient seed data.
   Run on a new/empty instance with permissions to CREATE DATABASE. Rerunning is not a migration.
*/
IF DB_ID(N'Hemo') IS NULL EXEC(N'CREATE DATABASE [Hemo]');
GO
USE [Hemo];
GO
-- Safety: baseline must never be replayed over an existing schema.
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 51001, 'Hemo baseline requires an empty database; use versioned migrations for upgrades.', 1;
GO

IF SCHEMA_ID(N'org') IS NULL EXEC(N'CREATE SCHEMA [org]');
GO

IF SCHEMA_ID(N'sec') IS NULL EXEC(N'CREATE SCHEMA [sec]');
GO

IF SCHEMA_ID(N'patient') IS NULL EXEC(N'CREATE SCHEMA [patient]');
GO

IF SCHEMA_ID(N'clinical') IS NULL EXEC(N'CREATE SCHEMA [clinical]');
GO

IF SCHEMA_ID(N'blood') IS NULL EXEC(N'CREATE SCHEMA [blood]');
GO

IF SCHEMA_ID(N'equip') IS NULL EXEC(N'CREATE SCHEMA [equip]');
GO

IF SCHEMA_ID(N'hv') IS NULL EXEC(N'CREATE SCHEMA [hv]');
GO

IF SCHEMA_ID(N'integration') IS NULL EXEC(N'CREATE SCHEMA [integration]');
GO

IF SCHEMA_ID(N'report') IS NULL EXEC(N'CREATE SCHEMA [report]');
GO

IF SCHEMA_ID(N'cfg') IS NULL EXEC(N'CREATE SCHEMA [cfg]');
GO

IF SCHEMA_ID(N'audit') IS NULL EXEC(N'CREATE SCHEMA [audit]');
GO

CREATE TABLE [org].[Facility] (
    [FacilityId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(30) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [FacilityType] VARCHAR(25) NOT NULL CONSTRAINT DF_org_Facility_Type DEFAULT ('Hospital'),
    [HisFacilityCode] NVARCHAR(60) NULL,
    [Province] NVARCHAR(100) NULL,
    [City] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_org_Facility_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_org_Facility_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_org_Facility] PRIMARY KEY CLUSTERED ([FacilityId] ASC),
    CONSTRAINT [CK_org_Facility_1] CHECK ([FacilityType] IN ('Hospital','BloodCenter','Other'))
);
GO

CREATE TABLE [org].[Department] (
    [DepartmentId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [ParentDepartmentId] BIGINT NULL,
    [Code] NVARCHAR(40) NOT NULL,
    [Name] NVARCHAR(160) NOT NULL,
    [DepartmentType] VARCHAR(30) NOT NULL CONSTRAINT DF_org_Department_Type DEFAULT ('Clinical'),
    [IsActive] BIT NOT NULL CONSTRAINT DF_org_Department_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_org_Department_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_org_Department] PRIMARY KEY CLUSTERED ([DepartmentId] ASC)
);
GO

CREATE TABLE [org].[Bed] (
    [BedId] BIGINT IDENTITY(1,1) NOT NULL,
    [DepartmentId] BIGINT NOT NULL,
    [BedCode] NVARCHAR(30) NOT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_org_Bed_Status DEFAULT ('Active'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_org_Bed_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_org_Bed] PRIMARY KEY CLUSTERED ([BedId] ASC),
    CONSTRAINT [CK_org_Bed_1] CHECK ([Status] IN ('Active','Blocked','OutOfService'))
);
GO

CREATE TABLE [org].[DutyShift] (
    [DutyShiftId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [DepartmentId] BIGINT NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [StartsAt] DATETIME2(3) NOT NULL,
    [EndsAt] DATETIME2(3) NOT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_org_DutyShift_Status DEFAULT ('Planned'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_org_DutyShift_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_org_DutyShift] PRIMARY KEY CLUSTERED ([DutyShiftId] ASC),
    CONSTRAINT [CK_org_DutyShift_1] CHECK ([EndsAt]>[StartsAt]),
    CONSTRAINT [CK_org_DutyShift_2] CHECK ([Status] IN ('Planned','Open','Closed'))
);
GO

CREATE TABLE [sec].[AccountUser] (
    [AccountUserId] BIGINT IDENTITY(1,1) NOT NULL,
    [UserName] NVARCHAR(100) NOT NULL,
    [DisplayName] NVARCHAR(180) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [ExternalIdentityId] NVARCHAR(180) NULL,
    [Email] NVARCHAR(254) NULL,
    [PersonnelCode] NVARCHAR(70) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_sec_AccountUser_Active DEFAULT (1),
    [LastLoginAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_AccountUser_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_sec_AccountUser] PRIMARY KEY CLUSTERED ([AccountUserId] ASC)
);
GO

CREATE TABLE [sec].[Role] (
    [RoleId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(45) NOT NULL,
    [Name] NVARCHAR(130) NOT NULL,
    [IsSystem] BIT NOT NULL CONSTRAINT DF_sec_Role_IsSystem DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_Role_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_sec_Role] PRIMARY KEY CLUSTERED ([RoleId] ASC)
);
GO

CREATE TABLE [sec].[Permission] (
    [PermissionId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(120) NOT NULL,
    [Description] NVARCHAR(260) NOT NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_Permission_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_sec_Permission] PRIMARY KEY CLUSTERED ([PermissionId] ASC)
);
GO

CREATE TABLE [sec].[RolePermission] (
    [RolePermissionId] BIGINT IDENTITY(1,1) NOT NULL,
    [RoleId] BIGINT NOT NULL,
    [PermissionId] BIGINT NOT NULL,
    CONSTRAINT [PK_sec_RolePermission] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC)
);
GO

CREATE TABLE [sec].[UserRole] (
    [UserRoleId] BIGINT IDENTITY(1,1) NOT NULL,
    [AccountUserId] BIGINT NOT NULL,
    [RoleId] BIGINT NOT NULL,
    [FacilityId] BIGINT NULL,
    [DepartmentId] BIGINT NULL,
    [EffectiveFrom] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_UserRole_From DEFAULT (SYSUTCDATETIME()),
    [EffectiveUntil] DATETIME2(3) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_sec_UserRole_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_UserRole_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_sec_UserRole] PRIMARY KEY CLUSTERED ([UserRoleId] ASC),
    CONSTRAINT [CK_sec_UserRole_1] CHECK ([EffectiveUntil] IS NULL OR [EffectiveUntil]>[EffectiveFrom]),
    CONSTRAINT [CK_sec_UserRole_2] CHECK ([DepartmentId] IS NULL OR [FacilityId] IS NOT NULL)
);
GO

CREATE TABLE [sec].[UserSession] (
    [UserSessionId] BIGINT IDENTITY(1,1) NOT NULL,
    [AccountUserId] BIGINT NOT NULL,
    [RefreshTokenHash] VARBINARY(32) NOT NULL,
    [IssuedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_UserSession_Issued DEFAULT (SYSUTCDATETIME()),
    [ExpiresAt] DATETIME2(3) NOT NULL,
    [RevokedAt] DATETIME2(3) NULL,
    [ClientIp] VARCHAR(45) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_sec_UserSession_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_sec_UserSession] PRIMARY KEY CLUSTERED ([UserSessionId] ASC),
    CONSTRAINT [CK_sec_UserSession_1] CHECK ([ExpiresAt]>[IssuedAt])
);
GO

CREATE TABLE [patient].[Patient] (
    [PatientId] BIGINT IDENTITY(1,1) NOT NULL,
    [NationalId] VARCHAR(10) NULL,
    [UnknownIdentityCode] NVARCHAR(70) NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(120) NOT NULL,
    [FatherName] NVARCHAR(100) NULL,
    [BirthDate] DATE NULL,
    [Sex] CHAR(1) NOT NULL CONSTRAINT DF_patient_Patient_Sex DEFAULT ('U'),
    [Mobile] NVARCHAR(30) NULL,
    [IsUnknown] BIT NOT NULL CONSTRAINT DF_patient_Patient_Unknown DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT DF_patient_Patient_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_Patient_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_Patient] PRIMARY KEY CLUSTERED ([PatientId] ASC),
    CONSTRAINT [CK_patient_Patient_1] CHECK ([Sex] IN ('M','F','U')),
    CONSTRAINT [CK_patient_Patient_2] CHECK ([NationalId] IS NULL OR ([NationalId] NOT LIKE '%[^0-9]%' AND LEN([NationalId])=10)),
    CONSTRAINT [CK_patient_Patient_3] CHECK ([IsUnknown]=0 OR [UnknownIdentityCode] IS NOT NULL)
);
GO

CREATE TABLE [patient].[PatientIdentifier] (
    [PatientIdentifierId] BIGINT IDENTITY(1,1) NOT NULL,
    [PatientId] BIGINT NOT NULL,
    [FacilityId] BIGINT NULL,
    [IdentifierType] VARCHAR(30) NOT NULL,
    [IdentifierValue] NVARCHAR(90) NOT NULL,
    [SourceSystem] NVARCHAR(60) NULL,
    [IsVerified] BIT NOT NULL CONSTRAINT DF_patient_PatientIdentifier_Verified DEFAULT (0),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_PatientIdentifier_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_PatientIdentifier] PRIMARY KEY CLUSTERED ([PatientIdentifierId] ASC)
);
GO

CREATE TABLE [patient].[Encounter] (
    [EncounterId] BIGINT IDENTITY(1,1) NOT NULL,
    [PatientId] BIGINT NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [HisEncounterNo] NVARCHAR(80) NULL,
    [AdmissionType] VARCHAR(25) NOT NULL CONSTRAINT DF_patient_Encounter_AdmissionType DEFAULT ('Routine'),
    [AdmittedAt] DATETIME2(3) NOT NULL,
    [DischargedAt] DATETIME2(3) NULL,
    [AttendingPhysicianId] BIGINT NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_patient_Encounter_Status DEFAULT ('Admitted'),
    [AdmissionNote] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_Encounter_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_Encounter] PRIMARY KEY CLUSTERED ([EncounterId] ASC),
    CONSTRAINT [CK_patient_Encounter_1] CHECK ([AdmissionType] IN ('Routine','Emergency','Transfer','Other')),
    CONSTRAINT [CK_patient_Encounter_2] CHECK ([Status] IN ('Admitted','DischargePending','Discharged','Cancelled')),
    CONSTRAINT [CK_patient_Encounter_3] CHECK ([DischargedAt] IS NULL OR [DischargedAt]>=[AdmittedAt])
);
GO

CREATE TABLE [patient].[EncounterLocation] (
    [EncounterLocationId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [DepartmentId] BIGINT NOT NULL,
    [BedId] BIGINT NULL,
    [FromAt] DATETIME2(3) NOT NULL,
    [ToAt] DATETIME2(3) NULL,
    [RecordedByUserId] BIGINT NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_EncounterLocation_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_EncounterLocation] PRIMARY KEY CLUSTERED ([EncounterLocationId] ASC),
    CONSTRAINT [CK_patient_EncounterLocation_1] CHECK ([ToAt] IS NULL OR [ToAt]>[FromAt])
);
GO

CREATE TABLE [patient].[Wristband] (
    [WristbandId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [Barcode] NVARCHAR(100) NOT NULL,
    [PrintedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_Wristband_Printed DEFAULT (SYSUTCDATETIME()),
    [PrintedByUserId] BIGINT NOT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_patient_Wristband_Status DEFAULT ('Active'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_Wristband_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_Wristband] PRIMARY KEY CLUSTERED ([WristbandId] ASC),
    CONSTRAINT [CK_patient_Wristband_1] CHECK ([Status] IN ('Active','Replaced','Voided'))
);
GO

CREATE TABLE [patient].[IdentityVerification] (
    [IdentityVerificationId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [WristbandId] BIGINT NULL,
    [VerifiedByUserId] BIGINT NOT NULL,
    [VerifiedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_IdentityVerification_At DEFAULT (SYSUTCDATETIME()),
    [FirstIdentifier] NVARCHAR(100) NOT NULL,
    [SecondIdentifier] NVARCHAR(100) NOT NULL,
    [WristbandMatched] BIT NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    [MismatchReason] NVARCHAR(800) NULL,
    [ChecklistJson] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_IdentityVerification_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_IdentityVerification] PRIMARY KEY CLUSTERED ([IdentityVerificationId] ASC),
    CONSTRAINT [CK_patient_IdentityVerification_1] CHECK ([Status] IN ('Verified','Mismatch','Pending')),
    CONSTRAINT [CK_patient_IdentityVerification_2] CHECK ([ChecklistJson] IS NULL OR ISJSON([ChecklistJson])=1)
);
GO

CREATE TABLE [patient].[HistoricalTransfusion] (
    [HistoricalTransfusionId] BIGINT IDENTITY(1,1) NOT NULL,
    [PatientId] BIGINT NOT NULL,
    [EncounterId] BIGINT NULL,
    [ExternalRecordNo] NVARCHAR(100) NULL,
    [OccurredAt] DATETIME2(3) NULL,
    [ProductDescription] NVARCHAR(160) NULL,
    [BloodGroupText] NVARCHAR(12) NULL,
    [HadReaction] BIT NOT NULL CONSTRAINT DF_patient_HistoricalTransfusion_Reaction DEFAULT (0),
    [ReactionSummary] NVARCHAR(1500) NULL,
    [SourceSystem] NVARCHAR(100) NULL,
    [ImportedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_HistoricalTransfusion_Imported DEFAULT (SYSUTCDATETIME()),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_HistoricalTransfusion_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_HistoricalTransfusion] PRIMARY KEY CLUSTERED ([HistoricalTransfusionId] ASC)
);
GO

CREATE TABLE [patient].[DischargeReview] (
    [DischargeReviewId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [ReviewerUserId] BIGINT NOT NULL,
    [ReviewedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_patient_DischargeReview_Status DEFAULT ('Draft'),
    [Remarks] NVARCHAR(1500) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_DischargeReview_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_DischargeReview] PRIMARY KEY CLUSTERED ([DischargeReviewId] ASC),
    CONSTRAINT [CK_patient_DischargeReview_1] CHECK ([Status] IN ('Draft','Blocked','Cleared'))
);
GO

CREATE TABLE [patient].[DischargeReviewItem] (
    [DischargeReviewItemId] BIGINT IDENTITY(1,1) NOT NULL,
    [DischargeReviewId] BIGINT NOT NULL,
    [ItemCode] VARCHAR(60) NOT NULL,
    [IsPassed] BIT NOT NULL,
    [Comment] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_patient_DischargeReviewItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_patient_DischargeReviewItem] PRIMARY KEY CLUSTERED ([DischargeReviewItemId] ASC)
);
GO

CREATE TABLE [clinical].[BloodRequest] (
    [BloodRequestId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [RequestNo] NVARCHAR(55) NOT NULL,
    [RequestedByUserId] BIGINT NOT NULL,
    [RequestedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_BloodRequest_At DEFAULT (SYSUTCDATETIME()),
    [RequestSource] VARCHAR(25) NOT NULL,
    [RequestType] VARCHAR(35) NOT NULL,
    [Priority] VARCHAR(20) NOT NULL CONSTRAINT DF_clinical_BloodRequest_Priority DEFAULT ('Routine'),
    [Status] VARCHAR(35) NOT NULL CONSTRAINT DF_clinical_BloodRequest_Status DEFAULT ('Draft'),
    [Indication] NVARCHAR(2000) NULL,
    [Diagnosis] NVARCHAR(1000) NULL,
    [ExpectedAt] DATETIME2(3) NULL,
    [ExternalRequestNo] NVARCHAR(75) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_BloodRequest_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_BloodRequest] PRIMARY KEY CLUSTERED ([BloodRequestId] ASC),
    CONSTRAINT [CK_clinical_BloodRequest_1] CHECK ([RequestSource] IN ('Nurse','Doctor','Bank','HIS')),
    CONSTRAINT [CK_clinical_BloodRequest_2] CHECK ([Priority] IN ('Routine','Urgent','Emergency')),
    CONSTRAINT [CK_clinical_BloodRequest_3] CHECK ([Status] IN ('Draft','Submitted','AwaitingDoctor','AwaitingBank','Approved','Crossmatch','Ready','PartiallyIssued','Issued','Completed','Rejected','Cancelled'))
);
GO

CREATE TABLE [clinical].[RequestClinicalData] (
    [RequestClinicalDataId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodRequestId] BIGINT NOT NULL,
    [Hemoglobin] DECIMAL(5,2) NULL,
    [Hematocrit] DECIMAL(5,2) NULL,
    [HasPreviousTransfusion] BIT NULL,
    [HasPreviousReaction] BIT NULL,
    [HasAntibodyHistory] BIT NULL,
    [PregnancyHistory] NVARCHAR(500) NULL,
    [PremedicationRequired] BIT NULL,
    [PremedicationDetails] NVARCHAR(1000) NULL,
    [EmergencyReason] NVARCHAR(1200) NULL,
    [AdditionalDataJson] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_RequestClinicalData_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_RequestClinicalData] PRIMARY KEY CLUSTERED ([RequestClinicalDataId] ASC),
    CONSTRAINT [CK_clinical_RequestClinicalData_1] CHECK ([Hemoglobin] IS NULL OR [Hemoglobin] BETWEEN 0 AND 30),
    CONSTRAINT [CK_clinical_RequestClinicalData_2] CHECK ([Hematocrit] IS NULL OR [Hematocrit] BETWEEN 0 AND 100),
    CONSTRAINT [CK_clinical_RequestClinicalData_3] CHECK ([AdditionalDataJson] IS NULL OR ISJSON([AdditionalDataJson])=1)
);
GO

CREATE TABLE [clinical].[BloodRequestItem] (
    [BloodRequestItemId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodRequestId] BIGINT NOT NULL,
    [ProductTypeId] BIGINT NOT NULL,
    [RequestedQuantity] DECIMAL(12,3) NOT NULL,
    [QuantityUnit] VARCHAR(20) NOT NULL CONSTRAINT DF_clinical_BloodRequestItem_Unit DEFAULT ('Unit'),
    [SpecialRequirements] NVARCHAR(800) NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_clinical_BloodRequestItem_Status DEFAULT ('Pending'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_BloodRequestItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_BloodRequestItem] PRIMARY KEY CLUSTERED ([BloodRequestItemId] ASC),
    CONSTRAINT [CK_clinical_BloodRequestItem_1] CHECK ([RequestedQuantity]>0),
    CONSTRAINT [CK_clinical_BloodRequestItem_2] CHECK ([Status] IN ('Pending','Reserved','Ready','PartiallyIssued','Issued','Cancelled'))
);
GO

CREATE TABLE [clinical].[RequestApproval] (
    [RequestApprovalId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodRequestId] BIGINT NOT NULL,
    [ApprovalStage] VARCHAR(35) NOT NULL,
    [ReviewerUserId] BIGINT NOT NULL,
    [Decision] VARCHAR(20) NOT NULL,
    [DecidedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_RequestApproval_At DEFAULT (SYSUTCDATETIME()),
    [Reason] NVARCHAR(1500) NULL,
    [RevisionNo] INT NOT NULL CONSTRAINT DF_clinical_RequestApproval_Revision DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_RequestApproval_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_RequestApproval] PRIMARY KEY CLUSTERED ([RequestApprovalId] ASC),
    CONSTRAINT [CK_clinical_RequestApproval_1] CHECK ([Decision] IN ('Approved','Rejected','Returned')),
    CONSTRAINT [CK_clinical_RequestApproval_2] CHECK ([RevisionNo]>0)
);
GO

CREATE TABLE [clinical].[ClinicalTask] (
    [ClinicalTaskId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [EncounterId] BIGINT NULL,
    [BloodRequestId] BIGINT NULL,
    [AssignedUserId] BIGINT NULL,
    [AssignedRoleId] BIGINT NULL,
    [TaskType] VARCHAR(70) NOT NULL,
    [Priority] VARCHAR(20) NOT NULL CONSTRAINT DF_clinical_ClinicalTask_Priority DEFAULT ('Routine'),
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_clinical_ClinicalTask_Status DEFAULT ('Open'),
    [DueAt] DATETIME2(3) NULL,
    [CompletedAt] DATETIME2(3) NULL,
    [CreatedByUserId] BIGINT NULL,
    [Summary] NVARCHAR(600) NOT NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_ClinicalTask_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_ClinicalTask] PRIMARY KEY CLUSTERED ([ClinicalTaskId] ASC),
    CONSTRAINT [CK_clinical_ClinicalTask_1] CHECK ([Status] IN ('Open','InProgress','Completed','Cancelled')),
    CONSTRAINT [CK_clinical_ClinicalTask_2] CHECK ([Priority] IN ('Routine','Urgent','Emergency')),
    CONSTRAINT [CK_clinical_ClinicalTask_3] CHECK ([AssignedUserId] IS NOT NULL OR [AssignedRoleId] IS NOT NULL)
);
GO

CREATE TABLE [clinical].[ClinicalNote] (
    [ClinicalNoteId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [BloodRequestId] BIGINT NULL,
    [WrittenByUserId] BIGINT NOT NULL,
    [NoteType] VARCHAR(40) NOT NULL,
    [Body] NVARCHAR(MAX) NOT NULL,
    [WrittenAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_ClinicalNote_At DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_clinical_ClinicalNote] PRIMARY KEY CLUSTERED ([ClinicalNoteId] ASC)
);
GO

CREATE TABLE [clinical].[Transfusion] (
    [TransfusionId] BIGINT IDENTITY(1,1) NOT NULL,
    [EncounterId] BIGINT NOT NULL,
    [BloodRequestItemId] BIGINT NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [StartedByUserId] BIGINT NULL,
    [EndedByUserId] BIGINT NULL,
    [StartedAt] DATETIME2(3) NULL,
    [EndedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_clinical_Transfusion_Status DEFAULT ('Planned'),
    [StopReason] NVARCHAR(1500) NULL,
    [FinalDisposition] VARCHAR(30) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_Transfusion_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_Transfusion] PRIMARY KEY CLUSTERED ([TransfusionId] ASC),
    CONSTRAINT [CK_clinical_Transfusion_1] CHECK ([Status] IN ('Planned','InProgress','Stopped','Completed','Cancelled')),
    CONSTRAINT [CK_clinical_Transfusion_2] CHECK ([EndedAt] IS NULL OR [StartedAt] IS NULL OR [EndedAt]>=[StartedAt])
);
GO

CREATE TABLE [clinical].[TransfusionChecklist] (
    [TransfusionChecklistId] BIGINT IDENTITY(1,1) NOT NULL,
    [TransfusionId] BIGINT NOT NULL,
    [PatientIdentityMatched] BIT NOT NULL,
    [UnitIdentityMatched] BIT NOT NULL,
    [BloodGroupChecked] BIT NOT NULL,
    [CompatibilityChecked] BIT NOT NULL,
    [ExpiryChecked] BIT NOT NULL,
    [BaselineVitalChecked] BIT NOT NULL,
    [CheckedByUserId] BIGINT NOT NULL,
    [CheckedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_TransfusionChecklist_At DEFAULT (SYSUTCDATETIME()),
    [Note] NVARCHAR(800) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_TransfusionChecklist_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_TransfusionChecklist] PRIMARY KEY CLUSTERED ([TransfusionChecklistId] ASC)
);
GO

CREATE TABLE [clinical].[VitalObservation] (
    [VitalObservationId] BIGINT IDENTITY(1,1) NOT NULL,
    [TransfusionId] BIGINT NOT NULL,
    [ObservedAt] DATETIME2(3) NOT NULL,
    [TimepointCode] VARCHAR(30) NOT NULL,
    [SystolicBp] SMALLINT NULL,
    [DiastolicBp] SMALLINT NULL,
    [HeartRate] SMALLINT NULL,
    [TemperatureC] DECIMAL(4,1) NULL,
    [Spo2] TINYINT NULL,
    [RespiratoryRate] SMALLINT NULL,
    [UrineOutputMl] DECIMAL(9,2) NULL,
    [RecordedByUserId] BIGINT NOT NULL,
    [ClinicalNote] NVARCHAR(1200) NULL,
    [AlertFlag] BIT NOT NULL CONSTRAINT DF_clinical_VitalObservation_Alert DEFAULT (0),
    CONSTRAINT [PK_clinical_VitalObservation] PRIMARY KEY CLUSTERED ([VitalObservationId] ASC),
    CONSTRAINT [CK_clinical_VitalObservation_1] CHECK ([Spo2] IS NULL OR [Spo2] BETWEEN 0 AND 100),
    CONSTRAINT [CK_clinical_VitalObservation_2] CHECK ([TemperatureC] IS NULL OR [TemperatureC] BETWEEN 20 AND 45),
    CONSTRAINT [CK_clinical_VitalObservation_3] CHECK ([HeartRate] IS NULL OR [HeartRate] BETWEEN 0 AND 300),
    CONSTRAINT [CK_clinical_VitalObservation_4] CHECK ([RespiratoryRate] IS NULL OR [RespiratoryRate] BETWEEN 0 AND 100)
);
GO

CREATE TABLE [clinical].[TransfusionAction] (
    [TransfusionActionId] BIGINT IDENTITY(1,1) NOT NULL,
    [TransfusionId] BIGINT NOT NULL,
    [ActionType] VARCHAR(60) NOT NULL,
    [ActionAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_TransfusionAction_At DEFAULT (SYSUTCDATETIME()),
    [PerformedByUserId] BIGINT NOT NULL,
    [Description] NVARCHAR(1500) NULL,
    CONSTRAINT [PK_clinical_TransfusionAction] PRIMARY KEY CLUSTERED ([TransfusionActionId] ASC)
);
GO

CREATE TABLE [clinical].[TransfusionSignoff] (
    [TransfusionSignoffId] BIGINT IDENTITY(1,1) NOT NULL,
    [TransfusionId] BIGINT NOT NULL,
    [SignerUserId] BIGINT NOT NULL,
    [SignoffRole] VARCHAR(35) NOT NULL,
    [SignedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_TransfusionSignoff_At DEFAULT (SYSUTCDATETIME()),
    [SignatureHash] VARBINARY(32) NULL,
    [Comment] NVARCHAR(800) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_TransfusionSignoff_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_clinical_TransfusionSignoff] PRIMARY KEY CLUSTERED ([TransfusionSignoffId] ASC)
);
GO

CREATE TABLE [blood].[ProductType] (
    [ProductTypeId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(40) NOT NULL,
    [Name] NVARCHAR(180) NOT NULL,
    [StorageMinC] DECIMAL(5,2) NULL,
    [StorageMaxC] DECIMAL(5,2) NULL,
    [ShelfLifeDays] INT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_blood_ProductType_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_ProductType_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_ProductType] PRIMARY KEY CLUSTERED ([ProductTypeId] ASC),
    CONSTRAINT [CK_blood_ProductType_1] CHECK ([StorageMinC] IS NULL OR [StorageMaxC] IS NULL OR [StorageMaxC]>[StorageMinC])
);
GO

CREATE TABLE [blood].[BloodGroup] (
    [BloodGroupId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(5) NOT NULL,
    [Abo] VARCHAR(2) NOT NULL,
    [RhD] CHAR(1) NOT NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_BloodGroup_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_BloodGroup] PRIMARY KEY CLUSTERED ([BloodGroupId] ASC),
    CONSTRAINT [CK_blood_BloodGroup_1] CHECK ([Abo] IN ('A','B','AB','O')),
    CONSTRAINT [CK_blood_BloodGroup_2] CHECK ([RhD] IN ('+','-'))
);
GO

CREATE TABLE [blood].[Supplier] (
    [SupplierId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(40) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [SupplierType] VARCHAR(30) NOT NULL CONSTRAINT DF_blood_Supplier_Type DEFAULT ('BloodCenter'),
    [IsActive] BIT NOT NULL CONSTRAINT DF_blood_Supplier_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_Supplier_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_Supplier] PRIMARY KEY CLUSTERED ([SupplierId] ASC)
);
GO

CREATE TABLE [blood].[StockOrder] (
    [StockOrderId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [SupplierId] BIGINT NOT NULL,
    [OrderNo] NVARCHAR(70) NOT NULL,
    [OrderedByUserId] BIGINT NOT NULL,
    [OrderedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_StockOrder_At DEFAULT (SYSUTCDATETIME()),
    [Status] VARCHAR(30) NOT NULL CONSTRAINT DF_blood_StockOrder_Status DEFAULT ('Draft'),
    [ExternalReference] NVARCHAR(100) NULL,
    [Notes] NVARCHAR(1200) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_StockOrder_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_StockOrder] PRIMARY KEY CLUSTERED ([StockOrderId] ASC),
    CONSTRAINT [CK_blood_StockOrder_1] CHECK ([Status] IN ('Draft','Submitted','PartiallyReceived','Received','Cancelled'))
);
GO

CREATE TABLE [blood].[StockOrderLine] (
    [StockOrderLineId] BIGINT IDENTITY(1,1) NOT NULL,
    [StockOrderId] BIGINT NOT NULL,
    [ProductTypeId] BIGINT NOT NULL,
    [BloodGroupId] BIGINT NULL,
    [Quantity] INT NOT NULL,
    [SpecialSpecification] NVARCHAR(600) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_StockOrderLine_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_StockOrderLine] PRIMARY KEY CLUSTERED ([StockOrderLineId] ASC),
    CONSTRAINT [CK_blood_StockOrderLine_1] CHECK ([Quantity]>0)
);
GO

CREATE TABLE [blood].[StockReceipt] (
    [StockReceiptId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [StockOrderId] BIGINT NULL,
    [SupplierId] BIGINT NOT NULL,
    [ReceiptNo] NVARCHAR(70) NOT NULL,
    [ExternalReceiptNo] NVARCHAR(90) NULL,
    [ReceivedAt] DATETIME2(3) NOT NULL,
    [ReceivedByUserId] BIGINT NOT NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_blood_StockReceipt_Status DEFAULT ('Draft'),
    [TemperatureC] DECIMAL(5,2) NULL,
    [HasDiscrepancy] BIT NOT NULL CONSTRAINT DF_blood_StockReceipt_Discrepancy DEFAULT (0),
    [DiscrepancyNote] NVARCHAR(1400) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_StockReceipt_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_StockReceipt] PRIMARY KEY CLUSTERED ([StockReceiptId] ASC),
    CONSTRAINT [CK_blood_StockReceipt_1] CHECK ([Status] IN ('Draft','Verified','Rejected','Posted'))
);
GO

CREATE TABLE [blood].[BloodUnit] (
    [BloodUnitId] BIGINT IDENTITY(1,1) NOT NULL,
    [UnitCode] NVARCHAR(80) NOT NULL,
    [ProductTypeId] BIGINT NOT NULL,
    [BloodGroupId] BIGINT NOT NULL,
    [SupplierId] BIGINT NULL,
    [CurrentFacilityId] BIGINT NOT NULL,
    [StockReceiptId] BIGINT NULL,
    [CollectionDate] DATE NULL,
    [ExpiryDate] DATE NOT NULL,
    [ReceivedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(30) NOT NULL CONSTRAINT DF_blood_BloodUnit_Status DEFAULT ('PendingReceipt'),
    [SpecialAttributesJson] NVARCHAR(MAX) NULL,
    [StorageLocation] NVARCHAR(120) NULL,
    [IsQuarantined] BIT NOT NULL CONSTRAINT DF_blood_BloodUnit_Quarantine DEFAULT (0),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_BloodUnit_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_BloodUnit] PRIMARY KEY CLUSTERED ([BloodUnitId] ASC),
    CONSTRAINT [CK_blood_BloodUnit_1] CHECK ([Status] IN ('PendingReceipt','Available','Reserved','Crossmatch','Issued','InTransit','Transfused','Returned','Quarantined','Expired','Wasted')),
    CONSTRAINT [CK_blood_BloodUnit_2] CHECK ([CollectionDate] IS NULL OR [ExpiryDate]>=[CollectionDate]),
    CONSTRAINT [CK_blood_BloodUnit_3] CHECK ([SpecialAttributesJson] IS NULL OR ISJSON([SpecialAttributesJson])=1)
);
GO

CREATE TABLE [blood].[StockReceiptLine] (
    [StockReceiptLineId] BIGINT IDENTITY(1,1) NOT NULL,
    [StockReceiptId] BIGINT NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [VerifiedByUserId] BIGINT NULL,
    [VerificationNote] NVARCHAR(700) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_StockReceiptLine_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_StockReceiptLine] PRIMARY KEY CLUSTERED ([StockReceiptLineId] ASC)
);
GO

CREATE TABLE [blood].[UnitReservation] (
    [UnitReservationId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [BloodRequestItemId] BIGINT NOT NULL,
    [ReservedByUserId] BIGINT NOT NULL,
    [ReservedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_UnitReservation_At DEFAULT (SYSUTCDATETIME()),
    [ReleasedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_blood_UnitReservation_Status DEFAULT ('Active'),
    [ReleaseReason] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_UnitReservation_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_UnitReservation] PRIMARY KEY CLUSTERED ([UnitReservationId] ASC),
    CONSTRAINT [CK_blood_UnitReservation_1] CHECK ([Status] IN ('Active','Released','Consumed')),
    CONSTRAINT [CK_blood_UnitReservation_2] CHECK ([ReleasedAt] IS NULL OR [ReleasedAt]>=[ReservedAt])
);
GO

CREATE TABLE [blood].[Crossmatch] (
    [CrossmatchId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodRequestItemId] BIGINT NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [PerformedByUserId] BIGINT NOT NULL,
    [PerformedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_Crossmatch_At DEFAULT (SYSUTCDATETIME()),
    [PatientBloodGroupId] BIGINT NULL,
    [UnitLabelBloodGroupId] BIGINT NULL,
    [VerifiedUnitBloodGroupId] BIGINT NULL,
    [AntibodyScreenResult] VARCHAR(30) NULL,
    [CompatibilityResult] VARCHAR(30) NOT NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_blood_Crossmatch_Status DEFAULT ('Draft'),
    [ApprovedByUserId] BIGINT NULL,
    [ApprovedAt] DATETIME2(3) NULL,
    [Comments] NVARCHAR(1500) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_Crossmatch_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_Crossmatch] PRIMARY KEY CLUSTERED ([CrossmatchId] ASC),
    CONSTRAINT [CK_blood_Crossmatch_1] CHECK ([Status] IN ('Draft','PendingReview','Final')),
    CONSTRAINT [CK_blood_Crossmatch_2] CHECK ([CompatibilityResult] IN ('Pending','Compatible','Incompatible','Discrepancy','NotApplicable'))
);
GO

CREATE TABLE [blood].[CrossmatchTest] (
    [CrossmatchTestId] BIGINT IDENTITY(1,1) NOT NULL,
    [CrossmatchId] BIGINT NOT NULL,
    [TestCode] VARCHAR(60) NOT NULL,
    [TestPhase] VARCHAR(35) NULL,
    [CellCode] VARCHAR(40) NULL,
    [Method] NVARCHAR(100) NULL,
    [Result] VARCHAR(35) NULL,
    [ReactionGrade] NVARCHAR(30) NULL,
    [ObservedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_CrossmatchTest_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_CrossmatchTest] PRIMARY KEY CLUSTERED ([CrossmatchTestId] ASC)
);
GO

CREATE TABLE [blood].[UnitIssue] (
    [UnitIssueId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [BloodRequestItemId] BIGINT NOT NULL,
    [IssueNo] NVARCHAR(75) NOT NULL,
    [IssuedByUserId] BIGINT NOT NULL,
    [IssuedToDepartmentId] BIGINT NOT NULL,
    [IssuedAt] DATETIME2(3) NOT NULL,
    [ReceivedByUserId] BIGINT NULL,
    [ReceivedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_blood_UnitIssue_Status DEFAULT ('Issued'),
    [TransportConditionNote] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_UnitIssue_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_UnitIssue] PRIMARY KEY CLUSTERED ([UnitIssueId] ASC),
    CONSTRAINT [CK_blood_UnitIssue_1] CHECK ([Status] IN ('Issued','InTransit','Received','Returned','Cancelled')),
    CONSTRAINT [CK_blood_UnitIssue_2] CHECK ([ReceivedAt] IS NULL OR [ReceivedAt]>=[IssuedAt])
);
GO

CREATE TABLE [blood].[Dispatch] (
    [DispatchId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [DepartmentId] BIGINT NOT NULL,
    [DispatchNo] NVARCHAR(75) NOT NULL,
    [CreatedByUserId] BIGINT NOT NULL,
    [CreatedAtDispatch] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_Dispatch_DispatchAt DEFAULT (SYSUTCDATETIME()),
    [SentAt] DATETIME2(3) NULL,
    [DeliveredAt] DATETIME2(3) NULL,
    [CourierUserId] BIGINT NULL,
    [ReceiverUserId] BIGINT NULL,
    [ContainerType] NVARCHAR(80) NULL,
    [TemperatureAtExitC] DECIMAL(5,2) NULL,
    [Status] VARCHAR(30) NOT NULL CONSTRAINT DF_blood_Dispatch_Status DEFAULT ('Pending'),
    [Note] NVARCHAR(1200) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_Dispatch_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_Dispatch] PRIMARY KEY CLUSTERED ([DispatchId] ASC),
    CONSTRAINT [CK_blood_Dispatch_1] CHECK ([Status] IN ('Pending','Preparing','InTransit','Delivered','Returned','Cancelled'))
);
GO

CREATE TABLE [blood].[DispatchItem] (
    [DispatchItemId] BIGINT IDENTITY(1,1) NOT NULL,
    [DispatchId] BIGINT NOT NULL,
    [UnitIssueId] BIGINT NOT NULL,
    [TemperatureAtHandoverC] DECIMAL(5,2) NULL,
    [ConditionNote] NVARCHAR(700) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_DispatchItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_DispatchItem] PRIMARY KEY CLUSTERED ([DispatchItemId] ASC)
);
GO

CREATE TABLE [blood].[UnitReturn] (
    [UnitReturnId] BIGINT IDENTITY(1,1) NOT NULL,
    [UnitIssueId] BIGINT NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [ReturnedByUserId] BIGINT NOT NULL,
    [ReceivedByUserId] BIGINT NOT NULL,
    [ReturnedAt] DATETIME2(3) NOT NULL,
    [ReasonCode] VARCHAR(60) NOT NULL,
    [ConditionCode] VARCHAR(40) NULL,
    [Decision] VARCHAR(20) NULL,
    [ReviewedByUserId] BIGINT NULL,
    [ReviewedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_blood_UnitReturn_Status DEFAULT ('Pending'),
    [TemperatureC] DECIMAL(5,2) NULL,
    [Notes] NVARCHAR(1400) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_UnitReturn_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_UnitReturn] PRIMARY KEY CLUSTERED ([UnitReturnId] ASC),
    CONSTRAINT [CK_blood_UnitReturn_1] CHECK ([Decision] IS NULL OR [Decision] IN ('Restock','Quarantine','Waste')),
    CONSTRAINT [CK_blood_UnitReturn_2] CHECK ([Status] IN ('Pending','Approved','Rejected'))
);
GO

CREATE TABLE [blood].[UnitWaste] (
    [UnitWasteId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [UnitReturnId] BIGINT NULL,
    [WasteCode] NVARCHAR(80) NOT NULL,
    [ReasonCode] VARCHAR(70) NOT NULL,
    [WasteDate] DATETIME2(3) NOT NULL,
    [RecordedByUserId] BIGINT NOT NULL,
    [ApprovedByUserId] BIGINT NULL,
    [ApprovedAt] DATETIME2(3) NULL,
    [DisposalMethod] NVARCHAR(250) NULL,
    [Description] NVARCHAR(1500) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_UnitWaste_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_UnitWaste] PRIMARY KEY CLUSTERED ([UnitWasteId] ASC)
);
GO

CREATE TABLE [blood].[ThawProcess] (
    [ThawProcessId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [BloodRequestItemId] BIGINT NULL,
    [EquipmentId] BIGINT NULL,
    [Method] VARCHAR(50) NOT NULL,
    [StartedAt] DATETIME2(3) NOT NULL,
    [FinishedAt] DATETIME2(3) NULL,
    [BathTemperatureC] DECIMAL(5,2) NULL,
    [Result] VARCHAR(25) NOT NULL CONSTRAINT DF_blood_ThawProcess_Result DEFAULT ('InProgress'),
    [PerformedByUserId] BIGINT NOT NULL,
    [Note] NVARCHAR(900) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_ThawProcess_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_ThawProcess] PRIMARY KEY CLUSTERED ([ThawProcessId] ASC),
    CONSTRAINT [CK_blood_ThawProcess_1] CHECK ([FinishedAt] IS NULL OR [FinishedAt]>=[StartedAt]),
    CONSTRAINT [CK_blood_ThawProcess_2] CHECK ([Result] IN ('InProgress','Passed','Failed','Cancelled'))
);
GO

CREATE TABLE [blood].[InterfacilityTransfer] (
    [InterfacilityTransferId] BIGINT IDENTITY(1,1) NOT NULL,
    [SourceFacilityId] BIGINT NOT NULL,
    [DestinationFacilityId] BIGINT NOT NULL,
    [TransferNo] NVARCHAR(75) NOT NULL,
    [IsEmergency] BIT NOT NULL CONSTRAINT DF_blood_InterfacilityTransfer_Emergency DEFAULT (0),
    [RequestedByUserId] BIGINT NOT NULL,
    [ApprovedByUserId] BIGINT NULL,
    [DepartedAt] DATETIME2(3) NULL,
    [ReceivedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_blood_InterfacilityTransfer_Status DEFAULT ('Draft'),
    [Reason] NVARCHAR(1300) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_InterfacilityTransfer_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_InterfacilityTransfer] PRIMARY KEY CLUSTERED ([InterfacilityTransferId] ASC),
    CONSTRAINT [CK_blood_InterfacilityTransfer_1] CHECK ([SourceFacilityId]<>[DestinationFacilityId]),
    CONSTRAINT [CK_blood_InterfacilityTransfer_2] CHECK ([Status] IN ('Draft','Approved','InTransit','Received','Cancelled'))
);
GO

CREATE TABLE [blood].[TransferItem] (
    [TransferItemId] BIGINT IDENTITY(1,1) NOT NULL,
    [InterfacilityTransferId] BIGINT NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [ReceivedCondition] NVARCHAR(400) NULL,
    [ReceivedTemperatureC] DECIMAL(5,2) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_TransferItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_TransferItem] PRIMARY KEY CLUSTERED ([TransferItemId] ASC)
);
GO

CREATE TABLE [blood].[InventoryMovement] (
    [InventoryMovementId] BIGINT IDENTITY(1,1) NOT NULL,
    [BloodUnitId] BIGINT NOT NULL,
    [FromFacilityId] BIGINT NULL,
    [ToFacilityId] BIGINT NULL,
    [MovementType] VARCHAR(50) NOT NULL,
    [MovementAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_InventoryMovement_At DEFAULT (SYSUTCDATETIME()),
    [PerformedByUserId] BIGINT NOT NULL,
    [ReferenceType] VARCHAR(45) NULL,
    [ReferenceId] BIGINT NULL,
    [PriorStatus] VARCHAR(30) NULL,
    [NewStatus] VARCHAR(30) NULL,
    [Explanation] NVARCHAR(1200) NULL,
    CONSTRAINT [PK_blood_InventoryMovement] PRIMARY KEY CLUSTERED ([InventoryMovementId] ASC)
);
GO

CREATE TABLE [blood].[DailyQualityControl] (
    [DailyQualityControlId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [EquipmentId] BIGINT NULL,
    [QcDate] DATE NOT NULL,
    [ShiftId] BIGINT NULL,
    [RecordedByUserId] BIGINT NOT NULL,
    [ReviewedByUserId] BIGINT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_blood_DailyQualityControl_Status DEFAULT ('Draft'),
    [Notes] NVARCHAR(1300) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_DailyQualityControl_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_DailyQualityControl] PRIMARY KEY CLUSTERED ([DailyQualityControlId] ASC),
    CONSTRAINT [CK_blood_DailyQualityControl_1] CHECK ([Status] IN ('Draft','Passed','Failed','Reviewed'))
);
GO

CREATE TABLE [blood].[QualityControlResult] (
    [QualityControlResultId] BIGINT IDENTITY(1,1) NOT NULL,
    [DailyQualityControlId] BIGINT NOT NULL,
    [ParameterCode] VARCHAR(70) NOT NULL,
    [ReagentLot] NVARCHAR(100) NULL,
    [ExpectedValue] NVARCHAR(100) NULL,
    [ObservedValue] NVARCHAR(100) NULL,
    [IsPassed] BIT NOT NULL,
    [CorrectiveNote] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_blood_QualityControlResult_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_blood_QualityControlResult] PRIMARY KEY CLUSTERED ([QualityControlResultId] ASC)
);
GO

CREATE TABLE [equip].[EquipmentCategory] (
    [EquipmentCategoryId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(45) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [StandardMinC] DECIMAL(5,2) NULL,
    [StandardMaxC] DECIMAL(5,2) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_EquipmentCategory_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_equip_EquipmentCategory] PRIMARY KEY CLUSTERED ([EquipmentCategoryId] ASC)
);
GO

CREATE TABLE [equip].[Equipment] (
    [EquipmentId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [DepartmentId] BIGINT NULL,
    [EquipmentCategoryId] BIGINT NOT NULL,
    [AssetTag] NVARCHAR(70) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Manufacturer] NVARCHAR(120) NULL,
    [Model] NVARCHAR(120) NULL,
    [SerialNo] NVARCHAR(150) NULL,
    [InstalledAt] DATE NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_equip_Equipment_Status DEFAULT ('Active'),
    [LocationNote] NVARCHAR(300) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_Equipment_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_equip_Equipment] PRIMARY KEY CLUSTERED ([EquipmentId] ASC),
    CONSTRAINT [CK_equip_Equipment_1] CHECK ([Status] IN ('Active','Service','OutOfService','Retired'))
);
GO

CREATE TABLE [equip].[MaintenancePlan] (
    [MaintenancePlanId] BIGINT IDENTITY(1,1) NOT NULL,
    [EquipmentId] BIGINT NOT NULL,
    [PlanType] VARCHAR(30) NOT NULL,
    [IntervalDays] INT NULL,
    [NextDueDate] DATE NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_equip_MaintenancePlan_Active DEFAULT (1),
    [Instructions] NVARCHAR(1400) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_MaintenancePlan_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_equip_MaintenancePlan] PRIMARY KEY CLUSTERED ([MaintenancePlanId] ASC),
    CONSTRAINT [CK_equip_MaintenancePlan_1] CHECK ([PlanType] IN ('PM','Calibration','Safety','Other')),
    CONSTRAINT [CK_equip_MaintenancePlan_2] CHECK ([IntervalDays] IS NULL OR [IntervalDays]>0)
);
GO

CREATE TABLE [equip].[MaintenanceRecord] (
    [MaintenanceRecordId] BIGINT IDENTITY(1,1) NOT NULL,
    [EquipmentId] BIGINT NOT NULL,
    [MaintenancePlanId] BIGINT NULL,
    [PerformedAt] DATETIME2(3) NOT NULL,
    [PerformedByUserId] BIGINT NULL,
    [Vendor] NVARCHAR(180) NULL,
    [Result] VARCHAR(25) NOT NULL,
    [CertificateNo] NVARCHAR(100) NULL,
    [NextDueDate] DATE NULL,
    [ChecklistJson] NVARCHAR(MAX) NULL,
    [Notes] NVARCHAR(1200) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_MaintenanceRecord_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_equip_MaintenanceRecord] PRIMARY KEY CLUSTERED ([MaintenanceRecordId] ASC),
    CONSTRAINT [CK_equip_MaintenanceRecord_1] CHECK ([Result] IN ('Passed','Failed','Conditional')),
    CONSTRAINT [CK_equip_MaintenanceRecord_2] CHECK ([ChecklistJson] IS NULL OR ISJSON([ChecklistJson])=1)
);
GO

CREATE TABLE [equip].[TemperatureReading] (
    [TemperatureReadingId] BIGINT IDENTITY(1,1) NOT NULL,
    [EquipmentId] BIGINT NOT NULL,
    [ObservedAt] DATETIME2(3) NOT NULL,
    [CurrentC] DECIMAL(6,2) NOT NULL,
    [MinimumC] DECIMAL(6,2) NULL,
    [MaximumC] DECIMAL(6,2) NULL,
    [ShiftId] BIGINT NULL,
    [RecorderUserId] BIGINT NULL,
    [SourceType] VARCHAR(20) NOT NULL CONSTRAINT DF_equip_TemperatureReading_Source DEFAULT ('Manual'),
    [DoorChecked] BIT NULL,
    [PowerChecked] BIT NULL,
    [AlarmChecked] BIT NULL,
    [Note] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_equip_TemperatureReading] PRIMARY KEY CLUSTERED ([TemperatureReadingId] ASC),
    CONSTRAINT [CK_equip_TemperatureReading_1] CHECK ([SourceType] IN ('Manual','Sensor','Integration')),
    CONSTRAINT [CK_equip_TemperatureReading_2] CHECK ([MinimumC] IS NULL OR [MaximumC] IS NULL OR [MaximumC]>=[MinimumC])
);
GO

CREATE TABLE [equip].[TemperatureExcursion] (
    [TemperatureExcursionId] BIGINT IDENTITY(1,1) NOT NULL,
    [EquipmentId] BIGINT NOT NULL,
    [FirstReadingId] BIGINT NULL,
    [DetectedAt] DATETIME2(3) NOT NULL,
    [ResolvedAt] DATETIME2(3) NULL,
    [MinObservedC] DECIMAL(6,2) NULL,
    [MaxObservedC] DECIMAL(6,2) NULL,
    [Severity] VARCHAR(20) NOT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_equip_TemperatureExcursion_Status DEFAULT ('Open'),
    [AssignedToUserId] BIGINT NULL,
    [CorrectiveAction] NVARCHAR(1500) NULL,
    [SupervisorApprovedByUserId] BIGINT NULL,
    [SupervisorApprovedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_TemperatureExcursion_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_equip_TemperatureExcursion] PRIMARY KEY CLUSTERED ([TemperatureExcursionId] ASC),
    CONSTRAINT [CK_equip_TemperatureExcursion_1] CHECK ([Status] IN ('Open','InProgress','Resolved','Closed')),
    CONSTRAINT [CK_equip_TemperatureExcursion_2] CHECK ([Severity] IN ('Low','Medium','High','Critical')),
    CONSTRAINT [CK_equip_TemperatureExcursion_3] CHECK ([ResolvedAt] IS NULL OR [ResolvedAt]>=[DetectedAt])
);
GO

CREATE TABLE [equip].[EquipmentIncident] (
    [EquipmentIncidentId] BIGINT IDENTITY(1,1) NOT NULL,
    [EquipmentId] BIGINT NOT NULL,
    [ReportedByUserId] BIGINT NOT NULL,
    [IncidentNo] NVARCHAR(70) NOT NULL,
    [OccurredAt] DATETIME2(3) NOT NULL,
    [Severity] VARCHAR(20) NOT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_equip_EquipmentIncident_Status DEFAULT ('Open'),
    [Description] NVARCHAR(2000) NOT NULL,
    [ResolutionNote] NVARCHAR(1600) NULL,
    [ClosedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_EquipmentIncident_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_equip_EquipmentIncident] PRIMARY KEY CLUSTERED ([EquipmentIncidentId] ASC),
    CONSTRAINT [CK_equip_EquipmentIncident_1] CHECK ([Status] IN ('Open','Assigned','Resolved','Closed')),
    CONSTRAINT [CK_equip_EquipmentIncident_2] CHECK ([Severity] IN ('Low','Medium','High','Critical'))
);
GO

CREATE TABLE [equip].[EquipmentIncidentAction] (
    [EquipmentIncidentActionId] BIGINT IDENTITY(1,1) NOT NULL,
    [EquipmentIncidentId] BIGINT NOT NULL,
    [PerformedByUserId] BIGINT NOT NULL,
    [ActionAt] DATETIME2(3) NOT NULL CONSTRAINT DF_equip_EquipmentIncidentAction_At DEFAULT (SYSUTCDATETIME()),
    [ActionType] VARCHAR(50) NOT NULL,
    [Details] NVARCHAR(1500) NOT NULL,
    CONSTRAINT [PK_equip_EquipmentIncidentAction] PRIMARY KEY CLUSTERED ([EquipmentIncidentActionId] ASC)
);
GO

CREATE TABLE [hv].[AdverseEvent] (
    [AdverseEventId] BIGINT IDENTITY(1,1) NOT NULL,
    [TransfusionId] BIGINT NOT NULL,
    [EventNo] NVARCHAR(70) NOT NULL,
    [DetectedAt] DATETIME2(3) NOT NULL,
    [ReportedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_AdverseEvent_Reported DEFAULT (SYSUTCDATETIME()),
    [ReportedByUserId] BIGINT NOT NULL,
    [SeverityGrade] TINYINT NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_hv_AdverseEvent_Status DEFAULT ('Reported'),
    [TransfusionStopped] BIT NOT NULL CONSTRAINT DF_hv_AdverseEvent_Stopped DEFAULT (0),
    [SymptomsSummary] NVARCHAR(1800) NULL,
    [ImmediateActions] NVARCHAR(1800) NULL,
    [Outcome] NVARCHAR(1000) NULL,
    [Narrative] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_AdverseEvent_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_AdverseEvent] PRIMARY KEY CLUSTERED ([AdverseEventId] ASC),
    CONSTRAINT [CK_hv_AdverseEvent_1] CHECK ([SeverityGrade] IS NULL OR [SeverityGrade] BETWEEN 0 AND 4),
    CONSTRAINT [CK_hv_AdverseEvent_2] CHECK ([Status] IN ('Reported','DoctorReview','Investigation','Confirmed','Closed','Cancelled'))
);
GO

CREATE TABLE [hv].[EventSymptom] (
    [EventSymptomId] BIGINT IDENTITY(1,1) NOT NULL,
    [AdverseEventId] BIGINT NOT NULL,
    [SymptomCode] VARCHAR(80) NOT NULL,
    [Details] NVARCHAR(450) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_EventSymptom_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_EventSymptom] PRIMARY KEY CLUSTERED ([EventSymptomId] ASC)
);
GO

CREATE TABLE [hv].[PhysicianEventAssessment] (
    [PhysicianEventAssessmentId] BIGINT IDENTITY(1,1) NOT NULL,
    [AdverseEventId] BIGINT NOT NULL,
    [AssessedByUserId] BIGINT NOT NULL,
    [AssessedAt] DATETIME2(3) NOT NULL,
    [DiagnosisCode] NVARCHAR(80) NULL,
    [DiagnosisText] NVARCHAR(1500) NOT NULL,
    [SeverityGrade] TINYINT NULL,
    [Causality] VARCHAR(35) NULL,
    [IsbtCode] NVARCHAR(50) NULL,
    [Interventions] NVARCHAR(1400) NULL,
    [Recommendations] NVARCHAR(1400) NULL,
    [Outcome] NVARCHAR(1400) NULL,
    [NationalReportRequired] BIT NOT NULL CONSTRAINT DF_hv_PhysicianEventAssessment_National DEFAULT (0),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_PhysicianEventAssessment_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_PhysicianEventAssessment] PRIMARY KEY CLUSTERED ([PhysicianEventAssessmentId] ASC),
    CONSTRAINT [CK_hv_PhysicianEventAssessment_1] CHECK ([SeverityGrade] IS NULL OR [SeverityGrade] BETWEEN 0 AND 4)
);
GO

CREATE TABLE [hv].[EventInvestigation] (
    [EventInvestigationId] BIGINT IDENTITY(1,1) NOT NULL,
    [AdverseEventId] BIGINT NOT NULL,
    [InvestigationType] VARCHAR(50) NOT NULL,
    [InvestigationAt] DATETIME2(3) NOT NULL,
    [PerformedByUserId] BIGINT NOT NULL,
    [ResultText] NVARCHAR(2000) NULL,
    [ResultJson] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_EventInvestigation_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_EventInvestigation] PRIMARY KEY CLUSTERED ([EventInvestigationId] ASC),
    CONSTRAINT [CK_hv_EventInvestigation_1] CHECK ([ResultJson] IS NULL OR ISJSON([ResultJson])=1)
);
GO

CREATE TABLE [hv].[RcaCase] (
    [RcaCaseId] BIGINT IDENTITY(1,1) NOT NULL,
    [AdverseEventId] BIGINT NULL,
    [EquipmentIncidentId] BIGINT NULL,
    [CaseNo] NVARCHAR(70) NOT NULL,
    [OpenedAt] DATETIME2(3) NOT NULL,
    [OpenedByUserId] BIGINT NOT NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_hv_RcaCase_Status DEFAULT ('Open'),
    [RootCauseSummary] NVARCHAR(MAX) NULL,
    [ClosedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_RcaCase_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_RcaCase] PRIMARY KEY CLUSTERED ([RcaCaseId] ASC),
    CONSTRAINT [CK_hv_RcaCase_1] CHECK ([AdverseEventId] IS NOT NULL OR [EquipmentIncidentId] IS NOT NULL),
    CONSTRAINT [CK_hv_RcaCase_2] CHECK ([Status] IN ('Open','Analyzing','ActionPending','Verified','Closed'))
);
GO

CREATE TABLE [hv].[RcaCause] (
    [RcaCauseId] BIGINT IDENTITY(1,1) NOT NULL,
    [RcaCaseId] BIGINT NOT NULL,
    [CauseType] VARCHAR(50) NOT NULL,
    [Description] NVARCHAR(1200) NOT NULL,
    [IsRootCause] BIT NOT NULL CONSTRAINT DF_hv_RcaCause_Root DEFAULT (0),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_RcaCause_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_RcaCause] PRIMARY KEY CLUSTERED ([RcaCauseId] ASC)
);
GO

CREATE TABLE [hv].[CorrectiveAction] (
    [CorrectiveActionId] BIGINT IDENTITY(1,1) NOT NULL,
    [RcaCaseId] BIGINT NULL,
    [EquipmentIncidentId] BIGINT NULL,
    [AuditId] BIGINT NULL,
    [Title] NVARCHAR(250) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [AssignedToUserId] BIGINT NOT NULL,
    [DueAt] DATETIME2(3) NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_hv_CorrectiveAction_Status DEFAULT ('Open'),
    [CompletedAt] DATETIME2(3) NULL,
    [VerifiedByUserId] BIGINT NULL,
    [VerifiedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_CorrectiveAction_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_CorrectiveAction] PRIMARY KEY CLUSTERED ([CorrectiveActionId] ASC),
    CONSTRAINT [CK_hv_CorrectiveAction_1] CHECK ([RcaCaseId] IS NOT NULL OR [EquipmentIncidentId] IS NOT NULL OR [AuditId] IS NOT NULL),
    CONSTRAINT [CK_hv_CorrectiveAction_2] CHECK ([Status] IN ('Open','InProgress','Completed','Verified','Cancelled'))
);
GO

CREATE TABLE [hv].[CommitteeMeeting] (
    [CommitteeMeetingId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [MeetingNo] NVARCHAR(75) NOT NULL,
    [MeetingType] VARCHAR(50) NOT NULL CONSTRAINT DF_hv_CommitteeMeeting_Type DEFAULT ('BTC'),
    [HeldAt] DATETIME2(3) NOT NULL,
    [ChairUserId] BIGINT NULL,
    [SecretaryUserId] BIGINT NULL,
    [MinutesText] NVARCHAR(MAX) NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_hv_CommitteeMeeting_Status DEFAULT ('Draft'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_CommitteeMeeting_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_CommitteeMeeting] PRIMARY KEY CLUSTERED ([CommitteeMeetingId] ASC),
    CONSTRAINT [CK_hv_CommitteeMeeting_1] CHECK ([Status] IN ('Draft','Approved','Published'))
);
GO

CREATE TABLE [hv].[MeetingAttendee] (
    [MeetingAttendeeId] BIGINT IDENTITY(1,1) NOT NULL,
    [CommitteeMeetingId] BIGINT NOT NULL,
    [AccountUserId] BIGINT NULL,
    [ExternalName] NVARCHAR(180) NULL,
    [AttendanceStatus] VARCHAR(25) NOT NULL CONSTRAINT DF_hv_MeetingAttendee_Status DEFAULT ('Present'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_MeetingAttendee_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_MeetingAttendee] PRIMARY KEY CLUSTERED ([MeetingAttendeeId] ASC),
    CONSTRAINT [CK_hv_MeetingAttendee_1] CHECK ([AccountUserId] IS NOT NULL OR [ExternalName] IS NOT NULL),
    CONSTRAINT [CK_hv_MeetingAttendee_2] CHECK ([AttendanceStatus] IN ('Present','Absent','Excused'))
);
GO

CREATE TABLE [hv].[MeetingDecision] (
    [MeetingDecisionId] BIGINT IDENTITY(1,1) NOT NULL,
    [CommitteeMeetingId] BIGINT NOT NULL,
    [DecisionNo] INT NOT NULL,
    [Description] NVARCHAR(2000) NOT NULL,
    [AssignedToUserId] BIGINT NULL,
    [DueDate] DATE NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_hv_MeetingDecision_Status DEFAULT ('Open'),
    [CompletedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_MeetingDecision_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_MeetingDecision] PRIMARY KEY CLUSTERED ([MeetingDecisionId] ASC),
    CONSTRAINT [CK_hv_MeetingDecision_1] CHECK ([Status] IN ('Open','InProgress','Completed','Cancelled'))
);
GO

CREATE TABLE [hv].[AuditQuestion] (
    [AuditQuestionId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(70) NOT NULL,
    [Category] NVARCHAR(130) NULL,
    [QuestionText] NVARCHAR(1000) NOT NULL,
    [MaxScore] DECIMAL(7,2) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_hv_AuditQuestion_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_AuditQuestion_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_AuditQuestion] PRIMARY KEY CLUSTERED ([AuditQuestionId] ASC),
    CONSTRAINT [CK_hv_AuditQuestion_1] CHECK ([MaxScore]>=0)
);
GO

CREATE TABLE [hv].[ClinicalAudit] (
    [ClinicalAuditId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [DepartmentId] BIGINT NOT NULL,
    [AuditNo] NVARCHAR(70) NOT NULL,
    [AuditType] VARCHAR(50) NOT NULL,
    [AuditedAt] DATETIME2(3) NOT NULL,
    [AuditorUserId] BIGINT NOT NULL,
    [Status] VARCHAR(25) NOT NULL CONSTRAINT DF_hv_ClinicalAudit_Status DEFAULT ('Draft'),
    [Score] DECIMAL(7,2) NULL,
    [Summary] NVARCHAR(2000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_ClinicalAudit_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_ClinicalAudit] PRIMARY KEY CLUSTERED ([ClinicalAuditId] ASC),
    CONSTRAINT [CK_hv_ClinicalAudit_1] CHECK ([Status] IN ('Draft','Submitted','Approved','Closed'))
);
GO

CREATE TABLE [hv].[AuditAnswer] (
    [AuditAnswerId] BIGINT IDENTITY(1,1) NOT NULL,
    [ClinicalAuditId] BIGINT NOT NULL,
    [AuditQuestionId] BIGINT NOT NULL,
    [Score] DECIMAL(7,2) NULL,
    [Answer] NVARCHAR(1400) NULL,
    [IsCompliant] BIT NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_AuditAnswer_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_AuditAnswer] PRIMARY KEY CLUSTERED ([AuditAnswerId] ASC),
    CONSTRAINT [CK_hv_AuditAnswer_1] CHECK ([Score] IS NULL OR [Score]>=0)
);
GO

CREATE TABLE [hv].[TrainingCourse] (
    [TrainingCourseId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [Code] NVARCHAR(70) NOT NULL,
    [Title] NVARCHAR(250) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [TargetAudience] NVARCHAR(600) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_hv_TrainingCourse_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_TrainingCourse_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_TrainingCourse] PRIMARY KEY CLUSTERED ([TrainingCourseId] ASC)
);
GO

CREATE TABLE [hv].[TrainingSession] (
    [TrainingSessionId] BIGINT IDENTITY(1,1) NOT NULL,
    [TrainingCourseId] BIGINT NOT NULL,
    [InstructorUserId] BIGINT NULL,
    [InstructorName] NVARCHAR(200) NULL,
    [StartsAt] DATETIME2(3) NOT NULL,
    [EndsAt] DATETIME2(3) NULL,
    [Capacity] INT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_hv_TrainingSession_Status DEFAULT ('Planned'),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_TrainingSession_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_TrainingSession] PRIMARY KEY CLUSTERED ([TrainingSessionId] ASC),
    CONSTRAINT [CK_hv_TrainingSession_1] CHECK ([EndsAt] IS NULL OR [EndsAt]>[StartsAt]),
    CONSTRAINT [CK_hv_TrainingSession_2] CHECK ([Capacity] IS NULL OR [Capacity]>0),
    CONSTRAINT [CK_hv_TrainingSession_3] CHECK ([Status] IN ('Planned','Completed','Cancelled'))
);
GO

CREATE TABLE [hv].[TrainingEnrollment] (
    [TrainingEnrollmentId] BIGINT IDENTITY(1,1) NOT NULL,
    [TrainingSessionId] BIGINT NOT NULL,
    [AccountUserId] BIGINT NOT NULL,
    [AttendanceStatus] VARCHAR(20) NOT NULL CONSTRAINT DF_hv_TrainingEnrollment_Status DEFAULT ('Registered'),
    [AttendedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_TrainingEnrollment_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_TrainingEnrollment] PRIMARY KEY CLUSTERED ([TrainingEnrollmentId] ASC),
    CONSTRAINT [CK_hv_TrainingEnrollment_1] CHECK ([AttendanceStatus] IN ('Registered','Attended','Absent','Cancelled'))
);
GO

CREATE TABLE [hv].[TrainingExamResult] (
    [TrainingExamResultId] BIGINT IDENTITY(1,1) NOT NULL,
    [TrainingEnrollmentId] BIGINT NOT NULL,
    [ExamTakenAt] DATETIME2(3) NOT NULL,
    [Score] DECIMAL(7,2) NOT NULL,
    [MaxScore] DECIMAL(7,2) NOT NULL,
    [Passed] BIT NOT NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_TrainingExamResult_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_TrainingExamResult] PRIMARY KEY CLUSTERED ([TrainingExamResultId] ASC),
    CONSTRAINT [CK_hv_TrainingExamResult_1] CHECK ([Score]>=0),
    CONSTRAINT [CK_hv_TrainingExamResult_2] CHECK ([MaxScore]>0),
    CONSTRAINT [CK_hv_TrainingExamResult_3] CHECK ([Score]<=[MaxScore])
);
GO

CREATE TABLE [hv].[DocumentLibrary] (
    [DocumentLibraryId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [Title] NVARCHAR(260) NOT NULL,
    [DocumentType] VARCHAR(45) NOT NULL,
    [Version] NVARCHAR(30) NULL,
    [StorageKey] NVARCHAR(450) NOT NULL,
    [UploadedByUserId] BIGINT NOT NULL,
    [UploadedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_DocumentLibrary_At DEFAULT (SYSUTCDATETIME()),
    [IsApproved] BIT NOT NULL CONSTRAINT DF_hv_DocumentLibrary_Approved DEFAULT (0),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_hv_DocumentLibrary_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_hv_DocumentLibrary] PRIMARY KEY CLUSTERED ([DocumentLibraryId] ASC)
);
GO

CREATE TABLE [integration].[Endpoint] (
    [EndpointId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [Code] VARCHAR(60) NOT NULL,
    [Name] NVARCHAR(180) NOT NULL,
    [Protocol] VARCHAR(25) NOT NULL,
    [EndpointAddress] NVARCHAR(600) NULL,
    [CredentialSecretRef] NVARCHAR(250) NULL,
    [IsEnabled] BIT NOT NULL CONSTRAINT DF_integration_Endpoint_Enabled DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_integration_Endpoint_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_integration_Endpoint] PRIMARY KEY CLUSTERED ([EndpointId] ASC),
    CONSTRAINT [CK_integration_Endpoint_1] CHECK ([Protocol] IN ('HL7v2','FHIR','REST','SOAP','Other'))
);
GO

CREATE TABLE [integration].[Message] (
    [MessageId] BIGINT IDENTITY(1,1) NOT NULL,
    [EndpointId] BIGINT NOT NULL,
    [PatientId] BIGINT NULL,
    [EncounterId] BIGINT NULL,
    [Direction] VARCHAR(10) NOT NULL,
    [MessageType] NVARCHAR(90) NOT NULL,
    [ExternalMessageId] NVARCHAR(160) NULL,
    [CorrelationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_integration_Message_Correlation DEFAULT (NEWID()),
    [CreatedAtMessage] DATETIME2(3) NOT NULL CONSTRAINT DF_integration_Message_At DEFAULT (SYSUTCDATETIME()),
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_integration_Message_Status DEFAULT ('Queued'),
    [PayloadStorageKey] NVARCHAR(500) NULL,
    [PayloadHash] VARBINARY(32) NULL,
    [ErrorSummary] NVARCHAR(2000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_integration_Message_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_integration_Message] PRIMARY KEY CLUSTERED ([MessageId] ASC),
    CONSTRAINT [CK_integration_Message_1] CHECK ([Direction] IN ('Inbound','Outbound')),
    CONSTRAINT [CK_integration_Message_2] CHECK ([Status] IN ('Queued','Sending','Succeeded','Failed','DeadLetter'))
);
GO

CREATE TABLE [integration].[MessageAttempt] (
    [MessageAttemptId] BIGINT IDENTITY(1,1) NOT NULL,
    [MessageId] BIGINT NOT NULL,
    [AttemptNo] INT NOT NULL,
    [StartedAt] DATETIME2(3) NOT NULL,
    [FinishedAt] DATETIME2(3) NULL,
    [WasSuccessful] BIT NOT NULL,
    [StatusCode] NVARCHAR(30) NULL,
    [ResponseSummary] NVARCHAR(1500) NULL,
    CONSTRAINT [PK_integration_MessageAttempt] PRIMARY KEY CLUSTERED ([MessageAttemptId] ASC),
    CONSTRAINT [CK_integration_MessageAttempt_1] CHECK ([AttemptNo]>0)
);
GO

CREATE TABLE [integration].[SyncJob] (
    [SyncJobId] BIGINT IDENTITY(1,1) NOT NULL,
    [EndpointId] BIGINT NOT NULL,
    [JobType] VARCHAR(60) NOT NULL,
    [TriggeredByUserId] BIGINT NULL,
    [StartedAt] DATETIME2(3) NOT NULL,
    [FinishedAt] DATETIME2(3) NULL,
    [Status] VARCHAR(20) NOT NULL,
    [ProcessedCount] INT NOT NULL CONSTRAINT DF_integration_SyncJob_Count DEFAULT (0),
    [FailedCount] INT NOT NULL CONSTRAINT DF_integration_SyncJob_Failed DEFAULT (0),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_integration_SyncJob_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_integration_SyncJob] PRIMARY KEY CLUSTERED ([SyncJobId] ASC),
    CONSTRAINT [CK_integration_SyncJob_1] CHECK ([Status] IN ('Queued','Running','Completed','Failed')),
    CONSTRAINT [CK_integration_SyncJob_2] CHECK ([ProcessedCount]>=0),
    CONSTRAINT [CK_integration_SyncJob_3] CHECK ([FailedCount]>=0)
);
GO

CREATE TABLE [report].[ReportDefinition] (
    [ReportDefinitionId] BIGINT IDENTITY(1,1) NOT NULL,
    [Code] VARCHAR(90) NOT NULL,
    [Title] NVARCHAR(250) NOT NULL,
    [Category] VARCHAR(65) NOT NULL,
    [TemplateKey] NVARCHAR(350) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_report_ReportDefinition_Active DEFAULT (1),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_report_ReportDefinition_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_report_ReportDefinition] PRIMARY KEY CLUSTERED ([ReportDefinitionId] ASC)
);
GO

CREATE TABLE [report].[ReportRun] (
    [ReportRunId] BIGINT IDENTITY(1,1) NOT NULL,
    [ReportDefinitionId] BIGINT NOT NULL,
    [FacilityId] BIGINT NOT NULL,
    [RequestedByUserId] BIGINT NOT NULL,
    [RequestedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_report_ReportRun_At DEFAULT (SYSUTCDATETIME()),
    [Status] VARCHAR(20) NOT NULL CONSTRAINT DF_report_ReportRun_Status DEFAULT ('Queued'),
    [FilterJson] NVARCHAR(MAX) NULL,
    [OutputFormat] VARCHAR(12) NOT NULL,
    [StorageKey] NVARCHAR(500) NULL,
    [FinishedAt] DATETIME2(3) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_report_ReportRun_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_report_ReportRun] PRIMARY KEY CLUSTERED ([ReportRunId] ASC),
    CONSTRAINT [CK_report_ReportRun_1] CHECK ([Status] IN ('Queued','Running','Completed','Failed')),
    CONSTRAINT [CK_report_ReportRun_2] CHECK ([OutputFormat] IN ('PDF','XLSX','CSV','HTML')),
    CONSTRAINT [CK_report_ReportRun_3] CHECK ([FilterJson] IS NULL OR ISJSON([FilterJson])=1)
);
GO

CREATE TABLE [report].[ReportSubmission] (
    [ReportSubmissionId] BIGINT IDENTITY(1,1) NOT NULL,
    [ReportRunId] BIGINT NOT NULL,
    [SubmittedByUserId] BIGINT NOT NULL,
    [SubmittedAt] DATETIME2(3) NOT NULL,
    [Destination] NVARCHAR(240) NOT NULL,
    [ExternalReceiptNo] NVARCHAR(140) NULL,
    [Status] VARCHAR(20) NOT NULL,
    [Notes] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_report_ReportSubmission_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_report_ReportSubmission] PRIMARY KEY CLUSTERED ([ReportSubmissionId] ASC),
    CONSTRAINT [CK_report_ReportSubmission_1] CHECK ([Status] IN ('Submitted','Accepted','Rejected'))
);
GO

CREATE TABLE [cfg].[Setting] (
    [SettingId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NULL,
    [SettingKey] VARCHAR(130) NOT NULL,
    [SettingValue] NVARCHAR(MAX) NULL,
    [ValueType] VARCHAR(20) NOT NULL CONSTRAINT DF_cfg_Setting_Type DEFAULT ('String'),
    [LastModifiedByUserId] BIGINT NULL,
    [ModifiedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_cfg_Setting_At DEFAULT (SYSUTCDATETIME()),
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_cfg_Setting_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_cfg_Setting] PRIMARY KEY CLUSTERED ([SettingId] ASC),
    CONSTRAINT [CK_cfg_Setting_1] CHECK ([ValueType] IN ('String','Number','Boolean','Json'))
);
GO

CREATE TABLE [cfg].[AlertThreshold] (
    [AlertThresholdId] BIGINT IDENTITY(1,1) NOT NULL,
    [FacilityId] BIGINT NULL,
    [RuleCode] VARCHAR(85) NOT NULL,
    [ProductTypeId] BIGINT NULL,
    [EquipmentCategoryId] BIGINT NULL,
    [LowerLimit] DECIMAL(11,3) NULL,
    [UpperLimit] DECIMAL(11,3) NULL,
    [Severity] VARCHAR(20) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT DF_cfg_AlertThreshold_Active DEFAULT (1),
    [Instructions] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT DF_cfg_AlertThreshold_CreatedAt DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_cfg_AlertThreshold] PRIMARY KEY CLUSTERED ([AlertThresholdId] ASC),
    CONSTRAINT [CK_cfg_AlertThreshold_1] CHECK ([LowerLimit] IS NULL OR [UpperLimit] IS NULL OR [UpperLimit]>[LowerLimit]),
    CONSTRAINT [CK_cfg_AlertThreshold_2] CHECK ([Severity] IN ('Info','Warning','Critical'))
);
GO

CREATE TABLE [audit].[AuditEvent] (
    [AuditEventId] BIGINT IDENTITY(1,1) NOT NULL,
    [AccountUserId] BIGINT NULL,
    [FacilityId] BIGINT NULL,
    [OccurredAt] DATETIME2(3) NOT NULL CONSTRAINT DF_audit_AuditEvent_At DEFAULT (SYSUTCDATETIME()),
    [EntityType] VARCHAR(120) NOT NULL,
    [EntityId] BIGINT NULL,
    [ActionType] VARCHAR(80) NOT NULL,
    [CorrelationId] UNIQUEIDENTIFIER NULL,
    [RemoteAddress] VARCHAR(45) NULL,
    [BeforeJson] NVARCHAR(MAX) NULL,
    [AfterJson] NVARCHAR(MAX) NULL,
    [EventDetails] NVARCHAR(1600) NULL,
    CONSTRAINT [PK_audit_AuditEvent] PRIMARY KEY CLUSTERED ([AuditEventId] ASC),
    CONSTRAINT [CK_audit_AuditEvent_1] CHECK ([BeforeJson] IS NULL OR ISJSON([BeforeJson])=1),
    CONSTRAINT [CK_audit_AuditEvent_2] CHECK ([AfterJson] IS NULL OR ISJSON([AfterJson])=1)
);
GO

ALTER TABLE [org].[Department] WITH CHECK ADD CONSTRAINT [FK_org_Department_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [org].[Department] WITH CHECK ADD CONSTRAINT [FK_org_Department_ParentDepartmentId] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [org].[Bed] WITH CHECK ADD CONSTRAINT [FK_org_Bed_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [org].[DutyShift] WITH CHECK ADD CONSTRAINT [FK_org_DutyShift_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [org].[DutyShift] WITH CHECK ADD CONSTRAINT [FK_org_DutyShift_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [sec].[RolePermission] WITH CHECK ADD CONSTRAINT [FK_sec_RolePermission_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [sec].[Role] ([RoleId]);
GO

ALTER TABLE [sec].[RolePermission] WITH CHECK ADD CONSTRAINT [FK_sec_RolePermission_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [sec].[Permission] ([PermissionId]);
GO

ALTER TABLE [sec].[UserRole] WITH CHECK ADD CONSTRAINT [FK_sec_UserRole_AccountUserId] FOREIGN KEY ([AccountUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [sec].[UserRole] WITH CHECK ADD CONSTRAINT [FK_sec_UserRole_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [sec].[Role] ([RoleId]);
GO

ALTER TABLE [sec].[UserRole] WITH CHECK ADD CONSTRAINT [FK_sec_UserRole_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [sec].[UserRole] WITH CHECK ADD CONSTRAINT [FK_sec_UserRole_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [sec].[UserSession] WITH CHECK ADD CONSTRAINT [FK_sec_UserSession_AccountUserId] FOREIGN KEY ([AccountUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [patient].[PatientIdentifier] WITH CHECK ADD CONSTRAINT [FK_patient_PatientIdentifier_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [patient].[Patient] ([PatientId]);
GO

ALTER TABLE [patient].[PatientIdentifier] WITH CHECK ADD CONSTRAINT [FK_patient_PatientIdentifier_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [patient].[Encounter] WITH CHECK ADD CONSTRAINT [FK_patient_Encounter_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [patient].[Patient] ([PatientId]);
GO

ALTER TABLE [patient].[Encounter] WITH CHECK ADD CONSTRAINT [FK_patient_Encounter_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [patient].[Encounter] WITH CHECK ADD CONSTRAINT [FK_patient_Encounter_AttendingPhysicianId] FOREIGN KEY ([AttendingPhysicianId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [patient].[EncounterLocation] WITH CHECK ADD CONSTRAINT [FK_patient_EncounterLocation_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [patient].[EncounterLocation] WITH CHECK ADD CONSTRAINT [FK_patient_EncounterLocation_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [patient].[EncounterLocation] WITH CHECK ADD CONSTRAINT [FK_patient_EncounterLocation_BedId] FOREIGN KEY ([BedId]) REFERENCES [org].[Bed] ([BedId]);
GO

ALTER TABLE [patient].[EncounterLocation] WITH CHECK ADD CONSTRAINT [FK_patient_EncounterLocation_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [patient].[Wristband] WITH CHECK ADD CONSTRAINT [FK_patient_Wristband_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [patient].[Wristband] WITH CHECK ADD CONSTRAINT [FK_patient_Wristband_PrintedByUserId] FOREIGN KEY ([PrintedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [patient].[IdentityVerification] WITH CHECK ADD CONSTRAINT [FK_patient_IdentityVerification_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [patient].[IdentityVerification] WITH CHECK ADD CONSTRAINT [FK_patient_IdentityVerification_WristbandId] FOREIGN KEY ([WristbandId]) REFERENCES [patient].[Wristband] ([WristbandId]);
GO

ALTER TABLE [patient].[IdentityVerification] WITH CHECK ADD CONSTRAINT [FK_patient_IdentityVerification_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [patient].[HistoricalTransfusion] WITH CHECK ADD CONSTRAINT [FK_patient_HistoricalTransfusion_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [patient].[Patient] ([PatientId]);
GO

ALTER TABLE [patient].[HistoricalTransfusion] WITH CHECK ADD CONSTRAINT [FK_patient_HistoricalTransfusion_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [patient].[DischargeReview] WITH CHECK ADD CONSTRAINT [FK_patient_DischargeReview_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [patient].[DischargeReview] WITH CHECK ADD CONSTRAINT [FK_patient_DischargeReview_ReviewerUserId] FOREIGN KEY ([ReviewerUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [patient].[DischargeReviewItem] WITH CHECK ADD CONSTRAINT [FK_patient_DischargeReviewItem_DischargeReviewId] FOREIGN KEY ([DischargeReviewId]) REFERENCES [patient].[DischargeReview] ([DischargeReviewId]);
GO

ALTER TABLE [clinical].[BloodRequest] WITH CHECK ADD CONSTRAINT [FK_clinical_BloodRequest_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [clinical].[BloodRequest] WITH CHECK ADD CONSTRAINT [FK_clinical_BloodRequest_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[RequestClinicalData] WITH CHECK ADD CONSTRAINT [FK_clinical_RequestClinicalData_BloodRequestId] FOREIGN KEY ([BloodRequestId]) REFERENCES [clinical].[BloodRequest] ([BloodRequestId]);
GO

ALTER TABLE [clinical].[BloodRequestItem] WITH CHECK ADD CONSTRAINT [FK_clinical_BloodRequestItem_BloodRequestId] FOREIGN KEY ([BloodRequestId]) REFERENCES [clinical].[BloodRequest] ([BloodRequestId]);
GO

ALTER TABLE [clinical].[BloodRequestItem] WITH CHECK ADD CONSTRAINT [FK_clinical_BloodRequestItem_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [blood].[ProductType] ([ProductTypeId]);
GO

ALTER TABLE [clinical].[RequestApproval] WITH CHECK ADD CONSTRAINT [FK_clinical_RequestApproval_BloodRequestId] FOREIGN KEY ([BloodRequestId]) REFERENCES [clinical].[BloodRequest] ([BloodRequestId]);
GO

ALTER TABLE [clinical].[RequestApproval] WITH CHECK ADD CONSTRAINT [FK_clinical_RequestApproval_ReviewerUserId] FOREIGN KEY ([ReviewerUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[ClinicalTask] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalTask_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [clinical].[ClinicalTask] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalTask_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [clinical].[ClinicalTask] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalTask_BloodRequestId] FOREIGN KEY ([BloodRequestId]) REFERENCES [clinical].[BloodRequest] ([BloodRequestId]);
GO

ALTER TABLE [clinical].[ClinicalTask] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalTask_AssignedUserId] FOREIGN KEY ([AssignedUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[ClinicalTask] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalTask_AssignedRoleId] FOREIGN KEY ([AssignedRoleId]) REFERENCES [sec].[Role] ([RoleId]);
GO

ALTER TABLE [clinical].[ClinicalTask] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalTask_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[ClinicalNote] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalNote_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [clinical].[ClinicalNote] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalNote_BloodRequestId] FOREIGN KEY ([BloodRequestId]) REFERENCES [clinical].[BloodRequest] ([BloodRequestId]);
GO

ALTER TABLE [clinical].[ClinicalNote] WITH CHECK ADD CONSTRAINT [FK_clinical_ClinicalNote_WrittenByUserId] FOREIGN KEY ([WrittenByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[Transfusion] WITH CHECK ADD CONSTRAINT [FK_clinical_Transfusion_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [clinical].[Transfusion] WITH CHECK ADD CONSTRAINT [FK_clinical_Transfusion_BloodRequestItemId] FOREIGN KEY ([BloodRequestItemId]) REFERENCES [clinical].[BloodRequestItem] ([BloodRequestItemId]);
GO

ALTER TABLE [clinical].[Transfusion] WITH CHECK ADD CONSTRAINT [FK_clinical_Transfusion_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [clinical].[Transfusion] WITH CHECK ADD CONSTRAINT [FK_clinical_Transfusion_StartedByUserId] FOREIGN KEY ([StartedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[Transfusion] WITH CHECK ADD CONSTRAINT [FK_clinical_Transfusion_EndedByUserId] FOREIGN KEY ([EndedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[TransfusionChecklist] WITH CHECK ADD CONSTRAINT [FK_clinical_TransfusionChecklist_TransfusionId] FOREIGN KEY ([TransfusionId]) REFERENCES [clinical].[Transfusion] ([TransfusionId]);
GO

ALTER TABLE [clinical].[TransfusionChecklist] WITH CHECK ADD CONSTRAINT [FK_clinical_TransfusionChecklist_CheckedByUserId] FOREIGN KEY ([CheckedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[VitalObservation] WITH CHECK ADD CONSTRAINT [FK_clinical_VitalObservation_TransfusionId] FOREIGN KEY ([TransfusionId]) REFERENCES [clinical].[Transfusion] ([TransfusionId]);
GO

ALTER TABLE [clinical].[VitalObservation] WITH CHECK ADD CONSTRAINT [FK_clinical_VitalObservation_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[TransfusionAction] WITH CHECK ADD CONSTRAINT [FK_clinical_TransfusionAction_TransfusionId] FOREIGN KEY ([TransfusionId]) REFERENCES [clinical].[Transfusion] ([TransfusionId]);
GO

ALTER TABLE [clinical].[TransfusionAction] WITH CHECK ADD CONSTRAINT [FK_clinical_TransfusionAction_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [clinical].[TransfusionSignoff] WITH CHECK ADD CONSTRAINT [FK_clinical_TransfusionSignoff_TransfusionId] FOREIGN KEY ([TransfusionId]) REFERENCES [clinical].[Transfusion] ([TransfusionId]);
GO

ALTER TABLE [clinical].[TransfusionSignoff] WITH CHECK ADD CONSTRAINT [FK_clinical_TransfusionSignoff_SignerUserId] FOREIGN KEY ([SignerUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[StockOrder] WITH CHECK ADD CONSTRAINT [FK_blood_StockOrder_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[StockOrder] WITH CHECK ADD CONSTRAINT [FK_blood_StockOrder_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [blood].[Supplier] ([SupplierId]);
GO

ALTER TABLE [blood].[StockOrder] WITH CHECK ADD CONSTRAINT [FK_blood_StockOrder_OrderedByUserId] FOREIGN KEY ([OrderedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[StockOrderLine] WITH CHECK ADD CONSTRAINT [FK_blood_StockOrderLine_StockOrderId] FOREIGN KEY ([StockOrderId]) REFERENCES [blood].[StockOrder] ([StockOrderId]);
GO

ALTER TABLE [blood].[StockOrderLine] WITH CHECK ADD CONSTRAINT [FK_blood_StockOrderLine_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [blood].[ProductType] ([ProductTypeId]);
GO

ALTER TABLE [blood].[StockOrderLine] WITH CHECK ADD CONSTRAINT [FK_blood_StockOrderLine_BloodGroupId] FOREIGN KEY ([BloodGroupId]) REFERENCES [blood].[BloodGroup] ([BloodGroupId]);
GO

ALTER TABLE [blood].[StockReceipt] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceipt_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[StockReceipt] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceipt_StockOrderId] FOREIGN KEY ([StockOrderId]) REFERENCES [blood].[StockOrder] ([StockOrderId]);
GO

ALTER TABLE [blood].[StockReceipt] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceipt_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [blood].[Supplier] ([SupplierId]);
GO

ALTER TABLE [blood].[StockReceipt] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceipt_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[BloodUnit] WITH CHECK ADD CONSTRAINT [FK_blood_BloodUnit_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [blood].[ProductType] ([ProductTypeId]);
GO

ALTER TABLE [blood].[BloodUnit] WITH CHECK ADD CONSTRAINT [FK_blood_BloodUnit_BloodGroupId] FOREIGN KEY ([BloodGroupId]) REFERENCES [blood].[BloodGroup] ([BloodGroupId]);
GO

ALTER TABLE [blood].[BloodUnit] WITH CHECK ADD CONSTRAINT [FK_blood_BloodUnit_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [blood].[Supplier] ([SupplierId]);
GO

ALTER TABLE [blood].[BloodUnit] WITH CHECK ADD CONSTRAINT [FK_blood_BloodUnit_CurrentFacilityId] FOREIGN KEY ([CurrentFacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[BloodUnit] WITH CHECK ADD CONSTRAINT [FK_blood_BloodUnit_StockReceiptId] FOREIGN KEY ([StockReceiptId]) REFERENCES [blood].[StockReceipt] ([StockReceiptId]);
GO

ALTER TABLE [blood].[StockReceiptLine] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceiptLine_StockReceiptId] FOREIGN KEY ([StockReceiptId]) REFERENCES [blood].[StockReceipt] ([StockReceiptId]);
GO

ALTER TABLE [blood].[StockReceiptLine] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceiptLine_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[StockReceiptLine] WITH CHECK ADD CONSTRAINT [FK_blood_StockReceiptLine_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[UnitReservation] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReservation_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[UnitReservation] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReservation_BloodRequestItemId] FOREIGN KEY ([BloodRequestItemId]) REFERENCES [clinical].[BloodRequestItem] ([BloodRequestItemId]);
GO

ALTER TABLE [blood].[UnitReservation] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReservation_ReservedByUserId] FOREIGN KEY ([ReservedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_BloodRequestItemId] FOREIGN KEY ([BloodRequestItemId]) REFERENCES [clinical].[BloodRequestItem] ([BloodRequestItemId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_PatientBloodGroupId] FOREIGN KEY ([PatientBloodGroupId]) REFERENCES [blood].[BloodGroup] ([BloodGroupId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_UnitLabelBloodGroupId] FOREIGN KEY ([UnitLabelBloodGroupId]) REFERENCES [blood].[BloodGroup] ([BloodGroupId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_VerifiedUnitBloodGroupId] FOREIGN KEY ([VerifiedUnitBloodGroupId]) REFERENCES [blood].[BloodGroup] ([BloodGroupId]);
GO

ALTER TABLE [blood].[Crossmatch] WITH CHECK ADD CONSTRAINT [FK_blood_Crossmatch_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[CrossmatchTest] WITH CHECK ADD CONSTRAINT [FK_blood_CrossmatchTest_CrossmatchId] FOREIGN KEY ([CrossmatchId]) REFERENCES [blood].[Crossmatch] ([CrossmatchId]);
GO

ALTER TABLE [blood].[UnitIssue] WITH CHECK ADD CONSTRAINT [FK_blood_UnitIssue_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[UnitIssue] WITH CHECK ADD CONSTRAINT [FK_blood_UnitIssue_BloodRequestItemId] FOREIGN KEY ([BloodRequestItemId]) REFERENCES [clinical].[BloodRequestItem] ([BloodRequestItemId]);
GO

ALTER TABLE [blood].[UnitIssue] WITH CHECK ADD CONSTRAINT [FK_blood_UnitIssue_IssuedByUserId] FOREIGN KEY ([IssuedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[UnitIssue] WITH CHECK ADD CONSTRAINT [FK_blood_UnitIssue_IssuedToDepartmentId] FOREIGN KEY ([IssuedToDepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [blood].[UnitIssue] WITH CHECK ADD CONSTRAINT [FK_blood_UnitIssue_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_blood_Dispatch_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_blood_Dispatch_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [blood].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_blood_Dispatch_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_blood_Dispatch_CourierUserId] FOREIGN KEY ([CourierUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[Dispatch] WITH CHECK ADD CONSTRAINT [FK_blood_Dispatch_ReceiverUserId] FOREIGN KEY ([ReceiverUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[DispatchItem] WITH CHECK ADD CONSTRAINT [FK_blood_DispatchItem_DispatchId] FOREIGN KEY ([DispatchId]) REFERENCES [blood].[Dispatch] ([DispatchId]);
GO

ALTER TABLE [blood].[DispatchItem] WITH CHECK ADD CONSTRAINT [FK_blood_DispatchItem_UnitIssueId] FOREIGN KEY ([UnitIssueId]) REFERENCES [blood].[UnitIssue] ([UnitIssueId]);
GO

ALTER TABLE [blood].[UnitReturn] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReturn_UnitIssueId] FOREIGN KEY ([UnitIssueId]) REFERENCES [blood].[UnitIssue] ([UnitIssueId]);
GO

ALTER TABLE [blood].[UnitReturn] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReturn_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[UnitReturn] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReturn_ReturnedByUserId] FOREIGN KEY ([ReturnedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[UnitReturn] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReturn_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[UnitReturn] WITH CHECK ADD CONSTRAINT [FK_blood_UnitReturn_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[UnitWaste] WITH CHECK ADD CONSTRAINT [FK_blood_UnitWaste_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[UnitWaste] WITH CHECK ADD CONSTRAINT [FK_blood_UnitWaste_UnitReturnId] FOREIGN KEY ([UnitReturnId]) REFERENCES [blood].[UnitReturn] ([UnitReturnId]);
GO

ALTER TABLE [blood].[UnitWaste] WITH CHECK ADD CONSTRAINT [FK_blood_UnitWaste_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[UnitWaste] WITH CHECK ADD CONSTRAINT [FK_blood_UnitWaste_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[ThawProcess] WITH CHECK ADD CONSTRAINT [FK_blood_ThawProcess_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[ThawProcess] WITH CHECK ADD CONSTRAINT [FK_blood_ThawProcess_BloodRequestItemId] FOREIGN KEY ([BloodRequestItemId]) REFERENCES [clinical].[BloodRequestItem] ([BloodRequestItemId]);
GO

ALTER TABLE [blood].[ThawProcess] WITH CHECK ADD CONSTRAINT [FK_blood_ThawProcess_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [blood].[ThawProcess] WITH CHECK ADD CONSTRAINT [FK_blood_ThawProcess_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[InterfacilityTransfer] WITH CHECK ADD CONSTRAINT [FK_blood_InterfacilityTransfer_SourceFacilityId] FOREIGN KEY ([SourceFacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[InterfacilityTransfer] WITH CHECK ADD CONSTRAINT [FK_blood_InterfacilityTransfer_DestinationFacilityId] FOREIGN KEY ([DestinationFacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[InterfacilityTransfer] WITH CHECK ADD CONSTRAINT [FK_blood_InterfacilityTransfer_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[InterfacilityTransfer] WITH CHECK ADD CONSTRAINT [FK_blood_InterfacilityTransfer_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[TransferItem] WITH CHECK ADD CONSTRAINT [FK_blood_TransferItem_InterfacilityTransferId] FOREIGN KEY ([InterfacilityTransferId]) REFERENCES [blood].[InterfacilityTransfer] ([InterfacilityTransferId]);
GO

ALTER TABLE [blood].[TransferItem] WITH CHECK ADD CONSTRAINT [FK_blood_TransferItem_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[InventoryMovement] WITH CHECK ADD CONSTRAINT [FK_blood_InventoryMovement_BloodUnitId] FOREIGN KEY ([BloodUnitId]) REFERENCES [blood].[BloodUnit] ([BloodUnitId]);
GO

ALTER TABLE [blood].[InventoryMovement] WITH CHECK ADD CONSTRAINT [FK_blood_InventoryMovement_FromFacilityId] FOREIGN KEY ([FromFacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[InventoryMovement] WITH CHECK ADD CONSTRAINT [FK_blood_InventoryMovement_ToFacilityId] FOREIGN KEY ([ToFacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[InventoryMovement] WITH CHECK ADD CONSTRAINT [FK_blood_InventoryMovement_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[DailyQualityControl] WITH CHECK ADD CONSTRAINT [FK_blood_DailyQualityControl_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [blood].[DailyQualityControl] WITH CHECK ADD CONSTRAINT [FK_blood_DailyQualityControl_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [blood].[DailyQualityControl] WITH CHECK ADD CONSTRAINT [FK_blood_DailyQualityControl_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [org].[DutyShift] ([DutyShiftId]);
GO

ALTER TABLE [blood].[DailyQualityControl] WITH CHECK ADD CONSTRAINT [FK_blood_DailyQualityControl_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[DailyQualityControl] WITH CHECK ADD CONSTRAINT [FK_blood_DailyQualityControl_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [blood].[QualityControlResult] WITH CHECK ADD CONSTRAINT [FK_blood_QualityControlResult_DailyQualityControlId] FOREIGN KEY ([DailyQualityControlId]) REFERENCES [blood].[DailyQualityControl] ([DailyQualityControlId]);
GO

ALTER TABLE [equip].[Equipment] WITH CHECK ADD CONSTRAINT [FK_equip_Equipment_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [equip].[Equipment] WITH CHECK ADD CONSTRAINT [FK_equip_Equipment_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [equip].[Equipment] WITH CHECK ADD CONSTRAINT [FK_equip_Equipment_EquipmentCategoryId] FOREIGN KEY ([EquipmentCategoryId]) REFERENCES [equip].[EquipmentCategory] ([EquipmentCategoryId]);
GO

ALTER TABLE [equip].[MaintenancePlan] WITH CHECK ADD CONSTRAINT [FK_equip_MaintenancePlan_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [equip].[MaintenanceRecord] WITH CHECK ADD CONSTRAINT [FK_equip_MaintenanceRecord_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [equip].[MaintenanceRecord] WITH CHECK ADD CONSTRAINT [FK_equip_MaintenanceRecord_MaintenancePlanId] FOREIGN KEY ([MaintenancePlanId]) REFERENCES [equip].[MaintenancePlan] ([MaintenancePlanId]);
GO

ALTER TABLE [equip].[MaintenanceRecord] WITH CHECK ADD CONSTRAINT [FK_equip_MaintenanceRecord_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [equip].[TemperatureReading] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureReading_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [equip].[TemperatureReading] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureReading_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [org].[DutyShift] ([DutyShiftId]);
GO

ALTER TABLE [equip].[TemperatureReading] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureReading_RecorderUserId] FOREIGN KEY ([RecorderUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [equip].[TemperatureExcursion] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureExcursion_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [equip].[TemperatureExcursion] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureExcursion_FirstReadingId] FOREIGN KEY ([FirstReadingId]) REFERENCES [equip].[TemperatureReading] ([TemperatureReadingId]);
GO

ALTER TABLE [equip].[TemperatureExcursion] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureExcursion_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [equip].[TemperatureExcursion] WITH CHECK ADD CONSTRAINT [FK_equip_TemperatureExcursion_SupervisorApprovedByUserId] FOREIGN KEY ([SupervisorApprovedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [equip].[EquipmentIncident] WITH CHECK ADD CONSTRAINT [FK_equip_EquipmentIncident_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [equip].[Equipment] ([EquipmentId]);
GO

ALTER TABLE [equip].[EquipmentIncident] WITH CHECK ADD CONSTRAINT [FK_equip_EquipmentIncident_ReportedByUserId] FOREIGN KEY ([ReportedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [equip].[EquipmentIncidentAction] WITH CHECK ADD CONSTRAINT [FK_equip_EquipmentIncidentAction_EquipmentIncidentId] FOREIGN KEY ([EquipmentIncidentId]) REFERENCES [equip].[EquipmentIncident] ([EquipmentIncidentId]);
GO

ALTER TABLE [equip].[EquipmentIncidentAction] WITH CHECK ADD CONSTRAINT [FK_equip_EquipmentIncidentAction_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[AdverseEvent] WITH CHECK ADD CONSTRAINT [FK_hv_AdverseEvent_TransfusionId] FOREIGN KEY ([TransfusionId]) REFERENCES [clinical].[Transfusion] ([TransfusionId]);
GO

ALTER TABLE [hv].[AdverseEvent] WITH CHECK ADD CONSTRAINT [FK_hv_AdverseEvent_ReportedByUserId] FOREIGN KEY ([ReportedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[EventSymptom] WITH CHECK ADD CONSTRAINT [FK_hv_EventSymptom_AdverseEventId] FOREIGN KEY ([AdverseEventId]) REFERENCES [hv].[AdverseEvent] ([AdverseEventId]);
GO

ALTER TABLE [hv].[PhysicianEventAssessment] WITH CHECK ADD CONSTRAINT [FK_hv_PhysicianEventAssessment_AdverseEventId] FOREIGN KEY ([AdverseEventId]) REFERENCES [hv].[AdverseEvent] ([AdverseEventId]);
GO

ALTER TABLE [hv].[PhysicianEventAssessment] WITH CHECK ADD CONSTRAINT [FK_hv_PhysicianEventAssessment_AssessedByUserId] FOREIGN KEY ([AssessedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[EventInvestigation] WITH CHECK ADD CONSTRAINT [FK_hv_EventInvestigation_AdverseEventId] FOREIGN KEY ([AdverseEventId]) REFERENCES [hv].[AdverseEvent] ([AdverseEventId]);
GO

ALTER TABLE [hv].[EventInvestigation] WITH CHECK ADD CONSTRAINT [FK_hv_EventInvestigation_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[RcaCase] WITH CHECK ADD CONSTRAINT [FK_hv_RcaCase_AdverseEventId] FOREIGN KEY ([AdverseEventId]) REFERENCES [hv].[AdverseEvent] ([AdverseEventId]);
GO

ALTER TABLE [hv].[RcaCase] WITH CHECK ADD CONSTRAINT [FK_hv_RcaCase_EquipmentIncidentId] FOREIGN KEY ([EquipmentIncidentId]) REFERENCES [equip].[EquipmentIncident] ([EquipmentIncidentId]);
GO

ALTER TABLE [hv].[RcaCase] WITH CHECK ADD CONSTRAINT [FK_hv_RcaCase_OpenedByUserId] FOREIGN KEY ([OpenedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[RcaCause] WITH CHECK ADD CONSTRAINT [FK_hv_RcaCause_RcaCaseId] FOREIGN KEY ([RcaCaseId]) REFERENCES [hv].[RcaCase] ([RcaCaseId]);
GO

ALTER TABLE [hv].[CorrectiveAction] WITH CHECK ADD CONSTRAINT [FK_hv_CorrectiveAction_RcaCaseId] FOREIGN KEY ([RcaCaseId]) REFERENCES [hv].[RcaCase] ([RcaCaseId]);
GO

ALTER TABLE [hv].[CorrectiveAction] WITH CHECK ADD CONSTRAINT [FK_hv_CorrectiveAction_EquipmentIncidentId] FOREIGN KEY ([EquipmentIncidentId]) REFERENCES [equip].[EquipmentIncident] ([EquipmentIncidentId]);
GO

ALTER TABLE [hv].[CorrectiveAction] WITH CHECK ADD CONSTRAINT [FK_hv_CorrectiveAction_AuditId] FOREIGN KEY ([AuditId]) REFERENCES [hv].[ClinicalAudit] ([ClinicalAuditId]);
GO

ALTER TABLE [hv].[CorrectiveAction] WITH CHECK ADD CONSTRAINT [FK_hv_CorrectiveAction_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[CorrectiveAction] WITH CHECK ADD CONSTRAINT [FK_hv_CorrectiveAction_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[CommitteeMeeting] WITH CHECK ADD CONSTRAINT [FK_hv_CommitteeMeeting_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [hv].[CommitteeMeeting] WITH CHECK ADD CONSTRAINT [FK_hv_CommitteeMeeting_ChairUserId] FOREIGN KEY ([ChairUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[CommitteeMeeting] WITH CHECK ADD CONSTRAINT [FK_hv_CommitteeMeeting_SecretaryUserId] FOREIGN KEY ([SecretaryUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[MeetingAttendee] WITH CHECK ADD CONSTRAINT [FK_hv_MeetingAttendee_CommitteeMeetingId] FOREIGN KEY ([CommitteeMeetingId]) REFERENCES [hv].[CommitteeMeeting] ([CommitteeMeetingId]);
GO

ALTER TABLE [hv].[MeetingAttendee] WITH CHECK ADD CONSTRAINT [FK_hv_MeetingAttendee_AccountUserId] FOREIGN KEY ([AccountUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[MeetingDecision] WITH CHECK ADD CONSTRAINT [FK_hv_MeetingDecision_CommitteeMeetingId] FOREIGN KEY ([CommitteeMeetingId]) REFERENCES [hv].[CommitteeMeeting] ([CommitteeMeetingId]);
GO

ALTER TABLE [hv].[MeetingDecision] WITH CHECK ADD CONSTRAINT [FK_hv_MeetingDecision_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[ClinicalAudit] WITH CHECK ADD CONSTRAINT [FK_hv_ClinicalAudit_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [hv].[ClinicalAudit] WITH CHECK ADD CONSTRAINT [FK_hv_ClinicalAudit_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [org].[Department] ([DepartmentId]);
GO

ALTER TABLE [hv].[ClinicalAudit] WITH CHECK ADD CONSTRAINT [FK_hv_ClinicalAudit_AuditorUserId] FOREIGN KEY ([AuditorUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[AuditAnswer] WITH CHECK ADD CONSTRAINT [FK_hv_AuditAnswer_ClinicalAuditId] FOREIGN KEY ([ClinicalAuditId]) REFERENCES [hv].[ClinicalAudit] ([ClinicalAuditId]);
GO

ALTER TABLE [hv].[AuditAnswer] WITH CHECK ADD CONSTRAINT [FK_hv_AuditAnswer_AuditQuestionId] FOREIGN KEY ([AuditQuestionId]) REFERENCES [hv].[AuditQuestion] ([AuditQuestionId]);
GO

ALTER TABLE [hv].[TrainingCourse] WITH CHECK ADD CONSTRAINT [FK_hv_TrainingCourse_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [hv].[TrainingSession] WITH CHECK ADD CONSTRAINT [FK_hv_TrainingSession_TrainingCourseId] FOREIGN KEY ([TrainingCourseId]) REFERENCES [hv].[TrainingCourse] ([TrainingCourseId]);
GO

ALTER TABLE [hv].[TrainingSession] WITH CHECK ADD CONSTRAINT [FK_hv_TrainingSession_InstructorUserId] FOREIGN KEY ([InstructorUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[TrainingEnrollment] WITH CHECK ADD CONSTRAINT [FK_hv_TrainingEnrollment_TrainingSessionId] FOREIGN KEY ([TrainingSessionId]) REFERENCES [hv].[TrainingSession] ([TrainingSessionId]);
GO

ALTER TABLE [hv].[TrainingEnrollment] WITH CHECK ADD CONSTRAINT [FK_hv_TrainingEnrollment_AccountUserId] FOREIGN KEY ([AccountUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [hv].[TrainingExamResult] WITH CHECK ADD CONSTRAINT [FK_hv_TrainingExamResult_TrainingEnrollmentId] FOREIGN KEY ([TrainingEnrollmentId]) REFERENCES [hv].[TrainingEnrollment] ([TrainingEnrollmentId]);
GO

ALTER TABLE [hv].[DocumentLibrary] WITH CHECK ADD CONSTRAINT [FK_hv_DocumentLibrary_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [hv].[DocumentLibrary] WITH CHECK ADD CONSTRAINT [FK_hv_DocumentLibrary_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [integration].[Endpoint] WITH CHECK ADD CONSTRAINT [FK_integration_Endpoint_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [integration].[Message] WITH CHECK ADD CONSTRAINT [FK_integration_Message_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES [integration].[Endpoint] ([EndpointId]);
GO

ALTER TABLE [integration].[Message] WITH CHECK ADD CONSTRAINT [FK_integration_Message_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [patient].[Patient] ([PatientId]);
GO

ALTER TABLE [integration].[Message] WITH CHECK ADD CONSTRAINT [FK_integration_Message_EncounterId] FOREIGN KEY ([EncounterId]) REFERENCES [patient].[Encounter] ([EncounterId]);
GO

ALTER TABLE [integration].[MessageAttempt] WITH CHECK ADD CONSTRAINT [FK_integration_MessageAttempt_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [integration].[Message] ([MessageId]);
GO

ALTER TABLE [integration].[SyncJob] WITH CHECK ADD CONSTRAINT [FK_integration_SyncJob_EndpointId] FOREIGN KEY ([EndpointId]) REFERENCES [integration].[Endpoint] ([EndpointId]);
GO

ALTER TABLE [integration].[SyncJob] WITH CHECK ADD CONSTRAINT [FK_integration_SyncJob_TriggeredByUserId] FOREIGN KEY ([TriggeredByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [report].[ReportRun] WITH CHECK ADD CONSTRAINT [FK_report_ReportRun_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [report].[ReportDefinition] ([ReportDefinitionId]);
GO

ALTER TABLE [report].[ReportRun] WITH CHECK ADD CONSTRAINT [FK_report_ReportRun_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [report].[ReportRun] WITH CHECK ADD CONSTRAINT [FK_report_ReportRun_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [report].[ReportSubmission] WITH CHECK ADD CONSTRAINT [FK_report_ReportSubmission_ReportRunId] FOREIGN KEY ([ReportRunId]) REFERENCES [report].[ReportRun] ([ReportRunId]);
GO

ALTER TABLE [report].[ReportSubmission] WITH CHECK ADD CONSTRAINT [FK_report_ReportSubmission_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [cfg].[Setting] WITH CHECK ADD CONSTRAINT [FK_cfg_Setting_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [cfg].[Setting] WITH CHECK ADD CONSTRAINT [FK_cfg_Setting_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [cfg].[AlertThreshold] WITH CHECK ADD CONSTRAINT [FK_cfg_AlertThreshold_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

ALTER TABLE [cfg].[AlertThreshold] WITH CHECK ADD CONSTRAINT [FK_cfg_AlertThreshold_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [blood].[ProductType] ([ProductTypeId]);
GO

ALTER TABLE [cfg].[AlertThreshold] WITH CHECK ADD CONSTRAINT [FK_cfg_AlertThreshold_EquipmentCategoryId] FOREIGN KEY ([EquipmentCategoryId]) REFERENCES [equip].[EquipmentCategory] ([EquipmentCategoryId]);
GO

ALTER TABLE [audit].[AuditEvent] WITH CHECK ADD CONSTRAINT [FK_audit_AuditEvent_AccountUserId] FOREIGN KEY ([AccountUserId]) REFERENCES [sec].[AccountUser] ([AccountUserId]);
GO

ALTER TABLE [audit].[AuditEvent] WITH CHECK ADD CONSTRAINT [FK_audit_AuditEvent_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [org].[Facility] ([FacilityId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_org_Facility_1] ON [org].[Facility] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_org_Department_1] ON [org].[Department] ([FacilityId], [Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_org_Bed_1] ON [org].[Bed] ([DepartmentId], [BedCode]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_sec_AccountUser_1] ON [sec].[AccountUser] ([UserName]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_sec_Role_1] ON [sec].[Role] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_sec_Permission_1] ON [sec].[Permission] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_sec_RolePermission_1] ON [sec].[RolePermission] ([RoleId], [PermissionId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_sec_UserSession_1] ON [sec].[UserSession] ([RefreshTokenHash]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Wristband_1] ON [patient].[Wristband] ([Barcode]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_DischargeReviewItem_1] ON [patient].[DischargeReviewItem] ([DischargeReviewId], [ItemCode]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_clinical_BloodRequest_1] ON [clinical].[BloodRequest] ([RequestNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_clinical_RequestClinicalData_1] ON [clinical].[RequestClinicalData] ([BloodRequestId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_clinical_Transfusion_1] ON [clinical].[Transfusion] ([BloodUnitId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_clinical_TransfusionChecklist_1] ON [clinical].[TransfusionChecklist] ([TransfusionId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_clinical_TransfusionSignoff_1] ON [clinical].[TransfusionSignoff] ([TransfusionId], [SignoffRole]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_ProductType_1] ON [blood].[ProductType] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_BloodGroup_1] ON [blood].[BloodGroup] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_Supplier_1] ON [blood].[Supplier] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_StockOrder_1] ON [blood].[StockOrder] ([FacilityId], [OrderNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_StockReceipt_1] ON [blood].[StockReceipt] ([FacilityId], [ReceiptNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_BloodUnit_1] ON [blood].[BloodUnit] ([UnitCode]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_StockReceiptLine_1] ON [blood].[StockReceiptLine] ([BloodUnitId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_UnitIssue_1] ON [blood].[UnitIssue] ([IssueNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_Dispatch_1] ON [blood].[Dispatch] ([FacilityId], [DispatchNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_DispatchItem_1] ON [blood].[DispatchItem] ([UnitIssueId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_UnitWaste_1] ON [blood].[UnitWaste] ([WasteCode]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_InterfacilityTransfer_1] ON [blood].[InterfacilityTransfer] ([SourceFacilityId], [TransferNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_TransferItem_1] ON [blood].[TransferItem] ([InterfacilityTransferId], [BloodUnitId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_equip_EquipmentCategory_1] ON [equip].[EquipmentCategory] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_equip_Equipment_1] ON [equip].[Equipment] ([FacilityId], [AssetTag]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_equip_EquipmentIncident_1] ON [equip].[EquipmentIncident] ([IncidentNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_AdverseEvent_1] ON [hv].[AdverseEvent] ([EventNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_EventSymptom_1] ON [hv].[EventSymptom] ([AdverseEventId], [SymptomCode]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_RcaCase_1] ON [hv].[RcaCase] ([CaseNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_CommitteeMeeting_1] ON [hv].[CommitteeMeeting] ([FacilityId], [MeetingNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_MeetingDecision_1] ON [hv].[MeetingDecision] ([CommitteeMeetingId], [DecisionNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_AuditQuestion_1] ON [hv].[AuditQuestion] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_ClinicalAudit_1] ON [hv].[ClinicalAudit] ([FacilityId], [AuditNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_AuditAnswer_1] ON [hv].[AuditAnswer] ([ClinicalAuditId], [AuditQuestionId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_TrainingCourse_1] ON [hv].[TrainingCourse] ([FacilityId], [Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_hv_TrainingEnrollment_1] ON [hv].[TrainingEnrollment] ([TrainingSessionId], [AccountUserId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_integration_Endpoint_1] ON [integration].[Endpoint] ([FacilityId], [Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_integration_Message_1] ON [integration].[Message] ([CorrelationId]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_integration_MessageAttempt_1] ON [integration].[MessageAttempt] ([MessageId], [AttemptNo]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_report_ReportDefinition_1] ON [report].[ReportDefinition] ([Code]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Patient_NationalId] ON [patient].[Patient] ([NationalId]) WHERE [NationalId] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Patient_UnknownIdentity] ON [patient].[Patient] ([UnknownIdentityCode]) WHERE [UnknownIdentityCode] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Identifier_Global] ON [patient].[PatientIdentifier] ([IdentifierType], [IdentifierValue]) WHERE [FacilityId] IS NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Identifier_Facility] ON [patient].[PatientIdentifier] ([FacilityId], [IdentifierType], [IdentifierValue]) WHERE [FacilityId] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Encounter_HisNo] ON [patient].[Encounter] ([FacilityId], [HisEncounterNo]) WHERE [HisEncounterNo] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_EncounterLocation_Active] ON [patient].[EncounterLocation] ([EncounterId]) WHERE [ToAt] IS NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_EncounterLocation_OccupiedBed] ON [patient].[EncounterLocation] ([BedId]) WHERE [ToAt] IS NULL AND [BedId] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_patient_Wristband_Active] ON [patient].[Wristband] ([EncounterId]) WHERE [Status] = 'Active';
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_UnitReservation_Active] ON [blood].[UnitReservation] ([BloodUnitId]) WHERE [Status] = 'Active';
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_sec_UserRole_Active] ON [sec].[UserRole] ([AccountUserId], [RoleId], [FacilityId], [DepartmentId]) WHERE [IsActive] = 1;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_UnitIssue_Active] ON [blood].[UnitIssue] ([BloodUnitId]) WHERE [Status] IN ('Issued','InTransit','Received');
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_UnitReturn_UniqueIssue] ON [blood].[UnitReturn] ([UnitIssueId]) ;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_blood_UnitWaste_UniqueUnit] ON [blood].[UnitWaste] ([BloodUnitId]) ;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_cfg_Setting_Global] ON [cfg].[Setting] ([SettingKey]) WHERE [FacilityId] IS NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_cfg_Setting_Facility] ON [cfg].[Setting] ([FacilityId], [SettingKey]) WHERE [FacilityId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_org_Department_ParentDepartmentId] ON [org].[Department] ([ParentDepartmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_org_Department_Query1] ON [org].[Department] ([FacilityId], [IsActive]);
GO

CREATE NONCLUSTERED INDEX [IX_org_DutyShift_DepartmentId] ON [org].[DutyShift] ([DepartmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_org_DutyShift_Query1] ON [org].[DutyShift] ([FacilityId], [StartsAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_sec_AccountUser_Query1] ON [sec].[AccountUser] ([IsActive], [UserName]);
GO

CREATE NONCLUSTERED INDEX [IX_sec_RolePermission_PermissionId] ON [sec].[RolePermission] ([PermissionId]);
GO

CREATE NONCLUSTERED INDEX [IX_sec_UserRole_RoleId] ON [sec].[UserRole] ([RoleId]);
GO

CREATE NONCLUSTERED INDEX [IX_sec_UserRole_DepartmentId] ON [sec].[UserRole] ([DepartmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_sec_UserRole_Query1] ON [sec].[UserRole] ([AccountUserId], [IsActive]);
GO

CREATE NONCLUSTERED INDEX [IX_sec_UserRole_Query2] ON [sec].[UserRole] ([FacilityId], [DepartmentId], [RoleId]);
GO

CREATE NONCLUSTERED INDEX [IX_sec_UserSession_Query1] ON [sec].[UserSession] ([AccountUserId], [ExpiresAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_patient_Patient_Query1] ON [patient].[Patient] ([LastName], [FirstName]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_PatientIdentifier_PatientId] ON [patient].[PatientIdentifier] ([PatientId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_PatientIdentifier_FacilityId] ON [patient].[PatientIdentifier] ([FacilityId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_PatientIdentifier_Query1] ON [patient].[PatientIdentifier] ([IdentifierType], [IdentifierValue]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_Encounter_AttendingPhysicianId] ON [patient].[Encounter] ([AttendingPhysicianId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_Encounter_Query1] ON [patient].[Encounter] ([PatientId], [AdmittedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_patient_Encounter_Query2] ON [patient].[Encounter] ([FacilityId], [Status], [AdmittedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_patient_EncounterLocation_EncounterId] ON [patient].[EncounterLocation] ([EncounterId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_EncounterLocation_BedId] ON [patient].[EncounterLocation] ([BedId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_EncounterLocation_RecordedByUserId] ON [patient].[EncounterLocation] ([RecordedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_EncounterLocation_Query1] ON [patient].[EncounterLocation] ([DepartmentId], [ToAt], [FromAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_patient_Wristband_EncounterId] ON [patient].[Wristband] ([EncounterId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_Wristband_PrintedByUserId] ON [patient].[Wristband] ([PrintedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_IdentityVerification_WristbandId] ON [patient].[IdentityVerification] ([WristbandId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_IdentityVerification_VerifiedByUserId] ON [patient].[IdentityVerification] ([VerifiedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_IdentityVerification_Query1] ON [patient].[IdentityVerification] ([EncounterId], [VerifiedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_patient_HistoricalTransfusion_EncounterId] ON [patient].[HistoricalTransfusion] ([EncounterId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_HistoricalTransfusion_Query1] ON [patient].[HistoricalTransfusion] ([PatientId], [OccurredAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_patient_DischargeReview_ReviewerUserId] ON [patient].[DischargeReview] ([ReviewerUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_patient_DischargeReview_Query1] ON [patient].[DischargeReview] ([EncounterId], [ReviewedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_BloodRequest_RequestedByUserId] ON [clinical].[BloodRequest] ([RequestedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_BloodRequest_Query1] ON [clinical].[BloodRequest] ([EncounterId], [RequestedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_BloodRequest_Query2] ON [clinical].[BloodRequest] ([Status], [Priority], [RequestedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_BloodRequestItem_ProductTypeId] ON [clinical].[BloodRequestItem] ([ProductTypeId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_BloodRequestItem_Query1] ON [clinical].[BloodRequestItem] ([BloodRequestId], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_RequestApproval_ReviewerUserId] ON [clinical].[RequestApproval] ([ReviewerUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_RequestApproval_Query1] ON [clinical].[RequestApproval] ([BloodRequestId], [ApprovalStage], [DecidedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalTask_EncounterId] ON [clinical].[ClinicalTask] ([EncounterId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalTask_BloodRequestId] ON [clinical].[ClinicalTask] ([BloodRequestId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalTask_AssignedRoleId] ON [clinical].[ClinicalTask] ([AssignedRoleId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalTask_CreatedByUserId] ON [clinical].[ClinicalTask] ([CreatedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalTask_Query1] ON [clinical].[ClinicalTask] ([AssignedUserId], [Status], [DueAt]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalTask_Query2] ON [clinical].[ClinicalTask] ([FacilityId], [AssignedRoleId], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalNote_BloodRequestId] ON [clinical].[ClinicalNote] ([BloodRequestId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalNote_WrittenByUserId] ON [clinical].[ClinicalNote] ([WrittenByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_ClinicalNote_Query1] ON [clinical].[ClinicalNote] ([EncounterId], [WrittenAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Transfusion_BloodRequestItemId] ON [clinical].[Transfusion] ([BloodRequestItemId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Transfusion_StartedByUserId] ON [clinical].[Transfusion] ([StartedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Transfusion_EndedByUserId] ON [clinical].[Transfusion] ([EndedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Transfusion_Query1] ON [clinical].[Transfusion] ([EncounterId], [StartedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Transfusion_Query2] ON [clinical].[Transfusion] ([Status], [StartedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_TransfusionChecklist_CheckedByUserId] ON [clinical].[TransfusionChecklist] ([CheckedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_VitalObservation_RecordedByUserId] ON [clinical].[VitalObservation] ([RecordedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_VitalObservation_Query1] ON [clinical].[VitalObservation] ([TransfusionId], [ObservedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_TransfusionAction_PerformedByUserId] ON [clinical].[TransfusionAction] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_TransfusionAction_Query1] ON [clinical].[TransfusionAction] ([TransfusionId], [ActionAt]);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_TransfusionSignoff_SignerUserId] ON [clinical].[TransfusionSignoff] ([SignerUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockOrder_SupplierId] ON [blood].[StockOrder] ([SupplierId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockOrder_OrderedByUserId] ON [blood].[StockOrder] ([OrderedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockOrder_Query1] ON [blood].[StockOrder] ([FacilityId], [Status], [OrderedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockOrderLine_StockOrderId] ON [blood].[StockOrderLine] ([StockOrderId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockOrderLine_ProductTypeId] ON [blood].[StockOrderLine] ([ProductTypeId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockOrderLine_BloodGroupId] ON [blood].[StockOrderLine] ([BloodGroupId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockReceipt_StockOrderId] ON [blood].[StockReceipt] ([StockOrderId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockReceipt_SupplierId] ON [blood].[StockReceipt] ([SupplierId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockReceipt_ReceivedByUserId] ON [blood].[StockReceipt] ([ReceivedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockReceipt_Query1] ON [blood].[StockReceipt] ([FacilityId], [ReceivedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_BloodUnit_ProductTypeId] ON [blood].[BloodUnit] ([ProductTypeId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_BloodUnit_SupplierId] ON [blood].[BloodUnit] ([SupplierId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_BloodUnit_StockReceiptId] ON [blood].[BloodUnit] ([StockReceiptId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_BloodUnit_Query1] ON [blood].[BloodUnit] ([CurrentFacilityId], [Status], [ExpiryDate]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_BloodUnit_Query2] ON [blood].[BloodUnit] ([BloodGroupId], [ProductTypeId], [Status], [ExpiryDate]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockReceiptLine_StockReceiptId] ON [blood].[StockReceiptLine] ([StockReceiptId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_StockReceiptLine_VerifiedByUserId] ON [blood].[StockReceiptLine] ([VerifiedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReservation_BloodUnitId] ON [blood].[UnitReservation] ([BloodUnitId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReservation_ReservedByUserId] ON [blood].[UnitReservation] ([ReservedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReservation_Query1] ON [blood].[UnitReservation] ([BloodRequestItemId], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_PerformedByUserId] ON [blood].[Crossmatch] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_PatientBloodGroupId] ON [blood].[Crossmatch] ([PatientBloodGroupId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_UnitLabelBloodGroupId] ON [blood].[Crossmatch] ([UnitLabelBloodGroupId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_VerifiedUnitBloodGroupId] ON [blood].[Crossmatch] ([VerifiedUnitBloodGroupId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_ApprovedByUserId] ON [blood].[Crossmatch] ([ApprovedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_Query1] ON [blood].[Crossmatch] ([BloodRequestItemId], [PerformedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Crossmatch_Query2] ON [blood].[Crossmatch] ([BloodUnitId], [PerformedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_CrossmatchTest_Query1] ON [blood].[CrossmatchTest] ([CrossmatchId], [TestCode]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitIssue_BloodUnitId] ON [blood].[UnitIssue] ([BloodUnitId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitIssue_IssuedByUserId] ON [blood].[UnitIssue] ([IssuedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitIssue_ReceivedByUserId] ON [blood].[UnitIssue] ([ReceivedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitIssue_Query1] ON [blood].[UnitIssue] ([BloodRequestItemId], [IssuedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitIssue_Query2] ON [blood].[UnitIssue] ([IssuedToDepartmentId], [Status], [IssuedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Dispatch_CreatedByUserId] ON [blood].[Dispatch] ([CreatedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Dispatch_CourierUserId] ON [blood].[Dispatch] ([CourierUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Dispatch_ReceiverUserId] ON [blood].[Dispatch] ([ReceiverUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_Dispatch_Query1] ON [blood].[Dispatch] ([DepartmentId], [Status], [CreatedAtDispatch] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_DispatchItem_DispatchId] ON [blood].[DispatchItem] ([DispatchId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReturn_UnitIssueId] ON [blood].[UnitReturn] ([UnitIssueId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReturn_ReturnedByUserId] ON [blood].[UnitReturn] ([ReturnedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReturn_ReceivedByUserId] ON [blood].[UnitReturn] ([ReceivedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReturn_ReviewedByUserId] ON [blood].[UnitReturn] ([ReviewedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitReturn_Query1] ON [blood].[UnitReturn] ([BloodUnitId], [ReturnedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitWaste_UnitReturnId] ON [blood].[UnitWaste] ([UnitReturnId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitWaste_RecordedByUserId] ON [blood].[UnitWaste] ([RecordedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitWaste_ApprovedByUserId] ON [blood].[UnitWaste] ([ApprovedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_UnitWaste_Query1] ON [blood].[UnitWaste] ([BloodUnitId], [WasteDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_ThawProcess_BloodRequestItemId] ON [blood].[ThawProcess] ([BloodRequestItemId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_ThawProcess_EquipmentId] ON [blood].[ThawProcess] ([EquipmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_ThawProcess_PerformedByUserId] ON [blood].[ThawProcess] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_ThawProcess_Query1] ON [blood].[ThawProcess] ([BloodUnitId], [StartedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InterfacilityTransfer_DestinationFacilityId] ON [blood].[InterfacilityTransfer] ([DestinationFacilityId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InterfacilityTransfer_RequestedByUserId] ON [blood].[InterfacilityTransfer] ([RequestedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InterfacilityTransfer_ApprovedByUserId] ON [blood].[InterfacilityTransfer] ([ApprovedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_TransferItem_BloodUnitId] ON [blood].[TransferItem] ([BloodUnitId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InventoryMovement_FromFacilityId] ON [blood].[InventoryMovement] ([FromFacilityId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InventoryMovement_PerformedByUserId] ON [blood].[InventoryMovement] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InventoryMovement_Query1] ON [blood].[InventoryMovement] ([BloodUnitId], [MovementAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_InventoryMovement_Query2] ON [blood].[InventoryMovement] ([ToFacilityId], [MovementAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_DailyQualityControl_EquipmentId] ON [blood].[DailyQualityControl] ([EquipmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_DailyQualityControl_ShiftId] ON [blood].[DailyQualityControl] ([ShiftId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_DailyQualityControl_RecordedByUserId] ON [blood].[DailyQualityControl] ([RecordedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_DailyQualityControl_ReviewedByUserId] ON [blood].[DailyQualityControl] ([ReviewedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_blood_DailyQualityControl_Query1] ON [blood].[DailyQualityControl] ([FacilityId], [QcDate] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_blood_QualityControlResult_Query1] ON [blood].[QualityControlResult] ([DailyQualityControlId], [ParameterCode]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_Equipment_DepartmentId] ON [equip].[Equipment] ([DepartmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_Equipment_EquipmentCategoryId] ON [equip].[Equipment] ([EquipmentCategoryId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_Equipment_Query1] ON [equip].[Equipment] ([FacilityId], [EquipmentCategoryId], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_MaintenancePlan_EquipmentId] ON [equip].[MaintenancePlan] ([EquipmentId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_MaintenancePlan_Query1] ON [equip].[MaintenancePlan] ([NextDueDate], [IsActive]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_MaintenanceRecord_MaintenancePlanId] ON [equip].[MaintenanceRecord] ([MaintenancePlanId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_MaintenanceRecord_PerformedByUserId] ON [equip].[MaintenanceRecord] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_MaintenanceRecord_Query1] ON [equip].[MaintenanceRecord] ([EquipmentId], [PerformedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureReading_ShiftId] ON [equip].[TemperatureReading] ([ShiftId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureReading_RecorderUserId] ON [equip].[TemperatureReading] ([RecorderUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureReading_Query1] ON [equip].[TemperatureReading] ([EquipmentId], [ObservedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureExcursion_FirstReadingId] ON [equip].[TemperatureExcursion] ([FirstReadingId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureExcursion_AssignedToUserId] ON [equip].[TemperatureExcursion] ([AssignedToUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureExcursion_SupervisorApprovedByUserId] ON [equip].[TemperatureExcursion] ([SupervisorApprovedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureExcursion_Query1] ON [equip].[TemperatureExcursion] ([EquipmentId], [Status], [DetectedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_equip_EquipmentIncident_ReportedByUserId] ON [equip].[EquipmentIncident] ([ReportedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_EquipmentIncident_Query1] ON [equip].[EquipmentIncident] ([EquipmentId], [OccurredAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_equip_EquipmentIncident_Query2] ON [equip].[EquipmentIncident] ([Status], [Severity], [OccurredAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_equip_EquipmentIncidentAction_PerformedByUserId] ON [equip].[EquipmentIncidentAction] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_equip_EquipmentIncidentAction_Query1] ON [equip].[EquipmentIncidentAction] ([EquipmentIncidentId], [ActionAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_AdverseEvent_ReportedByUserId] ON [hv].[AdverseEvent] ([ReportedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_AdverseEvent_Query1] ON [hv].[AdverseEvent] ([TransfusionId], [DetectedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_AdverseEvent_Query2] ON [hv].[AdverseEvent] ([Status], [ReportedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_PhysicianEventAssessment_AssessedByUserId] ON [hv].[PhysicianEventAssessment] ([AssessedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_PhysicianEventAssessment_Query1] ON [hv].[PhysicianEventAssessment] ([AdverseEventId], [AssessedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_EventInvestigation_PerformedByUserId] ON [hv].[EventInvestigation] ([PerformedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_EventInvestigation_Query1] ON [hv].[EventInvestigation] ([AdverseEventId], [InvestigationAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_RcaCase_AdverseEventId] ON [hv].[RcaCase] ([AdverseEventId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_RcaCase_EquipmentIncidentId] ON [hv].[RcaCase] ([EquipmentIncidentId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_RcaCase_OpenedByUserId] ON [hv].[RcaCase] ([OpenedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_RcaCase_Query1] ON [hv].[RcaCase] ([Status], [OpenedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_RcaCause_RcaCaseId] ON [hv].[RcaCause] ([RcaCaseId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CorrectiveAction_RcaCaseId] ON [hv].[CorrectiveAction] ([RcaCaseId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CorrectiveAction_EquipmentIncidentId] ON [hv].[CorrectiveAction] ([EquipmentIncidentId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CorrectiveAction_AuditId] ON [hv].[CorrectiveAction] ([AuditId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CorrectiveAction_VerifiedByUserId] ON [hv].[CorrectiveAction] ([VerifiedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CorrectiveAction_Query1] ON [hv].[CorrectiveAction] ([AssignedToUserId], [Status], [DueAt]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CommitteeMeeting_ChairUserId] ON [hv].[CommitteeMeeting] ([ChairUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CommitteeMeeting_SecretaryUserId] ON [hv].[CommitteeMeeting] ([SecretaryUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_CommitteeMeeting_Query1] ON [hv].[CommitteeMeeting] ([FacilityId], [HeldAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_MeetingAttendee_CommitteeMeetingId] ON [hv].[MeetingAttendee] ([CommitteeMeetingId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_MeetingAttendee_AccountUserId] ON [hv].[MeetingAttendee] ([AccountUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_MeetingDecision_AssignedToUserId] ON [hv].[MeetingDecision] ([AssignedToUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_ClinicalAudit_AuditorUserId] ON [hv].[ClinicalAudit] ([AuditorUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_ClinicalAudit_Query1] ON [hv].[ClinicalAudit] ([DepartmentId], [AuditedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_AuditAnswer_AuditQuestionId] ON [hv].[AuditAnswer] ([AuditQuestionId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_TrainingSession_TrainingCourseId] ON [hv].[TrainingSession] ([TrainingCourseId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_TrainingSession_InstructorUserId] ON [hv].[TrainingSession] ([InstructorUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_TrainingSession_Query1] ON [hv].[TrainingSession] ([StartsAt], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_TrainingEnrollment_AccountUserId] ON [hv].[TrainingEnrollment] ([AccountUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_TrainingExamResult_Query1] ON [hv].[TrainingExamResult] ([TrainingEnrollmentId], [ExamTakenAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_hv_DocumentLibrary_UploadedByUserId] ON [hv].[DocumentLibrary] ([UploadedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_hv_DocumentLibrary_Query1] ON [hv].[DocumentLibrary] ([FacilityId], [DocumentType], [UploadedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_integration_Message_PatientId] ON [integration].[Message] ([PatientId]);
GO

CREATE NONCLUSTERED INDEX [IX_integration_Message_EncounterId] ON [integration].[Message] ([EncounterId]);
GO

CREATE NONCLUSTERED INDEX [IX_integration_Message_Query1] ON [integration].[Message] ([EndpointId], [Status], [CreatedAtMessage]);
GO

CREATE NONCLUSTERED INDEX [IX_integration_SyncJob_TriggeredByUserId] ON [integration].[SyncJob] ([TriggeredByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_integration_SyncJob_Query1] ON [integration].[SyncJob] ([EndpointId], [StartedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_report_ReportRun_ReportDefinitionId] ON [report].[ReportRun] ([ReportDefinitionId]);
GO

CREATE NONCLUSTERED INDEX [IX_report_ReportRun_RequestedByUserId] ON [report].[ReportRun] ([RequestedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_report_ReportRun_Query1] ON [report].[ReportRun] ([FacilityId], [RequestedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_report_ReportSubmission_SubmittedByUserId] ON [report].[ReportSubmission] ([SubmittedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_report_ReportSubmission_Query1] ON [report].[ReportSubmission] ([ReportRunId], [SubmittedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_cfg_Setting_FacilityId] ON [cfg].[Setting] ([FacilityId]);
GO

CREATE NONCLUSTERED INDEX [IX_cfg_Setting_LastModifiedByUserId] ON [cfg].[Setting] ([LastModifiedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_cfg_Setting_Query1] ON [cfg].[Setting] ([SettingKey], [FacilityId]);
GO

CREATE NONCLUSTERED INDEX [IX_cfg_AlertThreshold_ProductTypeId] ON [cfg].[AlertThreshold] ([ProductTypeId]);
GO

CREATE NONCLUSTERED INDEX [IX_cfg_AlertThreshold_EquipmentCategoryId] ON [cfg].[AlertThreshold] ([EquipmentCategoryId]);
GO

CREATE NONCLUSTERED INDEX [IX_cfg_AlertThreshold_Query1] ON [cfg].[AlertThreshold] ([FacilityId], [RuleCode], [IsActive]);
GO

CREATE NONCLUSTERED INDEX [IX_audit_AuditEvent_AccountUserId] ON [audit].[AuditEvent] ([AccountUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_audit_AuditEvent_Query1] ON [audit].[AuditEvent] ([EntityType], [EntityId], [OccurredAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_audit_AuditEvent_Query2] ON [audit].[AuditEvent] ([FacilityId], [OccurredAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Request_OpenQueue] ON [clinical].[BloodRequest] ([Priority], [RequestedAt]) INCLUDE ([EncounterId], [RequestNo]) WHERE [Status] IN ('Submitted','AwaitingDoctor','AwaitingBank','Approved','Crossmatch');
GO

CREATE NONCLUSTERED INDEX [IX_blood_Units_AvailableFEFO] ON [blood].[BloodUnit] ([CurrentFacilityId], [ProductTypeId], [BloodGroupId], [ExpiryDate]) INCLUDE ([UnitCode]) WHERE [Status]='Available' AND [IsQuarantined]=0;
GO

CREATE NONCLUSTERED INDEX [IX_equip_TemperatureExcursion_Open] ON [equip].[TemperatureExcursion] ([DetectedAt] DESC, [EquipmentId]) WHERE [Status] IN ('Open','InProgress');
GO

CREATE NONCLUSTERED INDEX [IX_clinical_Task_Open] ON [clinical].[ClinicalTask] ([AssignedRoleId], [DueAt]) INCLUDE ([FacilityId], [AssignedUserId]) WHERE [Status] IN ('Open','InProgress');
GO

-- Reference data; safe only for a fresh database.
INSERT INTO [sec].[Role] ([Code],[Name]) VALUES
 ('NURSE',N'پرستار'),('DOCTOR',N'پزشک'),('BLOOD_TRANSFUSION_EXPERT',N'کارشناس بانک خون'),
 ('ADMISSION',N'پذیرش'),('BIOMED_ENGINEER',N'مهندس تجهیزات'),
 ('HV_MANAGER',N'مسئول هموویژیلانس'),('SUPERVISOR',N'سوپروایزر'),('SYSTEM_ADMIN',N'مدیر سامانه');
INSERT INTO [blood].[BloodGroup] ([Code],[Abo],[RhD]) VALUES
 ('A+','A','+'),('A-','A','-'),('B+','B','+'),('B-','B','-'),
 ('AB+','AB','+'),('AB-','AB','-'),('O+','O','+'),('O-','O','-');
INSERT INTO [blood].[ProductType] ([Code],[Name]) VALUES
 ('RBC',N'گلبول قرمز متراکم'),('WASHED_RBC',N'گلبول قرمز شسته'),
 ('IRRADIATED_RBC',N'گلبول قرمز تابش‌یافته'),('LEUKOREDUCED_RBC',N'گلبول قرمز لکوسیت‌زدایی‌شده'),
 ('FFP',N'پلاسمای تازه منجمد'),('PLATELET',N'پلاکت'),
 ('APHERESIS_PLATELET',N'پلاکت آفرزیس'),('CRYO',N'کرایوپرسیپیتات'),('WHOLE_BLOOD',N'خون کامل');
GO
-- Permissions, threshold values, hospital-specific statuses and clinical SOPs should be approved before seeding.
-- Data-integrity invariants crossing tables (same patient/facility/valid approval/issue/stock) MUST be
-- enforced transactionally in the API/domain service (or dedicated stored procedures); FKs alone are insufficient.


INSERT INTO [sec].[Permission] ([Code], [Description]) VALUES
 ('patient.read',N'patient.read'),
 ('patient.create',N'patient.create'),
 ('patient.update',N'patient.update'),
 ('patient.identity.verify',N'patient.identity.verify'),
 ('patient.history.read',N'patient.history.read'),
 ('patient.discharge.review',N'patient.discharge.review'),
 ('request.read',N'request.read'),
 ('request.create',N'request.create'),
 ('request.approve',N'request.approve'),
 ('request.cancel',N'request.cancel'),
 ('crossmatch.read',N'crossmatch.read'),
 ('crossmatch.perform',N'crossmatch.perform'),
 ('crossmatch.approve',N'crossmatch.approve'),
 ('inventory.read',N'inventory.read'),
 ('inventory.receive',N'inventory.receive'),
 ('inventory.reserve',N'inventory.reserve'),
 ('inventory.quarantine',N'inventory.quarantine'),
 ('inventory.waste',N'inventory.waste'),
 ('issue.create',N'issue.create'),
 ('issue.receive',N'issue.receive'),
 ('dispatch.manage',N'dispatch.manage'),
 ('return.manage',N'return.manage'),
 ('thaw.manage',N'thaw.manage'),
 ('transfusion.read',N'transfusion.read'),
 ('transfusion.start',N'transfusion.start'),
 ('transfusion.monitor',N'transfusion.monitor'),
 ('transfusion.complete',N'transfusion.complete'),
 ('reaction.report',N'reaction.report'),
 ('reaction.diagnose',N'reaction.diagnose'),
 ('reaction.review',N'reaction.review'),
 ('rca.manage',N'rca.manage'),
 ('audit.manage',N'audit.manage'),
 ('committee.manage',N'committee.manage'),
 ('training.manage',N'training.manage'),
 ('equipment.read',N'equipment.read'),
 ('equipment.manage',N'equipment.manage'),
 ('equipment.temperature.record',N'equipment.temperature.record'),
 ('equipment.temperature.approve',N'equipment.temperature.approve'),
 ('report.view',N'report.view'),
 ('report.submit',N'report.submit'),
 ('his.sync',N'his.sync'),
 ('settings.manage',N'settings.manage'),
 ('users.manage',N'users.manage'),
 ('qc.manage',N'qc.manage'),
 ('stockorder.manage',N'stockorder.manage'),
 ('transfer.manage',N'transfer.manage'),
 ('clinicalnote.create',N'clinicalnote.create');
GO

-- Starting RBAC role-permission matrix; review against hospital policy.
INSERT INTO [sec].[RolePermission] ([RoleId],[PermissionId])
SELECT r.[RoleId],p.[PermissionId]
FROM (VALUES
  ('ADMISSION','patient.read'),
  ('ADMISSION','patient.create'),
  ('ADMISSION','patient.update'),
  ('ADMISSION','patient.identity.verify'),
  ('ADMISSION','patient.history.read'),
  ('ADMISSION','patient.discharge.review'),
  ('ADMISSION','report.view'),
  ('ADMISSION','his.sync'),
  ('NURSE','patient.read'),
  ('NURSE','patient.history.read'),
  ('NURSE','request.read'),
  ('NURSE','request.create'),
  ('NURSE','transfusion.read'),
  ('NURSE','transfusion.start'),
  ('NURSE','transfusion.monitor'),
  ('NURSE','transfusion.complete'),
  ('NURSE','reaction.report'),
  ('NURSE','issue.receive'),
  ('NURSE','clinicalnote.create'),
  ('NURSE','report.view'),
  ('DOCTOR','patient.read'),
  ('DOCTOR','patient.history.read'),
  ('DOCTOR','request.read'),
  ('DOCTOR','request.create'),
  ('DOCTOR','request.approve'),
  ('DOCTOR','request.cancel'),
  ('DOCTOR','transfusion.read'),
  ('DOCTOR','transfusion.monitor'),
  ('DOCTOR','reaction.diagnose'),
  ('DOCTOR','reaction.review'),
  ('DOCTOR','clinicalnote.create'),
  ('DOCTOR','report.view'),
  ('DOCTOR','report.submit'),
  ('BLOOD_TRANSFUSION_EXPERT','patient.read'),
  ('BLOOD_TRANSFUSION_EXPERT','request.read'),
  ('BLOOD_TRANSFUSION_EXPERT','crossmatch.read'),
  ('BLOOD_TRANSFUSION_EXPERT','crossmatch.perform'),
  ('BLOOD_TRANSFUSION_EXPERT','inventory.read'),
  ('BLOOD_TRANSFUSION_EXPERT','inventory.receive'),
  ('BLOOD_TRANSFUSION_EXPERT','inventory.reserve'),
  ('BLOOD_TRANSFUSION_EXPERT','issue.create'),
  ('BLOOD_TRANSFUSION_EXPERT','thaw.manage'),
  ('BLOOD_TRANSFUSION_EXPERT','equipment.read'),
  ('BLOOD_TRANSFUSION_EXPERT','equipment.temperature.record'),
  ('BLOOD_TRANSFUSION_EXPERT','qc.manage'),
  ('BLOOD_TRANSFUSION_EXPERT','report.view'),
  ('SUPERVISOR','patient.read'),
  ('SUPERVISOR','request.read'),
  ('SUPERVISOR','request.approve'),
  ('SUPERVISOR','crossmatch.read'),
  ('SUPERVISOR','crossmatch.perform'),
  ('SUPERVISOR','crossmatch.approve'),
  ('SUPERVISOR','inventory.read'),
  ('SUPERVISOR','inventory.receive'),
  ('SUPERVISOR','inventory.reserve'),
  ('SUPERVISOR','inventory.quarantine'),
  ('SUPERVISOR','inventory.waste'),
  ('SUPERVISOR','issue.create'),
  ('SUPERVISOR','dispatch.manage'),
  ('SUPERVISOR','return.manage'),
  ('SUPERVISOR','thaw.manage'),
  ('SUPERVISOR','equipment.read'),
  ('SUPERVISOR','equipment.temperature.record'),
  ('SUPERVISOR','equipment.temperature.approve'),
  ('SUPERVISOR','qc.manage'),
  ('SUPERVISOR','stockorder.manage'),
  ('SUPERVISOR','transfer.manage'),
  ('SUPERVISOR','committee.manage'),
  ('SUPERVISOR','report.view'),
  ('SUPERVISOR','report.submit'),
  ('BIOMED_ENGINEER','equipment.read'),
  ('BIOMED_ENGINEER','equipment.manage'),
  ('BIOMED_ENGINEER','equipment.temperature.record'),
  ('BIOMED_ENGINEER','equipment.temperature.approve'),
  ('BIOMED_ENGINEER','report.view'),
  ('HV_MANAGER','patient.read'),
  ('HV_MANAGER','patient.history.read'),
  ('HV_MANAGER','request.read'),
  ('HV_MANAGER','transfusion.read'),
  ('HV_MANAGER','reaction.review'),
  ('HV_MANAGER','rca.manage'),
  ('HV_MANAGER','audit.manage'),
  ('HV_MANAGER','committee.manage'),
  ('HV_MANAGER','training.manage'),
  ('HV_MANAGER','equipment.read'),
  ('HV_MANAGER','report.view'),
  ('HV_MANAGER','report.submit'),
  ('HV_MANAGER','settings.manage'),
  ('HV_MANAGER','qc.manage')
) AS x([RoleCode],[PermissionCode])
INNER JOIN [sec].[Role] r ON r.[Code]=x.[RoleCode]
INNER JOIN [sec].[Permission] p ON p.[Code]=x.[PermissionCode];
INSERT INTO [sec].[RolePermission] ([RoleId],[PermissionId])
SELECT r.[RoleId],p.[PermissionId] FROM [sec].[Role] r CROSS JOIN [sec].[Permission] p WHERE r.[Code]='SYSTEM_ADMIN';
-- Fresh-only schema version marker; upgrades must use reviewed migrations.
INSERT INTO [cfg].[Setting] ([FacilityId],[SettingKey],[SettingValue],[ValueType])
VALUES (NULL, 'schema.version', N'1.0.0', 'String');
GO
-- Post-install object verification (expected: 88 tables, 203 trusted FKs, 259 nonclustered indexes)
SELECT
    (SELECT COUNT(*) FROM sys.tables AS t WHERE SCHEMA_NAME(t.schema_id) IN
        ('org','sec','patient','clinical','blood','equip','hv','integration','report','cfg','audit')) AS HemoTableCount,
    (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_ms_shipped = 0) AS ForeignKeyCount,
    (SELECT COUNT(*) FROM sys.indexes AS i INNER JOIN sys.tables AS t ON i.object_id=t.object_id
        WHERE i.type=2 AND t.is_ms_shipped=0) AS NonclusteredIndexCount;
SELECT [name], [is_disabled], [is_not_trusted]
FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1;
GO
