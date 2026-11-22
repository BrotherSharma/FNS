-- FNS PostgreSQL database schema
-- Run this script once using psql or pgAdmin on the server database.

BEGIN;

CREATE TABLE IF NOT EXISTS public.t_user (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL UNIQUE,
    c_password TEXT NOT NULL,
    c_role TEXT NOT NULL DEFAULT 'EndUser',
    c_firstname TEXT,
    c_lastname TEXT,
    c_username TEXT,
    c_gender TEXT,
    c_dob TIMESTAMP,
    c_createddate TIMESTAMP NOT NULL DEFAULT NOW(),
    c_profile_image_path TEXT,
    c_is_premium BOOLEAN NOT NULL DEFAULT FALSE
);

ALTER TABLE public.t_user
    ADD COLUMN IF NOT EXISTS c_password TEXT,
    ADD COLUMN IF NOT EXISTS c_role TEXT NOT NULL DEFAULT 'EndUser',
    ADD COLUMN IF NOT EXISTS c_firstname TEXT,
    ADD COLUMN IF NOT EXISTS c_lastname TEXT,
    ADD COLUMN IF NOT EXISTS c_username TEXT,
    ADD COLUMN IF NOT EXISTS c_gender TEXT,
    ADD COLUMN IF NOT EXISTS c_dob TIMESTAMP,
    ADD COLUMN IF NOT EXISTS c_createddate TIMESTAMP NOT NULL DEFAULT NOW(),
    ADD COLUMN IF NOT EXISTS c_profile_image_path TEXT,
    ADD COLUMN IF NOT EXISTS c_is_premium BOOLEAN NOT NULL DEFAULT FALSE;

CREATE TABLE IF NOT EXISTS public.t_healthinfo (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL,
    c_weight DOUBLE PRECISION NOT NULL DEFAULT 0,
    c_height DOUBLE PRECISION NOT NULL DEFAULT 0,
    c_age INTEGER NOT NULL DEFAULT 0,
    c_goal TEXT NOT NULL DEFAULT '',
    c_referral TEXT,
    c_diet TEXT NOT NULL DEFAULT '',
    c_lifestyle TEXT NOT NULL DEFAULT '',
    c_bloodtype TEXT NOT NULL DEFAULT '',
    c_sleeppatterns DOUBLE PRECISION NOT NULL DEFAULT 0,
    c_alcoholconsumption TEXT NOT NULL DEFAULT '',
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    c_updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_t_healthinfo_email UNIQUE (c_email),
    CONSTRAINT fk_t_healthinfo_user
        FOREIGN KEY (c_email) REFERENCES public.t_user(c_email) ON DELETE CASCADE
);

ALTER TABLE public.t_healthinfo
    ADD COLUMN IF NOT EXISTS c_weight DOUBLE PRECISION NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS c_height DOUBLE PRECISION NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS c_age INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS c_goal TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS c_referral TEXT,
    ADD COLUMN IF NOT EXISTS c_diet TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS c_lifestyle TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS c_bloodtype TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS c_sleeppatterns DOUBLE PRECISION NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS c_alcoholconsumption TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    ADD COLUMN IF NOT EXISTS c_updated_at TIMESTAMP NOT NULL DEFAULT NOW();

CREATE INDEX IF NOT EXISTS idx_t_healthinfo_email
    ON public.t_healthinfo (c_email);

CREATE TABLE IF NOT EXISTS public.t_password_reset (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL,
    c_token TEXT NOT NULL,
    c_expires_at TIMESTAMP NOT NULL,
    c_used BOOLEAN NOT NULL DEFAULT FALSE,
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_t_password_reset_user
        FOREIGN KEY (c_email) REFERENCES public.t_user(c_email) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_t_password_reset_token
    ON public.t_password_reset (c_token);

CREATE INDEX IF NOT EXISTS idx_t_password_reset_email
    ON public.t_password_reset (c_email);

CREATE TABLE IF NOT EXISTS public.t_food_data (
    c_id SERIAL PRIMARY KEY,
    c_food_name TEXT,
    c_meal TEXT,
    c_notes TEXT,
    c_email TEXT NOT NULL,
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_t_food_data_user
        FOREIGN KEY (c_email) REFERENCES public.t_user(c_email) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_t_food_data_email
    ON public.t_food_data (c_email);

CREATE TABLE IF NOT EXISTS public.t_daily_checklist (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL,
    c_checklist_date DATE NOT NULL,
    c_task_key TEXT NOT NULL,
    c_task_label TEXT NOT NULL,
    c_is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    c_updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_daily_checklist_user_date_task
        UNIQUE (c_email, c_checklist_date, c_task_key),
    CONSTRAINT fk_t_daily_checklist_user
        FOREIGN KEY (c_email) REFERENCES public.t_user(c_email) ON DELETE CASCADE
);

ALTER TABLE public.t_daily_checklist
    ADD COLUMN IF NOT EXISTS c_updated_at TIMESTAMP NOT NULL DEFAULT NOW();

CREATE INDEX IF NOT EXISTS idx_daily_checklist_email_date
    ON public.t_daily_checklist (c_email, c_checklist_date);

CREATE TABLE IF NOT EXISTS public.t_daily_mood (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL,
    c_mood_date DATE NOT NULL,
    c_mood TEXT NOT NULL,
    c_reason TEXT,
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    c_updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_daily_mood_user_date UNIQUE (c_email, c_mood_date),
    CONSTRAINT fk_t_daily_mood_user
        FOREIGN KEY (c_email) REFERENCES public.t_user(c_email) ON DELETE CASCADE
);

ALTER TABLE public.t_daily_mood
    ADD COLUMN IF NOT EXISTS c_reason TEXT,
    ADD COLUMN IF NOT EXISTS c_updated_at TIMESTAMP NOT NULL DEFAULT NOW();

CREATE INDEX IF NOT EXISTS idx_daily_mood_email_date
    ON public.t_daily_mood (c_email, c_mood_date);

CREATE TABLE IF NOT EXISTS public.payment_approvals (
    id SERIAL PRIMARY KEY,
    user_id INTEGER,
    user_email VARCHAR(255) NOT NULL,
    user_name VARCHAR(255) NOT NULL,
    amount NUMERIC(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    order_id VARCHAR(100) NOT NULL UNIQUE,
    approval_token VARCHAR(500) NOT NULL UNIQUE,
    payment_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    approval_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    approved_at TIMESTAMP,
    approved_by VARCHAR(255),
    rejection_reason TEXT
);

CREATE INDEX IF NOT EXISTS idx_payment_approvals_user_id
    ON public.payment_approvals (user_id);

CREATE INDEX IF NOT EXISTS idx_payment_approvals_user_email
    ON public.payment_approvals (user_email);

COMMIT;
