

## Online Course System Backend
<details> 
<summary> 
This chapter will guide you through developing a simple online course system, with entities `teacher`, `course`, `skill`, `material`.
</summary>

### Database Schema
#### 1. **Teachers Table**

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `firstname`      | Firstname        | String        |
| `lastname`       | Lastname         | String        |
| `email`          | Email            | String        |
| `phone_number`   | Phone Number     | String        |
| `image`          | Image            | String        |
| `bio`            | Bio              | Text          |


#### 2. **Courses Table**

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Course Name      | String        |
| `status`         | Status           | String        |
| `level`          | Level            | String        |
| `summary`        | Summary          | String        |
| `image`          | Image            | String        |
| `desc`           | Description      | Text          |
| `duration`       | Duration         | String        |
| `start_date`     | Start Date       | Datetime      |

#### 3. **Skills Table**
This table stores skills teachers possess. 

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Name             | String        |
| `years`          | Years            | Int           |
| `created_by`     | Created By       | String        |
| `created_at`     | Created At       | Datetime      |
| `updated_at`     | Updated At       | Datetime      |

#### 4. **Materials Table**
This table stores the materials associate with courses.

| **Field**        | **Header**  | **Data Type** |
|-------------------|-------------|---------------|
| `id`             | ID          | Int           |
| `name`           | Name        | String        |
| `type`           | Type        | String        |
| `image`          | Image       | String        |
| `link`           | Link        | String        |
| `file`           | File        | String        |

#### Relationships:
- **Teachers to Courses**: One-to-Many (A teacher can teach multiple courses, but a course is taught by only one teacher).
- **Teachers to Skills**: Many-to-Many (A teacher has multiple skills, and multiple teacher can share same skills).
- **Course to Material**: Many-to-Many (A course has multiple materials, and multiple course can share same materials).
### Build Schema using Fluent CMS Schema builder

After launching your ASP.NET Core application, locate the "Schema Builder" menu on the application's home page.

1. Navigate to the **Entities** menu within the Schema Builder.
2. Add entities such as "Teacher" and "Course."
3. When creating the "Course" entity:
   - Add basic attributes like `name`, `status`, `level`, and `description`.
   - Define relationships by adding attributes as shown below:
#### Example Attribute Definitions
**1. Teacher Attribute**

| **Attribute**    | **Value**     |
|-------------------|---------------|
| **Field**         | `teacher`    |
| **Header**        | Teacher      |
| **Data Type**     | Int          |
| **In List**       | true         |
| **In Detail**     | true         |
| **Is Default**    | false        |
| **Type**          | lookup       |
| **Options**       | teacher      |

**2. Materials Attribute**

 | **Attribute**    | **Value**   |
 |-------------------|-------------|
 | **Field**         | `materials` |
 | **Header**        | Materials   |
 | **In List**       | false       |
 | **In Detail**     | true        |
 | **Is Default**    | false       |
 | **Type**          | crosstable  |
 | **Options**       | material    |

### Managing data using Fluent CMS Admin Panel

After launching your ASP.NET Core application, locate the "Admin Panel" menu on the application's home page.

![img.png](img.png)
</details>
