CREATE TABLE [dbo].[Users]
(
	[UserID] int,
	[Name] varchar(100)DEFAULT'TBA',
	[PhoneNo] varchar(100),
	[AddressLine1] varchar(100),
	[AddressLine2] varchar(100),
	[Suburb] varchar(100),
	[Postcode] varchar(100),
	CONSTRAINT [UserID] PRIMARY KEY(UserID),
)
