CREATE DATABASE LibraryDB;
GO

USE LibraryDB;
GO

-- Tabela Users
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE, -- Email powinien być unikalny
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL
);

-- Tabela Categories
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(100) NOT NULL UNIQUE -- Unikalna nazwa kategorii
);

-- Tabela Books
CREATE TABLE Books (
    BookID INT PRIMARY KEY IDENTITY(1,1),
    Title VARCHAR(255) NOT NULL,
    Author VARCHAR(255) NOT NULL,
    ISBN VARCHAR(20) NOT NULL UNIQUE, -- ISBN powinien być unikalny
    YearPublished INT NOT NULL
);

-- Tabela BookCategories (pośrednicząca Books <-> Categories)
CREATE TABLE BookCategories (
    BookID INT NOT NULL,
    CategoryID INT NOT NULL,
    PRIMARY KEY (BookID, CategoryID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE CASCADE,
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE CASCADE
);

-- Tabela Copies (egzemplarze książek)
CREATE TABLE Copies (
    CopyID INT PRIMARY KEY IDENTITY(1,1),
    BookID INT NOT NULL,
    CatalogNumber VARCHAR(50) UNIQUE NOT NULL, 
    Available BIT NOT NULL DEFAULT 1, -- 1 = dostępny, 0 = niedostępny
    FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE CASCADE
);

-- Tabela Loans (wypożyczenia egzemplarzy)
CREATE TABLE Loans (
    LoanID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    CopyID INT NOT NULL,
    LoanDate DATETIME DEFAULT GETDATE() NOT NULL,
    DueDate DATETIME NOT NULL,
    ReturnDate DATETIME NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (CopyID) REFERENCES Copies(CopyID) ON DELETE CASCADE
);

-- Tabela Notifications
CREATE TABLE Notifications (
    NotificationID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    Message VARCHAR(255) NOT NULL,
    SentDate DATETIME DEFAULT GETDATE() NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);


-- Użytkownicy
INSERT INTO Users (FirstName, LastName, Email, PasswordHash, Role)
VALUES
('Jan', 'Kowalski', 'jan.kowalski@example.com', 'hash1', 'Bibliotekarz'),
('Anna', 'Nowak', 'anna.nowak@example.com', 'hash2', 'Klient');

-- Kategorie
INSERT INTO Categories (Name)
VALUES
('Fantasy'),
('Science Fiction'),
('History');

-- Książki
INSERT INTO Books (Title, Author, ISBN, YearPublished)
VALUES
('Władca Pierścieni', 'J.R.R. Tolkien', '978-0261102385', 1954),
('Diuna', 'Frank Herbert', '978-0441172719', 1965);

-- Przypisanie kategorii do książek
INSERT INTO BookCategories (BookID, CategoryID)
VALUES
(1, 1), -- Władca Pierścieni → Fantasy
(2, 1), -- Diuna → Fantasy
(2, 2); -- Diuna → Science Fiction

-- Egzemplarze
INSERT INTO Copies (BookID, CatalogNumber)
VALUES
(1, 'FAN-001'),
(1, 'FAN-002'),
(2, 'SCI-001');

-- Wypożyczenia
INSERT INTO Loans (UserID, CopyID, DueDate)
VALUES
(1, 1, DATEADD(DAY, 14, GETDATE()));

-- Powiadomienia
INSERT INTO Notifications (UserID, Message)
VALUES
(1, 'Twoje wypożyczenie wkrótce wygasa.');
