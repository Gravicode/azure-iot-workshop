USE [azure-iot-workshop-database]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Barometer_sensor] (
    [SensorType]     NVARCHAR (25) NOT NULL,
    [DeviceId]       NVARCHAR (25) NOT NULL,
    [AirPressure]    FLOAT (53)    NULL,
    [AirTemperature] FLOAT (53)    NULL,
    [TimeStamp]      DATETIME      NULL
);

CREATE TABLE [dbo].[Calories_sensor] (
    [SensorType]          NVARCHAR (25) NOT NULL,
    [DeviceId]            NVARCHAR (25) NOT NULL,
    [CaloriesBurnedTotal] INT           NULL,
    [CaloriesBurnedToday] INT           NULL,
    [TimeStamp]           DATETIME      NULL
);

CREATE TABLE [dbo].[Contact_sensor] (
    [SensorType] NVARCHAR (25) NOT NULL,
    [DeviceId]   NVARCHAR (25) NOT NULL,
    [State]      INT           NULL,
    [TimeStamp]  DATETIME      NULL
);

CREATE TABLE [dbo].[Distance_sensor] (
    [SensorType]    NVARCHAR (25) NOT NULL,
    [DeviceId]      NVARCHAR (25) NOT NULL,
    [DistanceTotal] INT           NULL,
    [DistanceToday] INT           NULL,
    [Speed]         FLOAT (53)    NULL,
    [Pace]          FLOAT (53)    NULL,
    [CurrentMotion] INT           NULL,
    [TimeStamp]     DATETIME      NULL
);

CREATE TABLE [dbo].[Heartbeat_sensor] (
    [SensorType]              NVARCHAR (25) NOT NULL,
    [DeviceId]                NVARCHAR (25) NOT NULL,
    [CurrentHeartRate]        INT           NULL,
    [CurrentHeartRateQuality] INT           NULL,
    [TimeStamp]               DATETIME      NULL
);

CREATE TABLE [dbo].[Light_sensor] (
    [SensorType]   NVARCHAR (25) NOT NULL,
    [DeviceId]     NVARCHAR (25) NOT NULL,
    [AmbientLight] FLOAT (53)    NULL,
    [TimeStamp]    DATETIME      NULL
);

CREATE TABLE [dbo].[Location_sensor] (
    [SensorType] NVARCHAR (25) NOT NULL,
    [DeviceId]   NVARCHAR (25) NOT NULL,
    [Longitude]  FLOAT (53)    NULL,
    [Latitude]   FLOAT (53)    NULL,
    [TimeStamp]  DATETIME      NULL
);

CREATE TABLE [dbo].[Pedometer_sensor] (
    [SensorType] NVARCHAR (25) NOT NULL,
    [DeviceId]   NVARCHAR (25) NOT NULL,
    [StepsTotal] BIGINT        NULL,
    [StepsToday] INT           NULL,
    [TimeStamp]  DATETIME      NULL
);

CREATE TABLE [dbo].[SkinTemperature_sensor] (
    [SensorType]      NVARCHAR (25) NOT NULL,
    [DeviceId]        NVARCHAR (25) NOT NULL,
    [SkinTemperature] FLOAT (53)    NULL,
    [TimeStamp]       DATETIME      NULL
);

CREATE TABLE [dbo].[UV_sensor] (
    [SensorType]    NVARCHAR (25) NOT NULL,
    [DeviceId]      NVARCHAR (25) NOT NULL,
    [Level]         INT           NULL,
    [ExposureToday] FLOAT (53)    NULL,
    [TimeStamp]     DATETIME      NULL
);

























