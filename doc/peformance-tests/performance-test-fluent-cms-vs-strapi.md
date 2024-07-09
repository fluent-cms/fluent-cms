## Performance Test : Fluent CMS vs. Strapi

With Strapi declare it's speed advantage against wordpress. I setup 
an blogs/post api to compare their performance.

### 1. **Environment Setup**

- **Infrastructure**:
    - Hardware specifications:
        - CPU: Apple M1 Pro 10-core
        - RAM: 16 GB
        - Storage: SSD

- **Software**:
    - Postgres latest version(16.3), run on Docker
    - Node 20.8 (for strapi)
    - Dotnet 8.0(for Fluent CMS), run on Docker
    - Strapi Latest Version (4.14)

### 2. **Test Scenarios**
- Entities: Setup an example blogs website, with 3 entities:
  - Authors
  - Categories
  - Post 
  - Relations: Each post can have multiple authors, and belongs to one category
  - Fluent CMS Schema: [postgres.sql](..%2F..%2Fserver%2Fexample-schema%2Fpostgres.sql)
  - Strapi Schema: [strapi_blogs.sql](..%2F..%2Fserver%2Fexample-schema%2Fstrapi_blogs.sql)
  - Indexes: Add an index on posts.published_time for both Fluent CMS and Strapi schemas.
- Populated 1m posts, 100 category, 10k authors [insert-data-postgres.sql](..%2F..%2Fserver%2Fexample-schema%2Finsert-data-postgres.sql)
- Both Fluent CMS and Strapi need to provides an latest endpoint.
  - supports pagination, each request retrieve 10 posts, then request the next 10 pages.
  - contains related entities authors and category.

### 3. **Test Tools**
- **k6**: Modern load testing tool built for developers.
### 4. **Test Execution**
**Virtual Users**: 1000
- **Duration**: 5 minutes
- **Test Scripts**: 
  - Fluent CMS [fluent-cms-latest-posts.js](..%2F..%2Fserver%2Fk6_test_scripts%2Ffluent-cms-latest-posts.js)
  - Strapi [strapi-posts.js](..%2F..%2Fserver%2Fk6_test_scripts%2Fstrapi-posts.js)
### 5. **Data Collection**
- Fluent CMS
```
     http_req_duration..............: avg=581.02ms min=6.5ms    med=575.23ms max=1.38s  p(90)=926.85ms p(95)=965.43ms
     http_reqs......................: 192687  1024.561066/s

```
- Strapi 
```
     http_req_duration..............: avg=29.1s    min=209.11ms med=29.82s max=51.82s  p(90)=46.99s  p(95)=49.32s  
     http_reqs......................: 3807    17.952644/s
```
### 6. **Analysis and Reporting**

- **Compare Metrics**:
    - Response Times: Fluent CMS is 50 times faster than Strapi.
    - Throughput: Fluent CMS can handle 57 times more requests per second compared to Strapi.
- **Identify Bottlenecks**:
    - Node.js can only utilize one CPU, while .NET can utilize the full potential of the CPU.
    - .NET code runs faster than Node.js.
    - Fluent CMS prioritizes performance, with optimized queries.