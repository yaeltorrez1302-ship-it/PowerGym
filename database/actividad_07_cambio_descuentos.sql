-- Migracion no destructiva: conserva todas las ventas previas.
BEGIN;

CREATE TABLE IF NOT EXISTS vendedores (
    id SERIAL PRIMARY KEY,
    usuario VARCHAR(60) NOT NULL UNIQUE,
    nombre VARCHAR(120) NOT NULL,
    rol VARCHAR(12) NOT NULL CHECK (rol IN ('VENDEDOR','AUDITOR')),
    hash_password TEXT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE ventas ADD COLUMN IF NOT EXISTS subtotal NUMERIC(10,2);
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS descuento_aplicado NUMERIC(10,2) NOT NULL DEFAULT 0;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS tipo_descuento VARCHAR(12) NOT NULL DEFAULT 'NINGUNO';
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS valor_descuento NUMERIC(10,2) NOT NULL DEFAULT 0;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS vendedor_id INTEGER REFERENCES vendedores(id);
UPDATE ventas SET subtotal = total WHERE subtotal IS NULL;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_ventas_descuento_20') THEN
        ALTER TABLE ventas ADD CONSTRAINT ck_ventas_descuento_20 CHECK (
            subtotal >= 0 AND
            descuento_aplicado >= 0 AND
            descuento_aplicado <= round(subtotal * 0.20, 2) AND
            total = subtotal - descuento_aplicado AND
            valor_descuento >= 0 AND
            tipo_descuento IN ('NINGUNO','PORCENTAJE','MONTO')
        );
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS auditoria_descuentos (
    id BIGSERIAL PRIMARY KEY,
    id_venta INTEGER NOT NULL UNIQUE REFERENCES ventas(id) ON DELETE RESTRICT,
    vendedor_id INTEGER NOT NULL REFERENCES vendedores(id) ON DELETE RESTRICT,
    fecha TIMESTAMPTZ NOT NULL DEFAULT now(),
    subtotal NUMERIC(10,2) NOT NULL,
    tipo_descuento VARCHAR(12) NOT NULL,
    valor_solicitado NUMERIC(10,2) NOT NULL,
    descuento_aplicado NUMERIC(10,2) NOT NULL CHECK (descuento_aplicado > 0),
    total_final NUMERIC(10,2) NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_auditoria_descuentos_vendedor ON auditoria_descuentos(vendedor_id, fecha DESC);

-- Auditoria inmutable desde las operaciones normales de aplicacion.
CREATE OR REPLACE FUNCTION impedir_modificacion_auditoria_descuentos()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Los registros de auditoria no se pueden modificar ni borrar';
END;
$$;
DROP TRIGGER IF EXISTS trg_auditoria_descuentos_inmutable ON auditoria_descuentos;
CREATE TRIGGER trg_auditoria_descuentos_inmutable
BEFORE UPDATE OR DELETE ON auditoria_descuentos
FOR EACH ROW EXECUTE FUNCTION impedir_modificacion_auditoria_descuentos();

COMMIT;
