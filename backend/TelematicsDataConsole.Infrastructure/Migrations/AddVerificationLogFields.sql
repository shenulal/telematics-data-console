-- Migration: Add additional fields to VerificationLogs table
-- Date: 2025-12-10
-- Description: Add IMEI, VerificationStatus, Notes, Latitude, Longitude, GpsTime columns

-- Add IMEI column (with default value for existing records)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[VerificationLogs]') AND name = 'Imei')
BEGIN
    ALTER TABLE [dbo].[VerificationLogs] ADD [Imei] NVARCHAR(20) NULL;
    PRINT 'Added Imei column';
END
GO

-- Add VerificationStatus column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[VerificationLogs]') AND name = 'VerificationStatus')
BEGIN
    ALTER TABLE [dbo].[VerificationLogs] ADD [VerificationStatus] NVARCHAR(50) NULL DEFAULT 'Verified';
    PRINT 'Added VerificationStatus column';
END
GO

-- Add Notes column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[VerificationLogs]') AND name = 'Notes')
BEGIN
    ALTER TABLE [dbo].[VerificationLogs] ADD [Notes] NVARCHAR(1000) NULL;
    PRINT 'Added Notes column';
END
GO

-- Add Latitude column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[VerificationLogs]') AND name = 'Latitude')
BEGIN
    ALTER TABLE [dbo].[VerificationLogs] ADD [Latitude] FLOAT NULL;
    PRINT 'Added Latitude column';
END
GO

-- Add Longitude column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[VerificationLogs]') AND name = 'Longitude')
BEGIN
    ALTER TABLE [dbo].[VerificationLogs] ADD [Longitude] FLOAT NULL;
    PRINT 'Added Longitude column';
END
GO

-- Add GpsTime column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[VerificationLogs]') AND name = 'GpsTime')
BEGIN
    ALTER TABLE [dbo].[VerificationLogs] ADD [GpsTime] DATETIME2 NULL;
    PRINT 'Added GpsTime column';
END
GO

-- Create index on IMEI for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VerificationLogs_Imei' AND object_id = OBJECT_ID(N'[dbo].[VerificationLogs]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VerificationLogs_Imei] ON [dbo].[VerificationLogs] ([Imei]);
    PRINT 'Created index IX_VerificationLogs_Imei';
END
GO

PRINT 'Migration completed successfully';

