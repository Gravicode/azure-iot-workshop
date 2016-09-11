USE [azure-iot-workshop-database]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[simulator-data] (
[DeviceID]	NVARCHAR(32) NULL,
[TimeStamp]	DATETIME NULL,
[Temperature] FLOAT NULL,
[Humidity] FLOAT NULL,
[WindSpeed] FLOAT NULL,
[Pressure] FLOAT NULL,
[Latitude] FLOAT NULL,
[Longitude] FLOAT NULL,
[EventProcessedUtcTime] DATETIME NULL,
[EventEnqueuedUtcTime] DATETIME NULL
)