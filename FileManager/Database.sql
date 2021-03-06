USE [FileManager]
GO
/****** Object:  Table [dbo].[File]    Script Date: 7/4/2013 8:51:59 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[File](
	[FileId] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](8000) NULL,
	[ParentFolderId] [bigint] NULL,
	[IsDeleted] [bit] NOT NULL,
	[IsPurged] [bit] NOT NULL,
	[CreatedTimeStamp] [datetime] NOT NULL,
	[LastModifiedTimestamp] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FileVersion]    Script Date: 7/4/2013 8:51:59 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FileVersion](
	[VersionId] [bigint] IDENTITY(1,1) NOT NULL,
	[FileId] [bigint] NOT NULL,
	[Version] [int] NOT NULL,
	[IsCurrent] [bit] NOT NULL,
	[FileStore] [uniqueidentifier] NOT NULL,
	[Size] [bigint] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Folder]    Script Date: 7/4/2013 8:51:59 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Folder](
	[FolderId] [bigint] IDENTITY(1,1) NOT NULL,
	[FullPath] [varchar](8000) NULL,
	[Name] [varchar](8000) NULL,
	[ParentFolderId] [bigint] NULL,
	[IsDeleted] [bit] NOT NULL,
	[IsPurged] [bit] NOT NULL,
	[CreatedTimeStamp] [datetime] NOT NULL,
	[LastModifiedTimestamp] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FolderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
