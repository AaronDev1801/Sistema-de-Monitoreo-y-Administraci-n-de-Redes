delete from telemetry
where id in (
  select id
  from telemetry
  order by created_at asc
  limit greatest((select count(*) from telemetry) - 10, 0)
);

delete from alerts
where id in (
  select id
  from alerts
  order by created_at asc
  limit greatest((select count(*) from alerts) - 10, 0)
);
