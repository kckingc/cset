USE [CSETWeb]
GO
/****** Object:  StoredProcedure [dbo].[usp_GetRankedCategoriesPage]    Script Date: 11/14/2018 3:57:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		hansbk
-- Create date: 7/30/2018
-- Description:	<Description,,>
-- =============================================
Create PROCEDURE [dbo].[usp_GetRankedCategoriesPage]
	@assessment_id int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    EXECUTE [dbo].[FillEmptyQuestionsForAnalysis]  @Assessment_Id	
	execute [dbo].[usp_getRankedCategories] @assessment_id

END
GO
