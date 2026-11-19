-- Solo despues de compilar y aprobar las pruebas.
-- NOT VALID permite conservar y consultar las ventas historicas sin vendedor,
-- pero PostgreSQL exige identificacion en todos los registros NUEVOS.
BEGIN;
UPDATE ventas SET subtotal = total WHERE subtotal IS NULL;
ALTER TABLE ventas ALTER COLUMN subtotal SET NOT NULL;
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_ventas_vendedor_autenticado') THEN
        ALTER TABLE ventas ADD CONSTRAINT ck_ventas_vendedor_autenticado
            CHECK (vendedor_id IS NOT NULL) NOT VALID;
    END IF;
END $$;
COMMIT;
