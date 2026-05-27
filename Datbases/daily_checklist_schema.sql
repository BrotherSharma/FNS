CREATE TABLE IF NOT EXISTS public.t_daily_checklist (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL,
    c_checklist_date DATE NOT NULL,
    c_task_key TEXT NOT NULL,
    c_task_label TEXT NOT NULL,
    c_is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    c_updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_daily_checklist_user_date_task UNIQUE (c_email, c_checklist_date, c_task_key)
);

CREATE INDEX IF NOT EXISTS idx_daily_checklist_email_date
    ON public.t_daily_checklist (c_email, c_checklist_date);

CREATE TABLE IF NOT EXISTS public.t_daily_mood (
    c_id SERIAL PRIMARY KEY,
    c_email TEXT NOT NULL,
    c_mood_date DATE NOT NULL,
    c_mood TEXT NOT NULL,
    c_reason TEXT NULL,
    c_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    c_updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_daily_mood_user_date UNIQUE (c_email, c_mood_date)
);

ALTER TABLE public.t_daily_mood
    ADD COLUMN IF NOT EXISTS c_reason TEXT NULL;

CREATE INDEX IF NOT EXISTS idx_daily_mood_email_date
    ON public.t_daily_mood (c_email, c_mood_date);
