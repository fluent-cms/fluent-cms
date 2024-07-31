## Performance Test : Fluent CMS  vs. An Asp.net core/entity framework core example 

With the question, since entity framework is so convenient, was it worth it to use CMS at all.   
Fluent CMS' performance-critical APIs are using SqlData/Dapper instead of Entity Framework.

To compare performance, I developed a small application called BlogEfExample using Entity Framework. [BlogEfExample](..%2F..%2Fexamples%2FBlogEfExample)

### 1. **Environment Setup**
- **Infrastructure**:
    - Hardware specifications:
        - CPU: Apple M1 Pro 10-core
        - RAM: 16 GB
        - Storage: SSD

- **Software**:
    - Postgres latest version(16.3), run on Docker
    - Dotnet 8.0, run on Docker

### 2. **Test Scenarios**
- Entities: Setup an example blogs website with 3 entities:
  - Authors
  - Categories
  - Post 
  - Relations: Each post can have multiple authors, and belongs to one category
  - Fluent CMS  Schema: [postgres.sql](..%2F..%2Fexamples%2Fexample-schema%2Fpostgres.sql)
  - Indexes: Add an index on posts.published_time. 
- Populated 1m posts, 100 category, 10k authors [insert-data-postgres.sql](..%2F..%2Fexamples%2Fexample-schema%2Finsert-data-postgres.sql)
- Fluent CMS provides an latest endpoint.
  - supports pagination, each request retrieve 10 posts, then request the next 10 pages.
  - contains related entities authors and category.
- I use Dotnet Entity Framework and mini API, write a similar API 
### 3. **Test Tools**
- **k6**: Modern load testing tool built for developers.
### 4. **Test Execution**
**Virtual Users**: 1000
- **Duration**: 5 minutes
- **Test Scripts**: 
  - Fluent CMS [fluent-cms-latest-posts.js](..%2F..%2Fk6_test_scripts%2Ffluent-cms-latest-posts.js)
  - Entity Framework Example [ef-posts.js](..%2F..%2Fk6_test_scripts%2Fef-posts.js)
### 5. **Data Collection**
- Fluent CMS
```
     http_req_duration..............: avg=196.85ms min=2.64ms  med=204.43ms max=775.83ms p(90)=298.72ms p(95)=315ms
     http_reqs......................: 552783  3008.503605/s
```
- Entity Framework Example 
```
     http_req_duration..............: avg=223.19ms min=2.53ms  med=220.84ms max=771.67ms p(90)=342.61ms p(95)=373.5ms
     http_reqs......................: 488356  2653.795461/s
```
### 6. **Analysis and Reporting**

- **Compare Metrics**:
    - Response Times: EF example is 10% than Fluent CMS
    - Throughput:  EF example can handle less requests
- **Identify Bottlenecks**:
    - Fluent CMS is using SqlData for performance critical query, behind the scene, it use  Dapper ORM. 
    - According Dapper's document, dapper's performance is better https://www.learndapper.com/#when-should-you-use-dapper 