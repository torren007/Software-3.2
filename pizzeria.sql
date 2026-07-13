CREATE DATABASE 5to_PizzeriaDB;
USE 5to_PizzeriaDB;



CREATE TABLE Pedidos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cliente VARCHAR(150) NOT NULL,
    DetallePizza VARCHAR(255) NOT NULL,
    ActorAsignado VARCHAR(50) DEFAULT 'Backend', -- Roles: Backend, Cocina, Reparto
    Estado VARCHAR(50) DEFAULT 'Pendiente', -- Pendiente, En Preparacion, En Viaje, Entregado
    Activo TINYINT DEFAULT 1 -- 1 = Activo, 0 = Borrado Lógico
);

