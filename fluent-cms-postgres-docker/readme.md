# How to export current data to init.sql
1. docker shell
   ```shell
   docker exec -it fluent-cms-db-postgres bash
   ```
2. dump in Postgres docker
    ```shell
    pg_dump -U postgres -d fluent-cms -F p -f /init.sql
    ```
3. copy init.sql to host  
    ```
   docker cp fluent-cms-db-postgres:/init.sql .
   ```