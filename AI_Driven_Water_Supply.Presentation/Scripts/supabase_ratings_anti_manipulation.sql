-- Run in Supabase SQL editor. Safe to re-run (IF NOT EXISTS).

alter table public.reviews
  add column if not exists ip_address text,
  add column if not exists device_fingerprint text,
  add column if not exists reviewer_id uuid references public.profiles(id);

create index if not exists reviews_provider_created_at_idx
  on public.reviews (provider_id, created_at desc);

create index if not exists reviews_provider_ip_created_idx
  on public.reviews (provider_id, ip_address, created_at desc);

create index if not exists reviews_provider_fp_created_idx
  on public.reviews (provider_id, device_fingerprint, created_at desc);

alter table public.profiles
  add column if not exists trust_score numeric not null default 1.0;

update public.profiles set trust_score = 1.0 where trust_score is null;
