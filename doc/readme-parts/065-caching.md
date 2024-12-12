
---
## Optimizing Caching
<details>
<summary>
To enhance performance, Fluent CMS implements caching strategies. 
</summary>

### Cache Types

1. **Entity Definition Cache**  
   Fluent CMS requires caching of all entity definitions to dynamically generate GraphQL types.

2. **Query Definition Cache**  
   Each query may depend on multiple related entities. Fluent CMS caches these definitions to compose efficient SQL queries.

---

### IMemoryCache in Fluent CMS

By default, Fluent CMS utilizes ASP.NET's `IMemoryCache` for caching.

- **Advantages**:
    - Simple to debug and deploy.
    - Suitable for single-node web applications.

- **Disadvantages**:
    - Not scalable for distributed environments. In multi-node deployments, cache invalidation on one node (e.g., Node A) does not propagate to other nodes (e.g., Node B).

---

### HybridCache for Scalable Caching

Starting with ASP.NET 9.0, the framework provides `HybridCache`, which combines a primary memory cache with a secondary external cache (e.g., Redis).

- **Key Features**:
    - **Scalability**: Combines the performance of local memory caching with the distributed consistency of external caching.
    - **Stampede Resolution**: The `HybridCache` resolves cache stampede issues, as confirmed by its developers.

- **Limitations**:  
  The current implementation lacks "Backend-Assisted Local Cache Invalidation," which means cache invalidation on one node does not immediately propagate to others.

- **Fluent CMS Strategy**:  
  To address this, Fluent CMS sets local cache expiration (20 seconds) to one-third of the distributed cache expiration (60 seconds). This ensures memory caches across nodes achieve consistency within 20 seconds, significantly improving over a standard memory cache's 60-second delay.

</details>
