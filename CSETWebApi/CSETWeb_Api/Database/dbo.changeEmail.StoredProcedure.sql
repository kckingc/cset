USE [CSETWeb]
GO
/****** Object:  StoredProcedure [dbo].[changeEmail]    Script Date: 11/14/2018 3:57:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[changeEmail]
	@originalEmail varchar(200),
	@newEmail varchar(200)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	if not exists (select * from users where PrimaryEmail = @newEmail)
		update USERS set PrimaryEmail = @newEmail where PrimaryEmail = @originalEmail
	--if we can't update then we can't update
END
GO
