## Performance Test : Fluent CMS  vs. An Asp.net core/entity framework core example 

With the question, since entity framework is so convenient, was it worth it to use CMS at all.   
Fluent CMS' performance-critical APIs are using SqlData/Dapper instead of Entity Framework.

To compare performance, I developed a small application called BlogEfExample using Entity Framework. [BlogEfExample](..%2F..%2Fserver%2FBlogEfExample) 

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
  - Fluent CMS  Schema: [postgres.sql](..%2F..%2Fserver%2Fexample-schema%2Fpostgres.sql)
  - Indexes: Add an index on posts.published_time. 
- Populated 1m posts, 100 category, 10k authors [insert-data-postgres.sql](..%2F..%2Fserver%2Fexample-schema%2Finsert-data-postgres.sql)
- Fluent CMS provides an latest endpoint.
  - supports pagination, each request retrieve 10 posts, then request the next 10 pages.
  - contains related entities authors and category.
- I use Dotnet Entity Framework and mini API, write a similar API  [Program.cs](..%2F..%2Fserver%2FBlogEfExample%2FProgram.cs)
### 3. **Test Tools**
- **k6**: Modern load testing tool built for developers.
### 4. **Test Execution**
**Virtual Users**: 1000
- **Duration**: 5 minutes
- **Test Scripts**: 
  - Fluent CMS [fluent-cms-latest-posts.js](..%2F..%2Fserver%2Fk6_test_scripts%2Ffluent-cms-latest-posts.js)
  - Entity Framework Example [ef-posts.js](..%2F..%2Fserver%2Fk6_test_scripts%2Fef-posts.js)
### 5. **Data Collection**
- Fluent CMS
```
     http_req_duration..............: avg=581.02ms min=6.5ms    med=575.23ms max=1.38s  p(90)=926.85ms p(95)=965.43ms
     http_reqs......................: 192687  1024.561066/s

```
- Entity Framework Example 
```
     http_req_duration..............: avg=232.81ms min=3.13ms  med=227.4ms  max=801.73ms p(90)=361.15ms p(95)=397.25ms
     http_reqs......................: 469557  2546.973785/s
```
### 6. **Analysis and Reporting**

- **Compare Metrics**:
    - Response Times: EF example is twice faster than Fluent CMS
    - Throughput:  EF example can handle twice requests
- **Identify Bottlenecks**:
    -  Fluent CMS need to read schema and build query, there are space to optimize