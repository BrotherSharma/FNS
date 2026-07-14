ALTER TABLE public.t_user
    ADD COLUMN IF NOT EXISTS c_profile_image_path TEXT NULL;
