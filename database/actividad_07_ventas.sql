-- POWER GYM - ACTIVIDAD 7 - TABLAS DE VENTAS
-- Este script NO modifica ni elimina socios, planes ni membresias.
-- Ejecutar en la base power_gym; es seguro ejecutarlo de nuevo.
BEGIN;

CREATE TABLE IF NOT EXISTS productos (
    id_producto SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL UNIQUE,
    precio NUMERIC(10, 2) NOT NULL CHECK (precio >= 0),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS ventas (
    id SERIAL PRIMARY KEY,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    cliente VARCHAR(150) NOT NULL,
    total NUMERIC(10, 2) NOT NULL CHECK (total >= 0)
);

CREATE TABLE IF NOT EXISTS detalle_ventas (
    id SERIAL PRIMARY KEY,
    id_venta INT NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
    id_producto INT NOT NULL REFERENCES productos(id_producto) ON DELETE RESTRICT,
    nombre_producto VARCHAR(100) NOT NULL,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10, 2) NOT NULL CHECK (precio_unitario >= 0),
    subtotal NUMERIC(10, 2) NOT NULL CHECK (subtotal >= 0)
);

CREATE INDEX IF NOT EXISTS idx_detalle_ventas_venta ON detalle_ventas(id_venta);
CREATE INDEX IF NOT EXISTS idx_detalle_ventas_producto ON detalle_ventas(id_producto);

-- Articulos de demostracion para la prueba de 3 productos.
-- Si existen, no se insertan duplicados.
INSERT INTO productos (nombre, precio)
VALUES
    ('Agua mineral 600 ml', 5.00),
    ('Barra energetica', 12.00),
    ('Toalla deportiva', 35.00)
ON CONFLICT (nombre) DO NOTHING;

COMMIT;
