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

CREATE TABLE posts (
                       id SERIAL PRIMARY KEY,
                       title VARCHAR(255) NOT NULL,
                       slug VARCHAR(255) NOT NULL,
                       reading_time varchar(20) not null ,
                       excerpt VARCHAR(1000) NOT NULL ,
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

create index posts_deleted_published_at_index
    on posts (deleted asc, published_at desc);

CREATE TABLE author_post (
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
    BEFORE UPDATE ON author_post
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

create index posts_deleted_index
    on posts (deleted);

create index posts_published_at_index
    on posts (published_at desc);

create index posts_category_id_index
    on posts (category_id);

update __schemas set settings = '{"Entity":null,"View":null,"Menu":{"Name":"top-menu-bar","MenuItems":[{"Icon":"pi-check","Label":"Posts","Url":"/entities/post","IsHref":false},{"Icon":"pi-bolt","Label":"Categories","Url":"/entities/category","IsHref":false},{"Icon":"pi-users","Label":"Authors","Url":"/entities/author","IsHref":false},{"Icon":"pi-cog","Label":"Schema Builder","Url":"/schema-ui/list.html","IsHref":true}]}}'
                 where name ='top-menu-bar';

INSERT INTO __schemas (name, type, settings) VALUES
    ('author', 'entity', '{"Entity":{"Name":"author","TableName":"authors","Title":"Author","PrimaryKey":"id","TitleAttribute":"name","DefaultPageSize":20,"Attributes":[{"DataType":"Int","Field":"id","Header":"Id","InList":true,"InDetail":true,"IsDefault":true,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"name","Header":"Name","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"slug","Header":"Slug","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"thumbnail_image","Header":"Thumbnail Image","InList":true,"InDetail":true,"IsDefault":false,"Type":"image","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"featured_image","Header":"Featured Image","InList":true,"InDetail":true,"IsDefault":false,"Type":"image","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"created_at","Header":"Created At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"updated_at","Header":"Updated At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"bio","Header":"Bio","InList":false,"InDetail":true,"IsDefault":false,"Type":"editor","Options":"","Crosstable":null,"Lookup":null}]},"View":null,"Menu":null}'),
    ('category', 'entity', '{"Entity":{"Name":"category","TableName":"categories","Title":"Categories","PrimaryKey":"id","TitleAttribute":"name","DefaultPageSize":20,"Attributes":[{"DataType":"Int","Field":"id","Header":"Id","InList":true,"InDetail":true,"IsDefault":true,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"name","Header":"Name","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Int","Field":"parent_category_id","Header":"Parent Category Id","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"featured_image","Header":"Featured Image","InList":true,"InDetail":true,"IsDefault":false,"Type":"image","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"thumbnail_image","Header":"Thumbnail Image","InList":true,"InDetail":true,"IsDefault":false,"Type":"image","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"description","Header":"Description","InList":false,"InDetail":true,"IsDefault":false,"Type":"editor","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"created_at","Header":"Created At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"updated_at","Header":"Updated At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"slug","Header":"Slug","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null}]},"View":null,"Menu":null}'),
    ('post', 'entity', '{"Entity":{"Name":"post","TableName":"posts","Title":"Posts","PrimaryKey":"id","TitleAttribute":"title","DefaultPageSize":0,"Attributes":[{"DataType":"Int","Field":"id","Header":"Id","InList":true,"InDetail":true,"IsDefault":true,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"title","Header":"Title","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Datetime","Field":"published_at","Header":"Published At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"slug","Header":"Slug","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"reading_time","Header":"Reading Time","InList":true,"InDetail":true,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Int","Field":"category_id","Header":"Category","InList":true,"InDetail":true,"IsDefault":false,"Type":"lookup","Options":"category","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"thumbnail_image","Header":"Thumbnail Image","InList":true,"InDetail":true,"IsDefault":false,"Type":"image","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"featured_image","Header":"Featured Image","InList":true,"InDetail":true,"IsDefault":false,"Type":"image","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"excerpt","Header":"excerpt","InList":false,"InDetail":true,"IsDefault":false,"Type":"textarea","Options":"","Crosstable":null,"Lookup":null},{"DataType":"String","Field":"authors","Header":"Authors","InList":false,"InDetail":true,"IsDefault":true,"Type":"crosstable","Options":"author","Crosstable":null,"Lookup":null},{"DataType":"Text","Field":"content","Header":"Content","InList":false,"InDetail":true,"IsDefault":false,"Type":"editor","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Datetime","Field":"created_at","Header":"Created At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null},{"DataType":"Datetime","Field":"updated_at","Header":"Updated At","InList":true,"InDetail":false,"IsDefault":false,"Type":"text","Options":"","Crosstable":null,"Lookup":null}]},"View":null,"Menu":null}'),
    ('latest-posts', 'view', '{"Entity":null,"View":{"Name":"latest-posts","AttributeNames":["id","title","category_id","thumbnail_image","published_at","authors","slug"],"EntityName":"post","PageSize":10,"Sorts":[{"FieldName":"published_at","Order":"Desc"}],"Filters":[]},"Menu":null}'),
    ('post-by-slug', 'view', '{"Entity":null,"View":{"Name":"post-by-slug","AttributeNames":null,"EntityName":"post","PageSize":0,"Sorts":[],"Filters":[{"FieldName":"slug","Operator":"and","Constraints":[{"Match":"in","Value":"querystring.slug","ResolvedValues":null}]}]},"Menu":null}'),
    ('latest-post-slugs', 'view', '{"Entity":null,"View":{"Name":"latest-post-slugs","AttributeNames":["id","slug","published_at"],"EntityName":"post","PageSize":1000,"Sorts":[{"FieldName":"published_at","Order":"Desc"}],"Filters":[]},"Menu":null}')