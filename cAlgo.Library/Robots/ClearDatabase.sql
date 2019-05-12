delete from dbo.position
go
delete from dbo.run
go
DBCC CHECKIDENT ('position', RESEED, 1)
go
DBCC CHECKIDENT ('run', RESEED, 1)
go
