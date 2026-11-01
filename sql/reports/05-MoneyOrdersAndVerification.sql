SELECT b.Code,COUNT(m.Id) MoneyOrders,COALESCE(SUM(m.OrderAmount),0) Amount,
 SUM(CASE WHEN v.Id IS NOT NULL THEN 1 ELSE 0 END) DocumentsRecorded,
 SUM(CASE WHEN v.IsVerified=1 THEN 1 ELSE 0 END) IndependentlyVerified
FROM dbo.Branches b LEFT JOIN dbo.MoneyOrders m ON m.BranchId=b.Id
LEFT JOIN dbo.IdentityVerifications v ON v.Id=m.IdentityVerificationId
GROUP BY b.Code;
