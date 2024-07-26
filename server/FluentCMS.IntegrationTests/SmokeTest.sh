test_sqlite(){
  local db_name=$1
  export DatabaseProvider=Sqlite
  export Sqlite="Data Source=${db_name}"
  dotnet test 
}

test_postgres_container() {
  local container_name="fluent-cms-db-postgres"
  local init_sql_path=$1
  # Remove existing container if it exists
  if docker ps -a --format '{{.Names}}' | grep -q "^${container_name}$"; then
    echo "Removing existing container: ${container_name}"
    docker rm -f "${container_name}"
  fi

  local docker_run_command="docker run -d --name $container_name -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_DB=fluent-cms -p 5432:5432"
  
  # Add volume mapping if init_sql_path is not empty
  if [ -n "$init_sql_path" ]; then
    docker_run_command+=" -v $init_sql_path:/docker-entrypoint-initdb.d/init.sql"
  fi
  docker_run_command+=" postgres:latest"
  
  eval $docker_run_command
  export DatabaseProvider=Postgres
  export Postgres="Host=localhost;Database=fluent-cms;Username=postgres;Password=mysecretpassword"
  dotnet test
}

# Exit immediately if a command exits with a non-zero status
set -e
#export Logging__LogLevel__Default=Warning
export Logging__LogLevel__Microsoft_AspNetCore=Warning

# Sqlite With Default Data 
#test_sqlite "cms.db"

# Sqlite With Empty Data 
#db_path=$(pwd)/temp.db && rm -f "$db_path" && test_sqlite "$db_path"

# Postgres With Default Data
test_postgres_container "$(pwd)/../../fluent-cms-postgres-docker/init.sql"

# Postgres With Empty Data 
#test_postgres_container ""

