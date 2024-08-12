# rdbms vs nosql comparision - Postgres vs Mongo DB  

## NoSql saves the trouble of join
This post if following up my last post [Query Performance](https://github.com/fluent-cms/fluent-cms/blob/main/doc/wiki/query-performance.md)
The relationships between entities is as follow, author and posts has many to many relationships,  people can co-author posts.  
![entity-relation.png](https://github.com/fluent-cms/fluent-cms/blob/main/doc/wiki/entity-relation.png)   
If we are lucky, all filter fields, sort fields are  in one table, then the performance of our query is not bad.
But in reality, we can not avoid the requirements of composing a filter from more than one entity. 
After we join two big tables, the performance goes bad.  

While in NoSql database such as MongoDB, with proper indexes and embedding multiple entity to one document, the query performance is manageable even with huge data 
from multiple entities.

I did a test, I migrate the composed data {post, category, authors) to mongo db, 
```json
{
"id": 1999999,
"title": "Post Title 1999999",
"published_at": "2024-09-02T15:56:28.246573",
"slug": "post-title-1999999",
"category_id": 63789,
"thumbnail_image": "http://example.com/thumbnail/1999999",
"category_id_data": {
  "id": 63789,
  "name": "Category 63789",
  "parent_category_id": 9,
  "featured_image": "http://example.com/featured/63789",
  "thumbnail_image": "http://example.com/thumbnail/63789",
  "created_at": "2024-08-10T12:22:54.867217",
  "updated_at": "2024-08-11T10:25:51.059176",
  "slug": "category-63789"
},
"authors": [
  {
    "id": 4888670,
    "name": "Author 4888670",
    "slug": "author-4888670",
    "thumbnail_image": "http://example.com/thumbnail/4888670",
    "featured_image": "http://example.com/featured/4888670",
    "created_at": "2024-08-10T12:22:08.80861",
    "updated_at": "2024-08-10T12:22:08.80861",
    "post_id": 1999999
  },
  {
    "id": 1963811,
    "name": "Author 1963811",
    "slug": "author-1963811",
    "thumbnail_image": "http://example.com/thumbnail/1963811",
    "featured_image": "http://example.com/featured/1963811",
    "created_at": "2024-08-10T12:22:08.80861",
    "updated_at": "2024-08-10T12:22:08.80861",
    "post_id": 1999999
  }]
}
```
then compared the query performance. The different is hug. RDBMS's performance is not acceptable.
PostgresQuery(I have added index on `posts.published_at desc`, `authors.slug`, `categories.slug`)
```
fluent-cms>  select * from posts 
                       join author_post ap on posts.id = ap.post_id
                       join authors a on a.id = ap.author_id
                       join categories c on posts.category_id = c.id
                   where
                       posts.published_at < '2024-09-02' and 
                       a.slug like 'author-1%' and 
                       c.slug like 'category-9%'
                   order by posts.published_at desc
                   limit 10
[2024-08-12 13:31:50] 10 rows retrieved starting from 1 in 16 s 846 ms (execution: 16 s 651 ms, fetching: 195 ms)

```

MongoDB query
```
cms> db.posts.find({
         "authors.slug": { $gte: "author-1", $lt: "author-2" },
         "category_id_data.slug": { $gte: "category-9", $lt: "category-:" },
         "published_at": { $lt: '2024-09-02' }
     }).sort({ "published_at": -1 })
     .limit(10)
[2024-08-12 08:27:26] 10 rows retrieved starting from 1 in 177 ms (execution: 148 ms, fetching: 29 ms)
```
## Ensuring data integrity
MongoDb achieves high query performance by put related entities together as single document, but that makes ensuring data integrity difficult.
An examples is changing an author's name, with traditional RDBMS, we can simply update author's name by it's id. 
But with Mongo DB's composited entity structure, update author's name in every document takes too much efforts.

## Combining RDBMS with NoSQL
1. **Data Integrity in RDBMS**:
    - **Strong Consistency**: Use an RDBMS to enforce strong data integrity constraints, such as foreign keys, unique constraints, and complex transactions.
    - **Data Validation**: Ensure that all data is valid and adheres to the necessary schema and business rules.

2. **Performance and Scalability in NoSQL**:
    - **High-Performance Reads**: After ensuring data integrity in the RDBMS, replicate or synchronize the data to MongoDB for use cases requiring high-performance reads, flexible schema, or horizontal scalability.
    - **Document-Oriented Queries**: Use MongoDB’s document model for queries that benefit from denormalized data structures, reducing the need for complex joins and increasing query performance.

### Data Flow and Integration
1. Transaction in RDBMS: A transaction is performed in the RDBMS, then the Application add and event to Kafka or RabbitMQ
2. MongoDB consumer: The MongoDB consumer then save data to mongo db
3. APIs server heavy read based on Mongo
