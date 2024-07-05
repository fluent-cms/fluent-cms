-- Create the authors table
CREATE TABLE authors (
                         id INTEGER PRIMARY KEY AUTOINCREMENT,
                         name TEXT NOT NULL,
                         slug TEXT NOT NULL,
                         bio TEXT,
                         thumbnail_image TEXT,
                         featured_image TEXT,
                         deleted INTEGER DEFAULT 0, -- Using 0 as FALSE
                         created_at INTEGER DEFAULT   (datetime('now', 'localtime')),
                         updated_at INTEGER DEFAULT  (datetime('now', 'localtime'))
);

-- Trigger to update the updated_at column before row update in authors table
CREATE TRIGGER update_authors_updated_at
    BEFORE UPDATE ON authors
    FOR EACH ROW
BEGIN
UPDATE authors
SET updated_at =  (datetime('now', 'localtime'))
WHERE id = OLD.id;
END;

-- Create the categories table
CREATE TABLE categories (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            slug TEXT NOT NULL,
                            description TEXT,
                            parent_category_id INTEGER,
                            thumbnail_image TEXT,
                            featured_image TEXT,
                            deleted INTEGER DEFAULT 0, -- Using 0 as FALSE
                            created_at INTEGER DEFAULT   (datetime('now', 'localtime')),
                            updated_at INTEGER DEFAULT   (datetime('now', 'localtime')),
                            FOREIGN KEY (parent_category_id) REFERENCES categories(id)
);

-- Trigger to update the updated_at column before row update in categories table
CREATE TRIGGER update_categories_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW
BEGIN
UPDATE categories
SET updated_at =  (datetime('now', 'localtime'))
WHERE id = OLD.id;
END;

-- Create the tags table
CREATE TABLE tags (
                      id INTEGER PRIMARY KEY AUTOINCREMENT,
                      name TEXT NOT NULL,
                      slug TEXT NOT NULL,
                      description TEXT,
                      thumbnail_image TEXT,
                      featured_image TEXT,
                      deleted INTEGER DEFAULT 0, -- Using 0 as FALSE
                      created_at INTEGER DEFAULT  (datetime('now', 'localtime')),
                      updated_at INTEGER DEFAULT  (datetime('now', 'localtime'))
);

-- Trigger to update the updated_at column before row update in tags table
CREATE TRIGGER update_tags_updated_at
    BEFORE UPDATE ON tags
    FOR EACH ROW
BEGIN
UPDATE tags
SET updated_at =  (datetime('now', 'localtime'))
WHERE id = OLD.id;
END;

-- Create the posts table
CREATE TABLE posts (
                       id INTEGER PRIMARY KEY AUTOINCREMENT,
                       title TEXT NOT NULL,
                       slug TEXT NOT NULL,
                       reading_time TEXT not null ,
                       excerpt TEXT NOT NULL ,
                       content TEXT,
                       category_id INTEGER,
                       thumbnail_image TEXT,
                       featured_image TEXT,
                       published_at TIMESTAMP DEFAULT  (datetime('now', 'localtime')),
                       deleted INTEGER DEFAULT 0, -- Using 0 as FALSE
                       created_at INTEGER DEFAULT  (datetime('now', 'localtime')),
                       updated_at INTEGER DEFAULT   (datetime('now', 'localtime')),
                       FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- Trigger to update the updated_at column before row update in posts table
CREATE TRIGGER update_posts_updated_at
    BEFORE UPDATE ON posts
    FOR EACH ROW
BEGIN
UPDATE posts
SET updated_at =  (datetime('now', 'localtime'))
WHERE id = OLD.id;
END;

-- Create the post_tags table
CREATE TABLE post_tags (
                           id INTEGER PRIMARY KEY AUTOINCREMENT,
                           post_id INTEGER,
                           tag_id INTEGER,
                           deleted INTEGER DEFAULT 0, -- Using 0 as FALSE
                           created_at INTEGER DEFAULT   (datetime('now', 'localtime')),
                           updated_at INTEGER DEFAULT  (datetime('now', 'localtime')),
                           FOREIGN KEY (post_id) REFERENCES posts(id),
                           FOREIGN KEY (tag_id) REFERENCES tags(id)
);

-- Trigger to update the updated_at column before row update in post_tags table
CREATE TRIGGER update_post_tags_updated_at
    BEFORE UPDATE ON post_tags
    FOR EACH ROW
BEGIN
UPDATE post_tags
SET updated_at =  (datetime('now', 'localtime'))
WHERE id = OLD.id;
END;

-- Create the post_authors table
CREATE TABLE post_authors (
                              id INTEGER PRIMARY KEY AUTOINCREMENT,
                              post_id INTEGER,
                              author_id INTEGER,
                              deleted INTEGER DEFAULT 0, -- Using 0 as FALSE
                              created_at INTEGER DEFAULT  (datetime('now', 'localtime')),
                              updated_at INTEGER DEFAULT  (datetime('now', 'localtime')),
                              FOREIGN KEY (post_id) REFERENCES posts(id),
                              FOREIGN KEY (author_id) REFERENCES authors(id)
);

-- Trigger to update the updated_at column before row update in post_authors table
CREATE TRIGGER update_post_authors_updated_at
    BEFORE UPDATE ON post_authors
    FOR EACH ROW
BEGIN
UPDATE post_authors
SET updated_at =  (datetime('now', 'localtime'))
WHERE id = OLD.id;
END;
