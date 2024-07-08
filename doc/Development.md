
## System Overviews
![overview.png](diagrams%2Foverview.png)
- Web Server: [FluentCMS](..%2Fserver%2FFluentCMS)
- Admin Panel Client: [admin-ui](..%2Fadmin-ui)
- Schema Builder: [schema-ui](..%2Fserver%2FFluentCMS%2Fwwwroot%2Fschema-ui)
- Demo Publish Site: [ui](..%2Fui)
## Server
- Startup project:  [FluentCMS](..%2Fserver%2FFluentCMS)
- Tools
  - Asp.net core
  - Entity framework core
  - SqlKata(https://sqlkata.com/), it using Dapper(https://www.learndapper.com/) behind the scene.

The design of the API Server offers several advantages, primarily revolving around modularity, scalability, maintainability, and separation of concerns. 

### Controllers and Services 
![api-controller-service.png](diagrams%2Fapi-controller-service.png)  
The diagram illustrates the architecture of an API server with the following components:

1. **External Clients**:
  - `Schema Builder`: jQuery-based interface for schema management.
  - `Admin Panel`: React-based interface for administrative tasks.
  - `Public Site`: Next.js-based interface for public-facing interactions.

2. **Controllers**:
  - `Schema Controller`: Manages schema CRUD operations.
  - `Entity Controller`: Manages entity CRUD operations.
  - `View Controller`: Provides APIs for public site interactions.

3. **Services**:
  - `Schema Service`: Handles business logic for schema CRUD operations.
  - `Entity Service`: Handles business logic for entity CRUD operations.
  - `View Service`: Handles business logic for public site APIs.

4. **Database**:
  - Uses Database for storing schema and content.

### Purpose of This Design

1. **Modularity**:
  - Each component (Schema Builder, Admin Panel, Public Site) operates independently, allowing for focused development and easier debugging.
  - Controllers and services are separated, ensuring that changes in one area do not directly affect others.

2. **Maintainability**:
  - Changes in one module (e.g., adding a new feature to the Admin Panel) do not necessitate changes in others (e.g., the Public Site).
  - Well-defined interfaces between components (e.g., HTTP/JSON) make it easier to update and extend the system.

3. **Reusability**:
  - The same services can be used by different controllers, promoting code reuse. For instance, both the Admin Panel and Schema Builder use the `Schema Controller`.

4. **Database Interaction**:
  - The use of ORM (Entity Framework for `Schema Service`) and direct SQL libraries (SqlKate for `Entity Service` and `View Service`) provides flexibility in database interaction methods, optimizing for both simplicity and performance.

### Detailed Interaction Flow

1. **Schema Management**:
  - The `Schema Builder` and `Admin Panel` send requests to the `Schema Controller` for schema management.
  - The `Schema Controller` delegates these requests to the `Schema Service`, which interacts with the database using Entity Framework.

2. **Entity Management**:
  - The `Admin Panel` sends entity management requests to the `Entity Controller`.
  - The `Entity Controller` forwards these requests to the `Entity Service`, which interacts with the database using SqlKate.

3. **Public Site Interactions**:
  - The `Public Site` sends requests to the `View Controller`.
  - The `View Controller` forwards these requests to the `View Service`, which also interacts with the database using SqlKate.


### Query Builder 
The Query Builder pattern offers several advantages, which can be illustrated using the given UML diagram of the `/api/entities/[entityName]? [pagination] [filter] [sort]` endpoint. Here's a detailed explanation:

#### UML Diagram Overview

The UML diagram describes the interaction between various components for handling an entity-based query with pagination, filtering, and sorting:
![api-query-sequence.png](diagrams%2Fapi-query-sequence.png)
1. **Entity Controller**: Handles incoming API requests.
2. **Entity Service**: Manages the business logic for entities.
3. **Schema Service**: Provides schema definitions for entities.
4. **Query Builder**: Composed of `Entity`, `Filters`, `Sorts`, and `Pagination`.
5. **Executor**: Executes the query against the database.
6. **Database**: Stores the schema and content.

#### Advantages of Query Builder

1. **Abstraction and Reusability**:
  - The `Query Builder` abstracts the complexities of query creation. Components like `Filters`, `Sorts`, and `Pagination` can be reused across different parts of the application, promoting DRY (Don't Repeat Yourself) principles.

2. **Dynamic Query Generation**:
  - With the `Query Builder`, queries are dynamically generated based on the schema definitions provided by the `Schema Service`. This allows for flexible and adaptable query creation without hardcoding.

3. **Simplified API Design**:
  - The endpoint `/api/entities/[entityName]? [pagination] [filter] [sort]` becomes more versatile. Users can retrieve data with varying conditions by simply adjusting URL parameters.

4. **Separation of Concerns**:
  - The architecture separates concerns effectively. The `Entity Controller` handles HTTP requests, `Entity Service` manages business logic, and the `Query Builder` constructs queries. This separation enhances maintainability and scalability.

5. **Performance Optimization**:
  - Pagination and filtering reduce the amount of data processed and transferred, improving performance. The `Query Builder` ensures that only the necessary data is retrieved from the database.

6. **Maintainability and Extensibility**:
  - Adding new filters, sorts, or pagination logic can be done by extending the respective components without altering the core functionality. This modularity simplifies maintenance and future enhancements.

#### Detailed Interaction Flow

1. **Request Handling**:
  - The `Entity Controller` receives a request with pagination, filters, and sorts parameters.

2. **Component Initialization**:
  - The controller initializes `Pagination`, `Filters`, and `Sorts` components based on URL parameters.

3. **Service Interaction**:
  - The controller invokes the `Entity Service` to list entities with the provided parameters.

4. **Schema Retrieval**:
  - The `Entity Service` queries the `Schema Service` to get the entity schema.

5. **Query Building**:
  - The `Entity Service` uses the schema to build a query. It applies pagination, filters, and sorts through the respective components in the `Query Builder`.

6. **Query Execution**:
  - The built query (KateQuery) is passed to the `Executor`, which interacts with the `Database` to fetch the results.

7. **Result Aggregation**:
  - The `Entity Service` counts the total number of filtered entities and returns the items and total count to the `Entity Controller`.

8. **Response**:
  - The `Entity Controller` sends the response to the client with the requested data and pagination details.
### Databases
- Fluent CMS supports PostgreSQL and SQLite:
  - SQLite: Simplifies the setup of a development environment. Users can clone the repository and start the server with zero configuration.
  - PostgreSQL: Facilitates performance comparison with other CMSs.
- **Supporting More Databases:**
  - Entity Framework and SqlKata provide an abstraction layer over specific database drivers.
  - To support a new database, implementations of the following interfaces are required
    - [IDefinitionExecutor.cs](..%2Fserver%2FUtils%2FDataDefinitionExecutor%2FIDefinitionExecutor.cs)
    - [IKateProvider.cs](..%2Fserver%2FUtils%2FKateQueryExecutor%2FIKateProvider.cs)
  - Upcoming Support: Plans to add support for MS SQL Server and MySQL/MariaDB.