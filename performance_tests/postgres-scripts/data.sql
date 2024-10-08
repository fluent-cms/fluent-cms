INSERT INTO authors (name, slug, bio, thumbnail_image, featured_image)
SELECT
    'Author ' || i,
    'author-' || i,
    'Bio of author ' || i,
    'http://example.com/thumbnail/' || i,
    'http://example.com/featured/' || i
FROM generate_series(1, 5000000) AS s(i);

INSERT INTO categories (name, slug, description, parent_category_id, thumbnail_image, featured_image)
SELECT
    'Category ' || i,
    'category-' || i,
    'Description of category ' || i,
    NULL,
    'http://example.com/thumbnail/' || i,
    'http://example.com/featured/' || i
FROM generate_series(1, 100000) AS s(i);

INSERT INTO posts (title, slug, reading_time, excerpt, content,
   category_id, thumbnail_image, featured_image, published_at)
SELECT
    'Post Title ' || i,
    'post-title-' || i,
    ('10 minutes'),
    'Excerpt of post ' || i,
    'Content of post ' || i,
    (random()*99999 + 1)::int,
    'http://example.com/thumbnail/' || i,
    'http://example.com/featured/' || i,
        now() + (i || ' seconds')::interval
FROM generate_series(1, 2000000) AS s(i);

INSERT INTO author_post (post_id, author_id)
SELECT
    (random()*1999999 + 1)::int,
    (random()*4999999  + 1)::int
FROM generate_series(1, 10000000) AS s(i);