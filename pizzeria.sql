DROP DATABASE IF EXISTS 5to_PizzeriaDB;
CREATE DATABASE 5to_PizzeriaDB;
USE 5to_PizzeriaDB;

-- TABLA FUERTE: Clientes
CREATE TABLE Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Direccion VARCHAR(255) NOT NULL
);

-- TABLA FUERTE: Pizzas (ahora con Descripcion e ImagenUrl para el landing)
CREATE TABLE Pizzas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Variedad VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    Precio DECIMAL(10, 2) NOT NULL,
    ImagenUrl VARCHAR(255) NULL
);

-- TABLA DEPENDIENTE: Pedidos
CREATE TABLE Pedidos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ClienteId INT NOT NULL,
    ActorAsignado VARCHAR(50) DEFAULT 'Cocina',
    Estado VARCHAR(50) DEFAULT 'Espera de confirmación',
    Activo TINYINT(1) DEFAULT 1,
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE
);

-- TABLA PUENTE: DetallesPedido
CREATE TABLE DetallesPedido (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PedidoId INT NOT NULL,
    PizzaId INT NOT NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
    FOREIGN KEY (PizzaId) REFERENCES Pizzas(Id) ON DELETE CASCADE
);

-- NUEVO: Usuarios para el login. CHAR(64) = SHA-256 en hex, igual que SHA2(..., 256).
CREATE TABLE Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash CHAR(64) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    Rol VARCHAR(30) NOT NULL DEFAULT 'Cliente',
    Activo TINYINT(1) NOT NULL DEFAULT 1
);

-- DATOS INICIALES: las pizzas que antes estaban hardcodeadas
INSERT INTO Pizzas (Variedad, Descripcion, Precio, ImagenUrl) VALUES
    ('Margarita Clásica', 'Salsa de tomate, mozzarella y albahaca fresca.', 5500.00, '/img/margarita.jpg'),
    ('Pepperoni Especial', 'Doble pepperoni con un toque de miel picante.', 6200.00, '/img/pepperoni.jpg'),
    ('Cuatro Quesos', 'Mozzarella, gorgonzola, parmesano y provolone.', 6800.00, '/img/cuatroquesos.jpg');

-- Usuario semilla. SHA2 genera el hash al insertar: coincide con el hash de la API.
INSERT INTO Usuarios (Email, PasswordHash, Nombre, Rol) VALUES
    ('admin@pizzeria.com', SHA2('admin1234', 256), 'Encargado', 'Admin');