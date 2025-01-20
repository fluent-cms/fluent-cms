## Performance Test : Fluent CMS  vs. An Asp.net core/entity framework core example 

With the question, since entity framework is so convenient, was it worth it to use CMS at all.   
Fluent CMS' performance-critical APIs are using SqlData/Dapper instead of Entity Framework.

To compare performance, I developed a small application called BlogEfExample using Entity Framework.[BlogEfExample](..%2F..%2Fexamples%2FBlogEfExample) 

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
  - Fluent CMS  Schema: 
  - Indexes: Add an index on posts.published_time. 
- Populated 1m posts, 100 category, 10k authors 
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
  - Fluent CMS [fluent-cms-posts.js](..%2F..%2Fperformance_tests%2Ffluent-cms-posts.js)
  - Entity Framework Example [ef-posts.js](..%2F..%2Fperformance_tests%2Fef-posts.js)
### 5. **Data Collection**
- Fluent CMS
```
     http_req_duration..............: avg=162.84ms min=1.29ms  med=174.66ms max=661.31ms p(90)=256.11ms p(95)=276.89ms
     http_reqs......................: 653279  3557.419406/s
```
- Entity Framework Example 
```
     http_req_duration..............: avg=204.63ms min=2.06ms med=207.03ms max=652.39ms p(90)=323.09ms p(95)=349.17ms
     http_reqs......................: 531784  2890.509673/s
```
### 6. **Analysis and Reporting**

- **Compare Metrics**:
    - Response Times: EF example is 10% than Fluent CMS
    - Throughput:  EF example can handle less requests
- **Identify Bottlenecks**:
    - Fluent CMS is using SqlData for performance critical query, behind the scene, it use  Dapper ORM. 
    - According Dapper's document, dapper's performance is better https://www.learndapper.com/#when-should-you-use-dapper 