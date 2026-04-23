-- Run in Supabase SQL editor. Safe to re-run (IF NOT EXISTS).
-- Stores moderation events for admin review. Tighten RLS for production (see comments).

create table if not exists public.admin_alerts (
  id uuid primary key default gen_random_uuid(),
  alert_type text not null,
  message text not null,
  detail jsonb,
  created_at timestamptz not null default now(),
  read boolean not null default false
);

create index if not exists admin_alerts_created_at_idx
  on public.admin_alerts (created_at desc);

create index if not exists admin_alerts_read_created_idx
  on public.admin_alerts (read, created_at desc);

alter table public.admin_alerts enable row level security;

-- MVP INSERT: any authenticated user can insert. A malicious client with your anon key could spam rows;
-- tighten later (e.g. Edge Function with service_role only, or SECURITY DEFINER RPC).
drop policy if exists "admin_alerts_insert_authenticated" on public.admin_alerts;
create policy "admin_alerts_insert_authenticated"
  on public.admin_alerts for insert
  to authenticated
  with check (true);

-- SELECT: profiles.role 'admin' (see Login.razor.cs lowercase admin).
drop policy if exists "admin_alerts_select_admin" on public.admin_alerts;
create policy "admin_alerts_select_admin"
  on public.admin_alerts for select
  to authenticated
  using (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

-- Optional: allow admins to mark alerts read (uncomment if you add an admin UI update).
-- drop policy if exists "admin_alerts_update_admin" on public.admin_alerts;
-- create policy "admin_alerts_update_admin"
--   on public.admin_alerts for update
--   to authenticated
--   using (
--     exists (
--       select 1 from public.profiles p
--       where p.id = auth.uid() and lower(p.role) = 'admin'
--     )
--   )
--   with check (true);
