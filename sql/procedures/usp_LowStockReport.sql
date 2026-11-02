CREATE OR ALTER PROCEDURE dbo.usp_LowStockReport
    @BranchCode varchar(40) = NULL,
    @Threshold int = 3
AS
BEGIN
    SET NOCOUNT ON;
    IF @Threshold IS NULL OR @Threshold < 0
        THROW 50003, 'Threshold must be nonnegative.', 1;
    IF @BranchCode IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Code = @BranchCode)
        THROW 50002, 'Branch was not found.', 1;

    -- A branch-specific call and an all-branch call have different selectivity.
    -- RECOMPILE specializes these predicates at the cost of compilation per call.
    -- Alternatives: cached optional predicates, separate query variants, or PSP where eligible.
    -- Re-measure before changing this policy; ADR 0009 records the tradeoff.
    SELECT s.BranchId, s.ProductId, p.Name AS Product, s.Quantity
    FROM dbo.BranchStock AS s
    INNER JOIN dbo.Branches AS b ON b.Id = s.BranchId
    INNER JOIN dbo.Products AS p ON p.Id = s.ProductId
    WHERE (@BranchCode IS NULL OR b.Code = @BranchCode) AND s.Quantity <= @Threshold
    ORDER BY s.Quantity, s.BranchId, s.ProductId
    OPTION (RECOMPILE);
END;
