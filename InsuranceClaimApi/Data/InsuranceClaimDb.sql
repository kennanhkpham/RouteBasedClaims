IF DB_ID(N'InsuranceClaimDb') IS NULL
    CREATE DATABASE InsuranceClaimDb;
GO

USE InsuranceClaimDb;
GO

--- Tables ---

IF OBJECT_ID(N'dbo.Client', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Client(
        clientId int primary key identity(1,1),
        clientPolicyNum varchar(50) not null,
        clientFullName varchar(100) not null,
        clientEmail varchar(100) not null
    );
END;

IF OBJECT_ID(N'dbo.Claim', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Claim(
        claimId int primary key identity(1,1),
        claimNumber varchar(50) not null,
        policyNumber varchar(50) not null,
        fullName varchar(100) not null,
        email varchar(100) not null,
        amountCost decimal(18,2) not null,
        status varchar(50) not null default 'submitted',
        createdAt datetime default getdate() not null
    );
END;

IF OBJECT_ID(N'dbo.PrescriptionDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrescriptionDetails(
        prescriptionId int primary key identity(1,1),
        prescriptionNumber varchar(50) not null,
        doctorName varchar(100) not null,
        datePrescribed datetime default getdate() not null,
        medicationName varchar(50) not null,
        prescriptionClaimId int not null unique,
        constraint fk_claimId foreign key (prescriptionClaimId) references dbo.Claim(claimId)
    );
END;

--- Sample Data ---

-- 1. Insert Sample Clients
INSERT INTO dbo.Client (clientPolicyNum, clientFullName, clientEmail)
SELECT seed.clientPolicyNum, seed.clientFullName, seed.clientEmail
FROM (VALUES
    ('POL-1001', 'John Doe', 'john.doe@example.com'),
    ('POL-1002', 'Jane Smith', 'jane.smith@example.com'),
    ('POL-1003', 'Robert Johnson', 'r.johnson@example.com'),
    ('POL-1004', 'Emily Davis', 'emily.davis@example.com'),
    ('POL-1005', 'Michael Brown', 'mbrown@example.com'),
    ('POL-1006', 'Sarah Wilson', 'swilson@example.com'),
    ('POL-1007', 'David Miller', 'david.m@example.com'),
    ('POL-1008', 'Jessica Taylor', 'jtaylor@example.com'),
    ('POL-1009', 'James Anderson', 'j.anderson@example.com'),
    ('POL-1010', 'Amanda Thomas', 'athomas@example.com')
) AS seed(clientPolicyNum, clientFullName, clientEmail)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Client AS existing
    WHERE existing.clientPolicyNum = seed.clientPolicyNum
);

-- 2. Insert Sample Claims
INSERT INTO dbo.Claim (claimNumber, policyNumber, fullName, email, amountCost, status, createdAt)
SELECT seed.claimNumber, seed.policyNumber, seed.fullName, seed.email,
       seed.amountCost, seed.status, seed.createdAt
FROM (VALUES
    ('CLM-A1B2C3D1', 'POL-1001', 'John Doe', 'john.doe@example.com', CAST(125.50 AS decimal(18,2)), 'Submitted', DATEADD(day, -10, GETDATE())),
    ('CLM-A1B2C3D2', 'POL-1002', 'Jane Smith', 'jane.smith@example.com', CAST(450.00 AS decimal(18,2)), 'Pending Review', DATEADD(day, -9, GETDATE())),
    ('CLM-A1B2C3D3', 'POL-1003', 'Robert Johnson', 'r.johnson@example.com', CAST(85.20 AS decimal(18,2)), 'Approved', DATEADD(day, -8, GETDATE())),
    ('CLM-A1B2C3D4', 'POL-1004', 'Emily Davis', 'emily.davis@example.com', CAST(310.00 AS decimal(18,2)), 'Rejected', DATEADD(day, -7, GETDATE())),
    ('CLM-A1B2C3D5', 'POL-1005', 'Michael Brown', 'mbrown@example.com', CAST(195.75 AS decimal(18,2)), 'Pending Authorization', DATEADD(day, -6, GETDATE())),
    ('CLM-A1B2C3D6', 'POL-1006', 'Sarah Wilson', 'swilson@example.com', CAST(600.00 AS decimal(18,2)), 'Submitted', DATEADD(day, -5, GETDATE())),
    ('CLM-A1B2C3D7', 'POL-1007', 'David Miller', 'david.m@example.com', CAST(42.10 AS decimal(18,2)), 'Approved', DATEADD(day, -4, GETDATE())),
    ('CLM-A1B2C3D8', 'POL-1008', 'Jessica Taylor', 'jtaylor@example.com', CAST(890.50 AS decimal(18,2)), 'Pending Review', DATEADD(day, -3, GETDATE())),
    ('CLM-A1B2C3D9', 'POL-1009', 'James Anderson', 'j.anderson@example.com', CAST(150.00 AS decimal(18,2)), 'Submitted', DATEADD(day, -2, GETDATE())),
    ('CLM-A1B2C3E0', 'POL-1010', 'Amanda Thomas', 'athomas@example.com', CAST(275.30 AS decimal(18,2)), 'Approved', DATEADD(day, -1, GETDATE()))
) AS seed(claimNumber, policyNumber, fullName, email, amountCost, status, createdAt)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Claim AS existing
    WHERE existing.claimNumber = seed.claimNumber
);

-- 3. Synchronize clients from the latest claim for each policy
;WITH LatestClaimPerPolicy AS (
    SELECT policyNumber, fullName, email,
           ROW_NUMBER() OVER (
               PARTITION BY policyNumber
               ORDER BY createdAt DESC, claimId DESC
           ) AS rowNum
    FROM dbo.Claim
)
UPDATE client
SET clientFullName = latest.fullName,
    clientEmail = latest.email
FROM dbo.Client AS client
INNER JOIN LatestClaimPerPolicy AS latest
    ON latest.policyNumber = client.clientPolicyNum
WHERE latest.rowNum = 1;

;WITH LatestClaimPerPolicy AS (
    SELECT policyNumber, fullName, email,
           ROW_NUMBER() OVER (
               PARTITION BY policyNumber
               ORDER BY createdAt DESC, claimId DESC
           ) AS rowNum
    FROM dbo.Claim
)
INSERT INTO dbo.Client (clientPolicyNum, clientFullName, clientEmail)
SELECT latest.policyNumber, latest.fullName, latest.email
FROM LatestClaimPerPolicy AS latest
WHERE latest.rowNum = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.Client AS existing
      WHERE existing.clientPolicyNum = latest.policyNumber
  );

-- 4. Insert sample prescriptions linked by claim number, not assumed identity values
INSERT INTO dbo.PrescriptionDetails
    (prescriptionNumber, doctorName, datePrescribed, medicationName, prescriptionClaimId)
SELECT seed.prescriptionNumber, seed.doctorName, seed.datePrescribed,
       seed.medicationName, claim.claimId
FROM (VALUES
    ('RX-2001', 'CLM-A1B2C3D1', 'Dr. Alice Carter', DATEADD(day, -15, GETDATE()), 'Amoxicillin'),
    ('RX-2002', 'CLM-A1B2C3D2', 'Dr. Bob Stevens', DATEADD(day, -14, GETDATE()), 'Lisinopril'),
    ('RX-2003', 'CLM-A1B2C3D3', 'Dr. Clara Vance', DATEADD(day, -13, GETDATE()), 'Metformin'),
    ('RX-2004', 'CLM-A1B2C3D4', 'Dr. Dan Evans', DATEADD(day, -12, GETDATE()), 'Atorvastatin'),
    ('RX-2005', 'CLM-A1B2C3D5', 'Dr. Alice Carter', DATEADD(day, -11, GETDATE()), 'Omeprazole'),
    ('RX-2006', 'CLM-A1B2C3D6', 'Dr. Frank Wright', DATEADD(day, -10, GETDATE()), 'Levothyroxine'),
    ('RX-2007', 'CLM-A1B2C3D7', 'Dr. Bob Stevens', DATEADD(day, -9, GETDATE()), 'Ibuprofen 800mg'),
    ('RX-2008', 'CLM-A1B2C3D8', 'Dr. Grace Lee', DATEADD(day, -8, GETDATE()), 'Gabapentin'),
    ('RX-2009', 'CLM-A1B2C3D9', 'Dr. Clara Vance', DATEADD(day, -7, GETDATE()), 'Sertraline'),
    ('RX-2010', 'CLM-A1B2C3E0', 'Dr. Dan Evans', DATEADD(day, -6, GETDATE()), 'Losartan')
) AS seed(prescriptionNumber, claimNumber, doctorName, datePrescribed, medicationName)
INNER JOIN dbo.Claim AS claim
    ON claim.claimNumber = seed.claimNumber
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.PrescriptionDetails AS existing
    WHERE existing.prescriptionClaimId = claim.claimId
);



-- DB Operations ---
select * from Claim
select * from Client
select * from PrescriptionDetails
