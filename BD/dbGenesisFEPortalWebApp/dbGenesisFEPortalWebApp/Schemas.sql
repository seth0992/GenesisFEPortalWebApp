CREATE DATABASE dbGenesisFEPortalWebApp;
GO

USE dbGenesisFEPortalWebApp;
GO

-- Creación de esquemas
CREATE SCHEMA Security; -- Para autenticación y autorización
GO
CREATE SCHEMA Catalog;  -- Para catálogos/listas de valores
GO
CREATE SCHEMA Core;     -- Para tablas principales del negocio
GO
CREATE SCHEMA Audit;    -- Para auditoría
GO


-- Tablas

/************************************/
/* Tablas del Core                  */
/************************************/
CREATE TABLE Core.Tenants (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL UNIQUE,
    Identification NVARCHAR(50) NOT NULL UNIQUE,
    CommercialName NVARCHAR(255),
    Email NVARCHAR(255),
    Phone NVARCHAR(20),
    Address NVARCHAR(500),
    Logo VARBINARY(MAX),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME
);

/************************************/
/* Tablas de Seguridad             */
/************************************/
CREATE TABLE Security.Roles (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    Permissions NVARCHAR(MAX),
    IsSystem BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
);

CREATE TABLE Security.Users (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    RoleId BIGINT NOT NULL,
    PhoneNumber NVARCHAR(20),
    EmailConfirmed BIT DEFAULT 0,
    TwoFactorEnabled BIT DEFAULT 0,
    LockoutEnd DATETIME,
    AccessFailedCount INT DEFAULT 0,
    LastLoginDate DATETIME,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    FOREIGN KEY (TenantId) REFERENCES Core.Tenants(ID),
    FOREIGN KEY (RoleId) REFERENCES Security.Roles(ID)
);

CREATE TABLE Security.RefreshTokens (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserID BIGINT NOT NULL,
    Token NVARCHAR(MAX) NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CreatedByIp NVARCHAR(50),
    RevokedAt DATETIME,
    RevokedByIp NVARCHAR(50),
    ReplacedByToken NVARCHAR(MAX),
	IsActive BIT DEFAULT 1,           -- Agregar esta columna
    UpdatedAt DATETIME,               -- Agregar esta columna
    FOREIGN KEY (UserID) REFERENCES Security.Users(ID)
);


/************************************/
/* Tablas de Catálogos             */
/************************************/

CREATE TABLE Catalog.IdentificationTypes(
    ID NVARCHAR(3) PRIMARY KEY, 
    Description NVARCHAR(255) NOT NULL -- Nombre del tipo de identificación
);

CREATE TABLE Catalog.Region (
    RegionID INT IDENTITY(1,1) PRIMARY KEY,
    RegionName VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_NombreRegion UNIQUE (RegionName)
);

CREATE TABLE Catalog.Provinces(
    ProvinceID INT PRIMARY KEY, -- Llave primaria con autoincremento
    ProvinceName NVARCHAR(255) NOT NULL UNIQUE -- Nombre de la provincia
);

CREATE TABLE Catalog.Cantons(
    CantonID INT PRIMARY KEY, -- Llave primaria con autoincremento
    CantonName NVARCHAR(255) NOT NULL, -- Nombre del cantón
    ProvinceId INT , -- Provincia
    FOREIGN KEY (ProvinceId) REFERENCES Catalog.Provinces(ProvinceID),
	CONSTRAINT UQ_Canton_Provincia UNIQUE (ProvinceId, CantonName)
);

CREATE TABLE Catalog.Districts(
    DistrictID INT PRIMARY KEY, -- Llave primaria con autoincremento
    DistrictName NVARCHAR(255) NOT NULL, -- Nombre del distrito
    CantonId INT , -- Cantón
	RegionID INT 
    FOREIGN KEY (CantonId) REFERENCES Catalog.Cantons(CantonID),
	FOREIGN KEY (RegionID) REFERENCES  Catalog.Region(RegionID)
);


/************************************/
/* Tablas de Negocio               */
/************************************/
CREATE TABLE Core.Customers (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId BIGINT NOT NULL,
    CustomerName NVARCHAR(255) NOT NULL,
    CommercialName NVARCHAR(255),
    Identification NVARCHAR(255) NOT NULL,
    IdentificationTypeId NVARCHAR(3) NOT NULL,
    Email NVARCHAR(255),
    PhoneCode NVARCHAR(10),
    Phone NVARCHAR(20),
    Address NVARCHAR(500),
    Neighborhood NVARCHAR(250),
    DistrictID INT,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    FOREIGN KEY (TenantId) REFERENCES Core.Tenants(ID),
    FOREIGN KEY (IdentificationTypeId) REFERENCES Catalog.IdentificationTypes(ID),
    FOREIGN KEY (DistrictID) REFERENCES Catalog.Districts(DistrictID)
);

/************************************/
/* Tablas de Auditoría             */
/************************************/
CREATE TABLE Audit.ChangeLog (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    EntityName NVARCHAR(100) NOT NULL,
    EntityID NVARCHAR(50) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    TenantId BIGINT,
    ChangedBy BIGINT,
    ChangedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (TenantId) REFERENCES Core.Tenants(ID)
);

CREATE TABLE Audit.AccessLog (
    ID BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserID BIGINT,
    TenantId BIGINT,
    Action NVARCHAR(50),
    IPAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    Status NVARCHAR(50),
    Details NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Security.Users(ID),
    FOREIGN KEY (TenantId) REFERENCES Core.Tenants(ID)
);


/************************************/
/* Índices                         */
/************************************/
CREATE INDEX IX_Roles_Name ON Security.Roles(Name);
CREATE INDEX IX_Users_Email ON Security.Users(Email);
CREATE INDEX IX_Users_TenantId_RoleId ON Security.Users(TenantId, RoleId);
CREATE INDEX IX_Customers_TenantId ON Core.Customers(TenantId);
CREATE INDEX IX_Customers_Identification ON Core.Customers(Identification);
CREATE INDEX IX_ChangeLog_EntityName_EntityId ON Audit.ChangeLog(EntityName, EntityID);
CREATE INDEX IX_AccessLog_UserId_CreatedAt ON Audit.AccessLog(UserID, CreatedAt);
