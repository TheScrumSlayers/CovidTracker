CREATE TABLE [dbo].[Signin]
(
	SigninID int,
   UserID int,
   Time DateTime NOT NULL,
   PhoneNo varchar(100),
   AddressLine1 varchar(100),
   AddressLine2 varchar(100),
   Suburb varchar(100),
   Postcode varchar(100),
   CONSTRAINT SigninID PRIMARY KEY(SigninID),
   CONSTRAINT UserIDFK FOREIGN KEY(UserID)REFERENCES Users(UserID)

)