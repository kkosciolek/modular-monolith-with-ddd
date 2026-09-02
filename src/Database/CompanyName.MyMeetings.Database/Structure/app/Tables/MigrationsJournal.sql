CREATE TABLE [app].[MigrationsHistory](
	[Id] [int] IDENTITY(1, 1) NOT NULL,
	[ScriptName] NVARCHAR(255) NOT NULL,
	[Applied] DATETIME NOT NULL,
 CONSTRAINT [PK_app_MigrationsHistory_Id] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))