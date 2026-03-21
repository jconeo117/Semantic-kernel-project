--
-- PostgreSQL database dump
--

-- Dumped from database version 17.5
-- Dumped by pg_dump version 17.5

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
-- Name: audits; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.audits (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id character varying(100) NOT NULL,
    session_id uuid NOT NULL,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL,
    event_type character varying(100) NOT NULL,
    content text DEFAULT ''::text NOT NULL,
    threat_level character varying(50),
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL
);


ALTER TABLE public.audits OWNER TO postgres;

--
-- Name: chat_sessions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.chat_sessions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id character varying(100) NOT NULL,
    user_phone character varying(50) DEFAULT ''::character varying NOT NULL,
    history_json jsonb DEFAULT '[]'::jsonb NOT NULL,
    needs_human_attention boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.chat_sessions OWNER TO postgres;

--
-- Name: tenant_billing; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tenant_billing (
    tenant_id character varying(100) NOT NULL,
    plan_type character varying(50) DEFAULT 'Trial'::character varying NOT NULL,
    billing_status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    active_until timestamp with time zone,
    suspended_at timestamp with time zone,
    suspension_reason character varying(500),
    notes character varying(1000)
);


ALTER TABLE public.tenant_billing OWNER TO postgres;

--
-- Name: tenants; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tenants (
    tenant_id character varying(100) NOT NULL,
    business_name character varying(200) NOT NULL,
    business_type character varying(100) DEFAULT ''::character varying NOT NULL,
    db_type character varying(50) DEFAULT 'PostgreSql'::character varying NOT NULL,
    connection_string character varying(500) DEFAULT ''::character varying NOT NULL,
    time_zone_id character varying(100) DEFAULT 'UTC'::character varying NOT NULL,
    phone_country_code character varying(10) DEFAULT ''::character varying NOT NULL,
    address character varying(300) DEFAULT ''::character varying NOT NULL,
    phone character varying(50) DEFAULT ''::character varying NOT NULL,
    working_hours character varying(200) DEFAULT ''::character varying NOT NULL,
    services jsonb DEFAULT '[]'::jsonb NOT NULL,
    accepted_insurance jsonb DEFAULT '[]'::jsonb NOT NULL,
    pricing jsonb DEFAULT '{}'::jsonb NOT NULL,
    custom_settings jsonb DEFAULT '{}'::jsonb NOT NULL,
    username character varying(100),
    password_hash character varying(200),
    message_provider character varying(50) DEFAULT 'Meta'::character varying NOT NULL,
    message_provider_account character varying(200) DEFAULT ''::character varying NOT NULL,
    message_provider_token character varying(500) DEFAULT ''::character varying NOT NULL,
    message_provider_phone character varying(50) DEFAULT ''::character varying NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone
);


ALTER TABLE public.tenants OWNER TO postgres;

--
-- Name: audits audits_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audits
    ADD CONSTRAINT audits_pkey PRIMARY KEY (id);


--
-- Name: chat_sessions chat_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.chat_sessions
    ADD CONSTRAINT chat_sessions_pkey PRIMARY KEY (id);


--
-- Name: tenant_billing tenant_billing_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tenant_billing
    ADD CONSTRAINT tenant_billing_pkey PRIMARY KEY (tenant_id);


--
-- Name: tenants tenants_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_pkey PRIMARY KEY (tenant_id);


--
-- Name: ix_audits_session_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_audits_session_id ON public.audits USING btree (session_id);


--
-- Name: ix_audits_tenant_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_audits_tenant_id ON public.audits USING btree (tenant_id);


--
-- Name: ix_audits_timestamp; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_audits_timestamp ON public.audits USING btree ("timestamp" DESC);


--
-- Name: ix_chat_sessions_tenant_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_chat_sessions_tenant_id ON public.chat_sessions USING btree (tenant_id);


--
-- Name: ix_chat_sessions_updated_at; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX ix_chat_sessions_updated_at ON public.chat_sessions USING btree (updated_at DESC);


--
-- Name: tenant_billing tenant_billing_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tenant_billing
    ADD CONSTRAINT tenant_billing_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(tenant_id) ON DELETE CASCADE;


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: pg_database_owner
--

GRANT USAGE ON SCHEMA public TO receptionist_app;


--
-- Name: TABLE audits; Type: ACL; Schema: public; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.audits TO receptionist_app;


--
-- Name: TABLE chat_sessions; Type: ACL; Schema: public; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.chat_sessions TO receptionist_app;


--
-- Name: TABLE tenant_billing; Type: ACL; Schema: public; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.tenant_billing TO receptionist_app;


--
-- Name: TABLE tenants; Type: ACL; Schema: public; Owner: postgres
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public.tenants TO receptionist_app;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO receptionist_app;


--
-- PostgreSQL database dump complete
--

