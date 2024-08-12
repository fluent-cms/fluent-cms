# rdbms vs nosql comparision - Postgres vs Mongo DB  

## NoSql saves the trouble of join
This post if following up my last post [Query Performance](https://github.com/fluent-cms/fluent-cms/blob/main/doc/wiki/query-performance.md)
The relationships between entities is as follow, author and posts has many to many relationships,  people can co-author posts.
![entity-relation.png](https://github.com/fluent-cms/fluent-cms/blob/main/doc/wiki/entity-relation.png)
If we are lucky, all filter fields, sort fields are  in one table, then the performance of our query is not bad.
But in reality, we can not avoid the requirements of composing a filter from more than entities. 
After we join two big table, the performance is bad.  

While in NoSql database such as MongoDB, with proper indexes and embed multiple entity to one document, the query performance is manageable even with huge data 
from multiple entities.

I also did a test, I migrate the composed data {post, category, authors) to mongo db, then compared the query performance.
PostgresQuery
```
fluent-cms> select * from posts 
                       left join author_post ap on posts.id = ap.post_id
                       left join authors a on a.id = ap.author_id
                       left join categories c on posts.category_id = c.id
                   where
                       posts.published_at < '2024-09-02' and 
                       a.slug like 'author-1%' and 
                       c.slug like 'category-9%'
                   order by posts.published_at desc
                   limit 10
[2024-08-12 08:36:52] 10 rows retrieved starting from 1 in 15 s 510 ms (execution: 15 s 465 ms, fetching: 45 ms)

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
MongoDb achieves high query performance by put related entities together, but that makes ensure data integrity difficult.
An examples is change an author's name, with traditional RDBMS, we can simply update author's name by it's id. 
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