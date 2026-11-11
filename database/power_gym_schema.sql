--
-- PostgreSQL database dump
--

\restrict PHxbYuysnODteSsDkIn3mo5BfkkrKrSzhodboZny6bbgLcDcjMHU0NyseHzXdMf

-- Dumped from database version 18.6
-- Dumped by pg_dump version 18.6

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: membresias; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.membresias (
    id_membresia integer NOT NULL,
    id_socio integer NOT NULL,
    id_plan integer NOT NULL,
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL,
    precio_aplicado numeric(10,2) NOT NULL,
    estado character varying(15) DEFAULT 'ACTIVA'::character varying NOT NULL,
    CONSTRAINT membresias_check CHECK ((fecha_fin >= fecha_inicio)),
    CONSTRAINT membresias_estado_check CHECK (((estado)::text = ANY ((ARRAY['ACTIVA'::character varying, 'CANCELADA'::character varying])::text[]))),
    CONSTRAINT membresias_precio_aplicado_check CHECK ((precio_aplicado >= (0)::numeric))
);


ALTER TABLE public.membresias OWNER TO postgres;

--
-- Name: membresias_id_membresia_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.membresias ALTER COLUMN id_membresia ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.membresias_id_membresia_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: planes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.planes (
    id_plan integer NOT NULL,
    nombre character varying(50) NOT NULL,
    duracion_dias integer NOT NULL,
    precio numeric(10,2) NOT NULL,
    activo boolean DEFAULT true NOT NULL,
    CONSTRAINT planes_duracion_dias_check CHECK ((duracion_dias > 0)),
    CONSTRAINT planes_precio_check CHECK ((precio >= (0)::numeric))
);


ALTER TABLE public.planes OWNER TO postgres;

--
-- Name: planes_id_plan_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.planes ALTER COLUMN id_plan ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.planes_id_plan_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: socios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.socios (
    id_socio integer NOT NULL,
    nombres character varying(80) NOT NULL,
    apellidos character varying(80) NOT NULL,
    documento character varying(20) NOT NULL,
    telefono character varying(25),
    correo character varying(120),
    fecha_registro date DEFAULT CURRENT_DATE NOT NULL,
    activo boolean DEFAULT true NOT NULL
);


ALTER TABLE public.socios OWNER TO postgres;

--
-- Name: socios_id_socio_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.socios ALTER COLUMN id_socio ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.socios_id_socio_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: membresias membresias_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.membresias
    ADD CONSTRAINT membresias_pkey PRIMARY KEY (id_membresia);


--
-- Name: planes planes_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.planes
    ADD CONSTRAINT planes_nombre_key UNIQUE (nombre);


--
-- Name: planes planes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.planes
    ADD CONSTRAINT planes_pkey PRIMARY KEY (id_plan);


--
-- Name: socios socios_documento_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.socios
    ADD CONSTRAINT socios_documento_key UNIQUE (documento);


--
-- Name: socios socios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.socios
    ADD CONSTRAINT socios_pkey PRIMARY KEY (id_socio);


--
-- Name: membresias membresias_id_plan_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.membresias
    ADD CONSTRAINT membresias_id_plan_fkey FOREIGN KEY (id_plan) REFERENCES public.planes(id_plan);


--
-- Name: membresias membresias_id_socio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.membresias
    ADD CONSTRAINT membresias_id_socio_fkey FOREIGN KEY (id_socio) REFERENCES public.socios(id_socio);


--
-- PostgreSQL database dump complete
--

\unrestrict PHxbYuysnODteSsDkIn3mo5BfkkrKrSzhodboZny6bbgLcDcjMHU0NyseHzXdMf

