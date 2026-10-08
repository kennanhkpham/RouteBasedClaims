create database InsuranceClaimDb
use InsuranceClaimDb

--- Tables ---

create table Client(
    clientId int primary key identity(1,1),
    clientPolicyNum varchar(50) not null,
    clientFullName varchar(100) not null,
    clientEmail varchar(100) not null
);

create table Claim(
    claimId int primary key identity(1,1),
    claimNumber varchar(50) not null,
    policyNumber varchar(50) not null,
    fullName varchar(100) not null,
    email varchar(100) not null,
    amountCost decimal(18,2) not null,
    status varchar(50) not null default 'submitted',
    createdAt datetime default getdate() not null,
);

create table PrescriptionDetails(
    prescriptionId int primary key identity(1,1),
    prescriptionNumber varchar(50) not null,
    doctorName varchar(100) not null,
    datePrescribed datetime default getdate() not null,
    medicationName varchar(50) not null,
    prescriptionClaimId int not null unique,
    constraint fk_claimId foreign key (prescriptionClaimId) references Claim(claimId) 
);

--- Sample Data ---


select * from Claim
select * from Client
select * from PrescriptionDetails

