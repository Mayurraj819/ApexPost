SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
SET LOCK_TIMEOUT 10000;
SELECT * FROM dbo.BranchStock WHERE Id=(SELECT TOP(1)Id FROM dbo.BranchStock ORDER BY Id);
-- RCSI OFF: may block/time out while A holds X lock. RCSI ON: versioned read can proceed.
-- Record your actual isolation/database setting and timing; do not assume an outcome.

-- Under RCSI ON, request locking READ COMMITTED to demonstrate a blocked reader:
SELECT * FROM dbo.BranchStock WITH(READCOMMITTEDLOCK) WHERE Id=(SELECT TOP(1)Id FROM dbo.BranchStock ORDER BY Id);
