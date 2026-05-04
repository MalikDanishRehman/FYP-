-- Admin panel schema, indexes, RLS, and RPC helpers.
-- Run in Supabase SQL Editor after public.orders, public.profiles, and public.bills exist.
-- Safe to re-run (IF NOT EXISTS / OR REPLACE). Includes public.admin_alerts (no separate script required).
-- Admin role check: lower(profiles.role) = 'admin'

-- ---------------------------------------------------------------------------
-- 1) profiles: moderation + contact (does not enable RLS on profiles here;
--    review your existing policies before tightening profiles RLS in production.)
-- ---------------------------------------------------------------------------
alter table public.profiles
  add column if not exists phone text;

alter table public.profiles
  add column if not exists account_status text not null default 'active';

do $$
begin
  if not exists (
    select 1 from pg_constraint
    where conname = 'profiles_account_status_check'
  ) then
    alter table public.profiles
      add constraint profiles_account_status_check
      check (account_status in ('active', 'banned', 'suspended'));
  end if;
end $$;

alter table public.profiles
  add column if not exists updated_at timestamptz default now();

create index if not exists profiles_role_idx on public.profiles (role);
create index if not exists profiles_account_status_idx on public.profiles (account_status);

create or replace function public.set_profiles_updated_at()
returns trigger as $$
begin
  new.updated_at = now();
  return new;
end;
$$ language plpgsql;

drop trigger if exists profiles_set_updated_at on public.profiles;
create trigger profiles_set_updated_at
  before update on public.profiles
  for each row execute function public.set_profiles_updated_at();

-- ---------------------------------------------------------------------------
-- 2) disputes
-- ---------------------------------------------------------------------------
create table if not exists public.disputes (
  id uuid primary key default gen_random_uuid(),
  order_id bigint references public.orders(id) on delete set null,
  consumer_id uuid references public.profiles(id) on delete set null,
  provider_id uuid references public.profiles(id) on delete set null,
  issue_type text not null,
  description text not null,
  priority text not null check (priority in ('Low', 'Med', 'High')),
  status text not null default 'Pending' check (status in ('Pending', 'Resolved', 'Closed')),
  resolution_notes text,
  resolved_by uuid references public.profiles(id) on delete set null,
  resolved_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists disputes_status_created_idx
  on public.disputes (status, created_at desc);
create index if not exists disputes_priority_status_idx
  on public.disputes (priority, status);
create index if not exists disputes_consumer_idx on public.disputes (consumer_id);
create index if not exists disputes_provider_idx on public.disputes (provider_id);

create or replace function public.set_disputes_updated_at()
returns trigger as $$
begin
  new.updated_at = now();
  return new;
end;
$$ language plpgsql;

drop trigger if exists disputes_set_updated_at on public.disputes;
create trigger disputes_set_updated_at
  before update on public.disputes
  for each row execute function public.set_disputes_updated_at();

alter table public.disputes enable row level security;

drop policy if exists "disputes_select_party_or_admin" on public.disputes;
create policy "disputes_select_party_or_admin"
  on public.disputes for select
  to authenticated
  using (
    consumer_id = auth.uid()
    or provider_id = auth.uid()
    or exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

drop policy if exists "disputes_insert_party" on public.disputes;
create policy "disputes_insert_party"
  on public.disputes for insert
  to authenticated
  with check (
    consumer_id = auth.uid()
    or provider_id = auth.uid()
  );

drop policy if exists "disputes_update_admin" on public.disputes;
create policy "disputes_update_admin"
  on public.disputes for update
  to authenticated
  using (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  )
  with check (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

-- ---------------------------------------------------------------------------
-- 3) admin_audit_log
-- ---------------------------------------------------------------------------
create table if not exists public.admin_audit_log (
  id bigserial primary key,
  admin_id uuid not null references public.profiles(id) on delete cascade,
  action text not null,
  entity_type text,
  entity_id text,
  payload jsonb,
  created_at timestamptz not null default now()
);

create index if not exists admin_audit_log_created_idx
  on public.admin_audit_log (created_at desc);
create index if not exists admin_audit_log_admin_created_idx
  on public.admin_audit_log (admin_id, created_at desc);

alter table public.admin_audit_log enable row level security;

drop policy if exists "admin_audit_log_select_admin" on public.admin_audit_log;
create policy "admin_audit_log_select_admin"
  on public.admin_audit_log for select
  to authenticated
  using (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

drop policy if exists "admin_audit_log_insert_self_admin" on public.admin_audit_log;
create policy "admin_audit_log_insert_self_admin"
  on public.admin_audit_log for insert
  to authenticated
  with check (
    admin_id = auth.uid()
    and exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

-- ---------------------------------------------------------------------------
-- 4) admin_preferences (per admin user)
-- ---------------------------------------------------------------------------
create table if not exists public.admin_preferences (
  admin_id uuid primary key references public.profiles(id) on delete cascade,
  preferences jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now()
);

alter table public.admin_preferences enable row level security;

drop policy if exists "admin_preferences_select_own_admin" on public.admin_preferences;
create policy "admin_preferences_select_own_admin"
  on public.admin_preferences for select
  to authenticated
  using (
    admin_id = auth.uid()
    and exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

drop policy if exists "admin_preferences_upsert_own_admin" on public.admin_preferences;
create policy "admin_preferences_upsert_own_admin"
  on public.admin_preferences for insert
  to authenticated
  with check (
    admin_id = auth.uid()
    and exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

drop policy if exists "admin_preferences_update_own_admin" on public.admin_preferences;
create policy "admin_preferences_update_own_admin"
  on public.admin_preferences for update
  to authenticated
  using (
    admin_id = auth.uid()
    and exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  )
  with check (
    admin_id = auth.uid()
    and exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

-- ---------------------------------------------------------------------------
-- 5) RPC: dashboard metrics (admin-only, security definer bypasses RLS safely after check)
-- ---------------------------------------------------------------------------
create or replace function public.admin_dashboard_metrics()
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  is_adm boolean;
  rev bigint;
  vend bigint;
  succ numeric;
  pend_dis bigint;
  pend_ord bigint;
begin
  select exists (
    select 1 from public.profiles p
    where p.id = auth.uid() and lower(p.role) = 'admin'
  ) into is_adm;

  if not is_adm then
    raise exception 'forbidden';
  end if;

  select coalesce(sum(o.total_price), 0)::bigint into rev from public.orders o;

  select count(*)::bigint into vend
  from public.profiles pr
  where pr.role = 'Provider' and coalesce(pr.account_status, 'active') = 'active';

  select case
    when count(*) = 0 then 100::numeric
    else round(
      100.0 * (count(*) filter (where lower(coalesce(o.status, '')) in ('delivered', 'completed')))
      / nullif(count(*), 0),
      1
    )
  end into succ
  from public.orders o;

  select count(*)::bigint into pend_dis from public.disputes d where d.status = 'Pending';

  select count(*)::bigint into pend_ord from public.orders o2 where lower(coalesce(o2.status, '')) = 'pending';

  return jsonb_build_object(
    'total_revenue_pkr', rev,
    'active_vendors', vend,
    'success_rate_percent', succ,
    'pending_disputes', pend_dis,
    'pending_orders', pend_ord
  );
end;
$$;

revoke all on function public.admin_dashboard_metrics() from public;
grant execute on function public.admin_dashboard_metrics() to authenticated;

-- ---------------------------------------------------------------------------
-- 6) RPC: profile moderation counts
-- ---------------------------------------------------------------------------
create or replace function public.admin_profile_counts()
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  is_adm boolean;
begin
  select exists (
    select 1 from public.profiles p
    where p.id = auth.uid() and lower(p.role) = 'admin'
  ) into is_adm;

  if not is_adm then
    raise exception 'forbidden';
  end if;

  return jsonb_build_object(
    'consumers', (select count(*)::bigint from public.profiles where role = 'Consumer'),
    'providers', (select count(*)::bigint from public.profiles where role = 'Provider'),
    'banned', (select count(*)::bigint from public.profiles where coalesce(account_status, 'active') = 'banned'),
    'active_consumers', (select count(*)::bigint from public.profiles where role = 'Consumer' and coalesce(account_status, 'active') = 'active')
  );
end;
$$;

revoke all on function public.admin_profile_counts() from public;
grant execute on function public.admin_profile_counts() to authenticated;

-- ---------------------------------------------------------------------------
-- 6b) Privileged admin RPCs (avoid relying on profiles RLS for moderation)
-- ---------------------------------------------------------------------------
create or replace function public.admin_set_account_status(p_target uuid, p_status text)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
  if not exists (
    select 1 from public.profiles p
    where p.id = auth.uid() and lower(p.role) = 'admin'
  ) then
    raise exception 'forbidden';
  end if;

  if p_target = auth.uid() then
    raise exception 'cannot_change_self';
  end if;

  if p_status is null or p_status not in ('active', 'banned', 'suspended') then
    raise exception 'invalid_status';
  end if;

  update public.profiles
  set account_status = p_status,
      updated_at = now()
  where id = p_target;
end;
$$;

revoke all on function public.admin_set_account_status(uuid, text) from public;
grant execute on function public.admin_set_account_status(uuid, text) to authenticated;

create or replace function public.admin_resolve_dispute(p_id uuid, p_notes text)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
  if not exists (
    select 1 from public.profiles p
    where p.id = auth.uid() and lower(p.role) = 'admin'
  ) then
    raise exception 'forbidden';
  end if;

  update public.disputes
  set status = 'Resolved',
      resolution_notes = p_notes,
      resolved_by = auth.uid(),
      resolved_at = now(),
      updated_at = now()
  where id = p_id;
end;
$$;

revoke all on function public.admin_resolve_dispute(uuid, text) from public;
grant execute on function public.admin_resolve_dispute(uuid, text) to authenticated;

-- Optional: allow admins to read all orders for dashboard tables (combines with existing policies via OR).
drop policy if exists "orders_select_admin" on public.orders;
create policy "orders_select_admin"
  on public.orders for select
  to authenticated
  using (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

drop policy if exists "bills_select_admin" on public.bills;
create policy "bills_select_admin"
  on public.bills for select
  to authenticated
  using (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );

-- ---------------------------------------------------------------------------
-- 7) admin_alerts: create table + RLS (standalone; no need to run supabase_admin_alerts.sql first)
-- ---------------------------------------------------------------------------
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

-- MVP INSERT: any authenticated user can insert (tighten in production if needed).
drop policy if exists "admin_alerts_insert_authenticated" on public.admin_alerts;
create policy "admin_alerts_insert_authenticated"
  on public.admin_alerts for insert
  to authenticated
  with check (true);

-- SELECT: admins only (profiles.role lowercase 'admin', see Login.razor.cs).
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

-- UPDATE: admins mark alerts read.
drop policy if exists "admin_alerts_update_admin" on public.admin_alerts;
create policy "admin_alerts_update_admin"
  on public.admin_alerts for update
  to authenticated
  using (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  )
  with check (
    exists (
      select 1 from public.profiles p
      where p.id = auth.uid() and lower(p.role) = 'admin'
    )
  );
