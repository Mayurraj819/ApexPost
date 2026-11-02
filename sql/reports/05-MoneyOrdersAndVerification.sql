-- Retain branches without orders. Recording identity is distinct from independent verification.
-- Money orders are PHP-only under the current domain rule.
SELECT b.Code, COUNT(m.Id) AS MoneyOrders, COALESCE(SUM(m.OrderAmount), 0) AS Amount,
    SUM(CASE WHEN v.Id IS NOT NULL THEN 1 ELSE 0 END) AS DocumentsRecorded,
    SUM(CASE WHEN v.IsVerified = 1 THEN 1 ELSE 0 END) AS IndependentlyVerified
FROM dbo.Branches AS b
LEFT JOIN dbo.MoneyOrders AS m ON m.BranchId = b.Id
LEFT JOIN dbo.IdentityVerifications AS v ON v.Id = m.IdentityVerificationId
GROUP BY b.Code;
