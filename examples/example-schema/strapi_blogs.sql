create table public.authors
(
    id              serial
        primary key,
    name            varchar(255),
    slug            varchar(255),
    bio             text,
    thumbnail_image varchar(255),
    featured_image  varchar(255),
    created_at      timestamp(6),
    updated_at      timestamp(6),
    created_by_id   integer
        constraint authors_created_by_id_fk
        references public.admin_users
        on delete set null,
    updated_by_id   integer
        constraint authors_updated_by_id_fk
        references public.admin_users
        on delete set null,
    published_time  timestamp(6)
);

alter table public.authors
    owner to postgres;

create index authors_created_by_id_fk
    on public.authors (created_by_id);

create index authors_updated_by_id_fk
    on public.authors (updated_by_id);

create table public.categories
(
    id              serial
        primary key,
    name            varchar(255),
    slug            varchar(255),
    description     text,
    thumbnail_image varchar(255),
    featured_image  varchar(255),
    created_at      timestamp(6),
    updated_at      timestamp(6),
    created_by_id   integer
        constraint categories_created_by_id_fk
        references public.admin_users
        on delete set null,
    updated_by_id   integer
        constraint categories_updated_by_id_fk
        references public.admin_users
        on delete set null
);

alter table public.categories
    owner to postgres;

create index categories_created_by_id_fk
    on public.categories (created_by_id);

create index categories_updated_by_id_fk
    on public.categories (updated_by_id);

create table public.posts
(
    id              serial
        primary key,
    title           varchar(255),
    slug            varchar(255),
    reading_time    varchar(255),
    excerpt         text,
    content         text,
    thumbnail_image varchar(255),
    featured_image  varchar(255),
    created_at      timestamp(6),
    updated_at      timestamp(6),
    created_by_id   integer
        constraint posts_created_by_id_fk
        references public.admin_users
        on delete set null,
    updated_by_id   integer
        constraint posts_updated_by_id_fk
        references public.admin_users
        on delete set null,
    published_time  timestamp(6)
);

alter table public.posts
    owner to postgres;

create index posts_created_by_id_fk
    on public.posts (created_by_id);

create index posts_updated_by_id_fk
    on public.posts (updated_by_id);

create index posts_published_time_index
    on public.posts (published_time desc);

create table public.posts_authors_links
(
    id           serial
        primary key,
    post_id      integer
        constraint posts_authors_links_fk
        references public.posts
        on delete cascade,
    author_id    integer
        constraint posts_authors_links_inv_fk
        references public.authors
        on delete cascade,
    author_order double precision,
    post_order   double precision,
    constraint posts_authors_links_unique
        unique (post_id, author_id)
);

alter table public.posts_authors_links
    owner to postgres;

create index posts_authors_links_fk
    on public.posts_authors_links (post_id);

create index posts_authors_links_inv_fk
    on public.posts_authors_links (author_id);

create index posts_authors_links_order_fk
    on public.posts_authors_links (author_order);

create index posts_authors_links_order_inv_fk
    on public.posts_authors_links (post_order);

create table public.posts_category_links
(
    id          serial
        primary key,
    post_id     integer
        constraint posts_category_links_fk
        references public.posts
        on delete cascade,
    category_id integer
        constraint posts_category_links_inv_fk
        references public.categories
        on delete cascade,
    post_order  double precision,
    constraint posts_category_links_unique
        unique (post_id, category_id)
);

alter table public.posts_category_links
    owner to postgres;

create index posts_category_links_fk
    on public.posts_category_links (post_id);

create index posts_category_links_inv_fk
    on public.posts_category_links (category_id);

create index posts_category_links_order_inv_fk
    on public.posts_category_links (post_order);

