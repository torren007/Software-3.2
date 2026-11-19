-- Active: 1700068523370@@127.0.0.1@3306@5to_PizzeriaDB
-- PARTE 1: BD actualizada — pizzas con Descripcion/ImagenUrl + Usuarios para login

DROP DATABASE IF EXISTS 5to_PizzeriaDB;
CREATE DATABASE 5to_PizzeriaDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE 5to_PizzeriaDB;

-- TABLA FUERTE: Clientes
CREATE TABLE Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Direccion VARCHAR(255) NOT NULL
);

-- TABLA FUERTE: Pizzas
-- NUEVO: Descripcion e ImagenUrl para que el landing y el formulario
-- tomen el menú directamente de acá, no del repositorio hardcodeado.
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

-- NUEVO: Usuarios para el login.
-- CHAR(64) = SHA-256 en hex, exactamente lo que devuelve SHA2(..., 256).
CREATE TABLE Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash CHAR(64) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    Rol VARCHAR(30) NOT NULL DEFAULT 'Cliente',
    Activo TINYINT(1) NOT NULL DEFAULT 1
);

-- SEMILLAS: las 3 pizzas que estaban hardcodeadas en RepositorioPizzas.
-- Las rutas apuntan a /images/ (donde están tus fotos reales), no a /img/.
INSERT INTO Pizzas (Variedad, Descripcion, Precio, ImagenUrl) VALUES
    ('Margarita Clásica', 'Salsa de tomate, mozzarella y albahaca fresca.', 5500.00, '/images/margarita.jpg'),
    ('Pepperoni Especial', 'Doble pepperoni con un toque de miel picante.', 6200.00, '/images/pepperoni.jpg'),
    ('Cuatro Quesos', 'Mozzarella, gorgonzola, parmesano y provolone.', 6800.00, '/images/cuatro-quesos.jpg');

-- OPCIONAL: las 4 variedades que tenía el viejo Agregar.cshtml hardcodeado.
-- Si querés que el formulario siga mostrándolas, dejá este bloque;
-- si solo querés las 3 del repositorio, borralo.
INSERT INTO Pizzas (Variedad, Descripcion, Precio, ImagenUrl) VALUES
    ('Muzzarella', 'Salsa de tomate casera, queso muzzarella fundido, orégano y aceitunas negras.', 5500.00, 'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=600&q=80'),
    ('Napolitana', 'Muzzarella, rodajas de tomate natural, toque de ajo fresco y albahaca.', 6200.00, 'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?auto=format&fit=crop&w=600&q=80'),
    ('Fugazzeta', 'Cebolla dorada al orégano, doble capa de muzzarella y pimienta blanca.', 6400.00, 'https://images.unsplash.com/photo-1593560708920-61dd98c46a4e?auto=format&fit=crop&w=600&q=80'),
    ('Calabresa', 'Longaniza calabresa artesanal, toque de ají molido y base de queso suave.', 6800.00, 'https://images.unsplash.com/photo-1628840042765-356cda07504e?auto=format&fit=crop&w=600&q=80');

-- SEMILLA: usuario para probar el login (admin@pizzeria.com / admin1234)
INSERT INTO Usuarios (Email, PasswordHash, Nombre, Rol) VALUES
    ('admin@pizzeria.com', SHA2('admin1234', 256), 'Encargado', 'Admin');