WITH ranked_published AS (
    SELECT id,
           FIRST_VALUE(id) OVER (
               PARTITION BY store_id
               ORDER BY published_at DESC NULLS LAST, created_at DESC, id DESC) AS winner_id,
           ROW_NUMBER() OVER (
               PARTITION BY store_id
               ORDER BY published_at DESC NULLS LAST, created_at DESC, id DESC) AS position
    FROM authorization_models
    WHERE state = 'Published'
)
UPDATE authorization_models AS model
SET state = 'Archived',
    archived_at = COALESCE(model.archived_at, NOW()),
    superseded_by = ranked.winner_id,
    revision = model.revision + 1
FROM ranked_published AS ranked
WHERE model.id = ranked.id
  AND ranked.position > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_authorization_models_single_published_per_store
    ON authorization_models(store_id)
    WHERE state = 'Published';
