BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_JOB_DETAILS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_JOB_DETAILS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [JOB_NAME] [nvarchar](150) NOT NULL,
                            [JOB_GROUP] [nvarchar](150) NOT NULL,
                            [DESCRIPTION] [nvarchar](250) NULL,
                            [JOB_CLASS_NAME] [nvarchar](250) NOT NULL,
                            [IS_DURABLE] [bit] NOT NULL,
                            [IS_NONCONCURRENT] [bit] NOT NULL,
                            [IS_UPDATE_DATA] [bit] NOT NULL,
                            [REQUESTS_RECOVERY] [bit] NOT NULL,
                            [JOB_DATA] [varbinary](max) NULL,
                            CONSTRAINT [PK_QRTZ_JOB_DETAILS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [JOB_NAME] ASC,
                                [JOB_GROUP] ASC
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_TRIGGERS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_TRIGGERS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [TRIGGER_NAME] [nvarchar](150) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            [JOB_NAME] [nvarchar](150) NOT NULL,
                            [JOB_GROUP] [nvarchar](150) NOT NULL,
                            [DESCRIPTION] [nvarchar](250) NULL,
                            [NEXT_FIRE_TIME] [bigint] NULL,
                            [PREV_FIRE_TIME] [bigint] NULL,
                            [PRIORITY] [int] NULL,
                            [TRIGGER_STATE] [nvarchar](16) NOT NULL,
                            [TRIGGER_TYPE] [nvarchar](8) NOT NULL,
                            [START_TIME] [bigint] NOT NULL,
                            [END_TIME] [bigint] NULL,
                            [CALENDAR_NAME] [nvarchar](150) NULL,
                            [MISFIRE_INSTR] [int] NULL,
                            [JOB_DATA] [varbinary](max) NULL,
                            CONSTRAINT [PK_QRTZ_TRIGGERS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [TRIGGER_NAME] ASC,
                                [TRIGGER_GROUP] ASC
                            ),
                            CONSTRAINT [FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS] FOREIGN KEY
                            (
                                [SCHED_NAME],
                                [JOB_NAME],
                                [JOB_GROUP]
                            ) REFERENCES [dbo].[QRTZ_JOB_DETAILS] (
                                [SCHED_NAME],
                                [JOB_NAME],
                                [JOB_GROUP]
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_SIMPLE_TRIGGERS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [TRIGGER_NAME] [nvarchar](150) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            [REPEAT_COUNT] [int] NOT NULL,
                            [REPEAT_INTERVAL] [bigint] NOT NULL,
                            [TIMES_TRIGGERED] [int] NOT NULL,
                            CONSTRAINT [PK_QRTZ_SIMPLE_TRIGGERS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [TRIGGER_NAME] ASC,
                                [TRIGGER_GROUP] ASC
                            ),
                            CONSTRAINT [FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY
                            (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            ) REFERENCES [dbo].[QRTZ_TRIGGERS] (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_CRON_TRIGGERS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_CRON_TRIGGERS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [TRIGGER_NAME] [nvarchar](150) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            [CRON_EXPRESSION] [nvarchar](120) NOT NULL,
                            [TIME_ZONE_ID] [nvarchar](80) NULL,
                            CONSTRAINT [PK_QRTZ_CRON_TRIGGERS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [TRIGGER_NAME] ASC,
                                [TRIGGER_GROUP] ASC
                            ),
                            CONSTRAINT [FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY
                            (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            ) REFERENCES [dbo].[QRTZ_TRIGGERS] (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_SIMPROP_TRIGGERS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [TRIGGER_NAME] [nvarchar](150) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            [STR_PROP_1] [nvarchar](512) NULL,
                            [STR_PROP_2] [nvarchar](512) NULL,
                            [STR_PROP_3] [nvarchar](512) NULL,
                            [INT_PROP_1] [int] NULL,
                            [INT_PROP_2] [int] NULL,
                            [LONG_PROP_1] [bigint] NULL,
                            [LONG_PROP_2] [bigint] NULL,
                            [DEC_PROP_1] [decimal](18, 2) NULL,
                            [DEC_PROP_2] [decimal](18, 2) NULL,
                            [BOOL_PROP_1] [bit] NULL,
                            [BOOL_PROP_2] [bit] NULL,
                            CONSTRAINT [PK_QRTZ_SIMPROP_TRIGGERS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [TRIGGER_NAME] ASC,
                                [TRIGGER_GROUP] ASC
                            ),
                            CONSTRAINT [FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY
                            (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            ) REFERENCES [dbo].[QRTZ_TRIGGERS] (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_BLOB_TRIGGERS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_BLOB_TRIGGERS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [TRIGGER_NAME] [nvarchar](150) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            [BLOB_DATA] [varbinary](max) NULL,
                            CONSTRAINT [PK_QRTZ_BLOB_TRIGGERS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [TRIGGER_NAME] ASC,
                                [TRIGGER_GROUP] ASC
                            ),
                            CONSTRAINT [FK_QRTZ_BLOB_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY
                            (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            ) REFERENCES [dbo].[QRTZ_TRIGGERS] (
                                [SCHED_NAME],
                                [TRIGGER_NAME],
                                [TRIGGER_GROUP]
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_CALENDARS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_CALENDARS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [CALENDAR_NAME] [nvarchar](150) NOT NULL,
                            [CALENDAR] [varbinary](max) NOT NULL,
                            CONSTRAINT [PK_QRTZ_CALENDARS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [CALENDAR_NAME] ASC
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_PAUSED_TRIGGER_GRPS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_PAUSED_TRIGGER_GRPS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            CONSTRAINT [PK_QRTZ_PAUSED_TRIGGER_GRPS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [TRIGGER_GROUP] ASC
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_FIRED_TRIGGERS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_FIRED_TRIGGERS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [ENTRY_ID] [nvarchar](140) NOT NULL,
                            [TRIGGER_NAME] [nvarchar](150) NOT NULL,
                            [TRIGGER_GROUP] [nvarchar](150) NOT NULL,
                            [INSTANCE_NAME] [nvarchar](200) NOT NULL,
                            [FIRED_TIME] [bigint] NOT NULL,
                            [SCHED_TIME] [bigint] NOT NULL,
                            [PRIORITY] [int] NOT NULL,
                            [STATE] [nvarchar](16) NOT NULL,
                            [JOB_NAME] [nvarchar](150) NULL,
                            [JOB_GROUP] [nvarchar](150) NULL,
                            [IS_NONCONCURRENT] [bit] NULL,
                            [REQUESTS_RECOVERY] [bit] NULL,
                            CONSTRAINT [PK_QRTZ_FIRED_TRIGGERS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [ENTRY_ID] ASC
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_SCHEDULER_STATE]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_SCHEDULER_STATE](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [INSTANCE_NAME] [nvarchar](200) NOT NULL,
                            [LAST_CHECKIN_TIME] [bigint] NOT NULL,
                            [CHECKIN_INTERVAL] [bigint] NOT NULL,
                            CONSTRAINT [PK_QRTZ_SCHEDULER_STATE] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [INSTANCE_NAME] ASC
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QRTZ_LOCKS]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[QRTZ_LOCKS](
                            [SCHED_NAME] [nvarchar](120) NOT NULL,
                            [LOCK_NAME] [nvarchar](40) NOT NULL,
                            CONSTRAINT [PK_QRTZ_LOCKS] PRIMARY KEY CLUSTERED 
                            (
                                [SCHED_NAME] ASC,
                                [LOCK_NAME] ASC
                            )
                        )
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_J_REQ_RECOVERY' AND object_id = OBJECT_ID('QRTZ_JOB_DETAILS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_J_REQ_RECOVERY] ON [dbo].[QRTZ_JOB_DETAILS] ([SCHED_NAME], [REQUESTS_RECOVERY])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_J_GRP' AND object_id = OBJECT_ID('QRTZ_JOB_DETAILS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_J_GRP] ON [dbo].[QRTZ_JOB_DETAILS] ([SCHED_NAME], [JOB_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_J' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_J] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_JG' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_JG] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [JOB_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_C' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_C] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [CALENDAR_NAME])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_G' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_G] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_STATE' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_STATE] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_STATE])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_N_STATE' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_N_STATE] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP], [TRIGGER_STATE])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_N_G_STATE' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_N_G_STATE] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_GROUP], [TRIGGER_STATE])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NEXT_FIRE_TIME' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_NEXT_FIRE_TIME] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [NEXT_FIRE_TIME])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_ST' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_NFT_ST] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_STATE], [NEXT_FIRE_TIME])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_MISFIRE' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_NFT_MISFIRE] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [MISFIRE_INSTR], [NEXT_FIRE_TIME])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_ST_MISFIRE' AND object_id = OBJECT_ID('QRTZ_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_T_NFT_ST_MISFIRE] ON [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [MISFIRE_INSTR], [NEXT_FIRE_TIME], [TRIGGER_STATE])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_TRIG_INST_NAME' AND object_id = OBJECT_ID('QRTZ_FIRED_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_FT_TRIG_INST_NAME] ON [dbo].[QRTZ_FIRED_TRIGGERS] ([SCHED_NAME], [INSTANCE_NAME])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_INST_JOB_REQ_RCVRY' AND object_id = OBJECT_ID('QRTZ_FIRED_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_FT_INST_JOB_REQ_RCVRY] ON [dbo].[QRTZ_FIRED_TRIGGERS] ([SCHED_NAME], [INSTANCE_NAME], [REQUESTS_RECOVERY])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_J_G' AND object_id = OBJECT_ID('QRTZ_FIRED_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_FT_J_G] ON [dbo].[QRTZ_FIRED_TRIGGERS] ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_JG' AND object_id = OBJECT_ID('QRTZ_FIRED_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_FT_JG] ON [dbo].[QRTZ_FIRED_TRIGGERS] ([SCHED_NAME], [JOB_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_T_G' AND object_id = OBJECT_ID('QRTZ_FIRED_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_FT_T_G] ON [dbo].[QRTZ_FIRED_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_TG' AND object_id = OBJECT_ID('QRTZ_FIRED_TRIGGERS'))
                    BEGIN
                        CREATE INDEX [IDX_QRTZ_FT_TG] ON [dbo].[QRTZ_FIRED_TRIGGERS] ([SCHED_NAME], [TRIGGER_GROUP])
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226150000_AddQuartzTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251226150000_AddQuartzTables', N'8.0.22');
END;
GO

COMMIT;
GO

