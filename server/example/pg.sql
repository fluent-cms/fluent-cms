CREATE OR REPLACE FUNCTION update_updated_at_column()
    RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TABLE authors (
                         id SERIAL PRIMARY KEY,
                         name VARCHAR(100) NOT NULL,
                         slug VARCHAR(100) NOT NULL,
                         bio TEXT,
                         thumbnail_image VARCHAR(255),
                         featured_image VARCHAR(255),
                         deleted BOOLEAN DEFAULT FALSE,
                         created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                         updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TRIGGER update_authors_updated_at
    BEFORE UPDATE ON authors
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TABLE categories (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100) NOT NULL,
                            slug VARCHAR(100) NOT NULL,
                            description TEXT,
                            parent_category_id INT,
                            thumbnail_image VARCHAR(255),
                            featured_image VARCHAR(255),
                            deleted BOOLEAN DEFAULT FALSE,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (parent_category_id) REFERENCES categories(id)
);

CREATE TRIGGER update_categories_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TABLE tags (
                      id SERIAL PRIMARY KEY,
                      name VARCHAR(100) NOT NULL,
                      slug VARCHAR(100) NOT NULL,
                      description TEXT,
                      thumbnail_image VARCHAR(255),
                      featured_image VARCHAR(255),
                      deleted BOOLEAN DEFAULT FALSE,
                      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                      updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TRIGGER update_tags_updated_at
    BEFORE UPDATE ON tags
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TABLE posts (
                       id SERIAL PRIMARY KEY,
                       title VARCHAR(255) NOT NULL,
                       slug VARCHAR(255) NOT NULL,
                       content TEXT  NULL,
                       category_id INT,
                       thumbnail_image VARCHAR(255),
                       featured_image VARCHAR(255),
                       published_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                       deleted BOOLEAN DEFAULT FALSE,
                       created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                       updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                       FOREIGN KEY (category_id) REFERENCES categories(id)
);

CREATE TRIGGER update_posts_updated_at
    BEFORE UPDATE ON posts
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TABLE post_tags (
                           id SERIAL PRIMARY KEY,
                           post_id INT,
                           tag_id INT,
                           deleted BOOLEAN DEFAULT FALSE,
                           created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                           updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                           FOREIGN KEY (post_id) REFERENCES posts(id),
                           FOREIGN KEY (tag_id) REFERENCES tags(id)
);

CREATE TRIGGER update_post_tags_updated_at
    BEFORE UPDATE ON post_tags
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TABLE post_authors (
                              id SERIAL PRIMARY KEY,
                              post_id INT,
                              author_id INT,
                              deleted BOOLEAN DEFAULT FALSE,
                              created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                              updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                              FOREIGN KEY (post_id) REFERENCES posts(id),
                              FOREIGN KEY (author_id) REFERENCES authors(id)
);

CREATE TRIGGER update_post_authors_updated_at
    BEFORE UPDATE ON post_authors
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();
