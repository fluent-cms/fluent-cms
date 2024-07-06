# How to export current data to init.sql

1. dump in Postgres docker
    ```shell
    pg_dump -U postgres -d fluent-cms -F p -f /init.sql
    ```
2. copy init.sql to host  
    ```
   docker cp fluent-cms-db-postgres:/init.sql .
   ```