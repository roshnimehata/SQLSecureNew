SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[vwfilterrules]'))
drop view [dbo].[vwfilterrules]
GO

CREATE VIEW [dbo].[vwfilterrules] 
AS 
SELECT DISTINCT 
a.filterruleheaderid, 
a.connectionname, 
a.rulename, 
a.description,
a.createdby, 
a.createdtm, 
a.lastmodifiedtm, 
a.lastmodifiedby, 
b.filterruleid, 
b.class, 
b.scope, 
b.matchstring, 
c.objectvalue as classname 
FROM  
[filterruleheader] a, 
[filterrule] b, 
[filterruleclass] c 
WHERE 
a.filterruleheaderid = b.filterruleheaderid 
and b.class = c.objectclass

GO

GRANT SELECT ON [dbo].[vwfilterrules] TO [SQLSecureView]

GO
