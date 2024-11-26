

## Online Course System Backend

<details> 
<summary> 
This section provides detailed guidance on developing a foundational online course system, encompassing key entities: `teacher`, `course`, `skill`, and `material`.
</summary>

### Database Schema

#### 1. **Teachers Table**
The `Teachers` table maintains information about instructors, including their personal and professional details.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `firstname`      | First Name       | String        |
| `lastname`       | Last Name        | String        |
| `email`          | Email            | String        |
| `phone_number`   | Phone Number     | String        |
| `image`          | Image            | String        |
| `bio`            | Bio              | Text          |

#### 2. **Courses Table**
The `Courses` table captures the details of educational offerings, including their scope, duration, and prerequisites.

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
The `Skills` table records competencies attributed to teachers.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Skill Name       | String        |
| `years`          | Years of Experience | Int      |
| `created_by`     | Created By       | String        |
| `created_at`     | Created At       | Datetime      |
| `updated_at`     | Updated At       | Datetime      |

#### 4. **Materials Table**
The `Materials` table inventories resources linked to courses.

| **Field**        | **Header**  | **Data Type** |
|-------------------|-------------|---------------|
| `id`             | ID          | Int           |
| `name`           | Name        | String        |
| `type`           | Type        | String        |
| `image`          | Image       | String        |
| `link`           | Link        | String        |
| `file`           | File        | String        |

### Relationships
- **Teachers to Courses**: One-to-Many (Each teacher can teach multiple courses; each course is assigned to one teacher).
- **Teachers to Skills**: Many-to-Many (Multiple teachers can share skills, and one teacher may have multiple skills).
- **Courses to Materials**: Many-to-Many (A course may include multiple materials, and the same material can be used in different courses).

---

### Schema Creation via Fluent CMS Schema Builder

#### Accessing Schema Builder
After launching the web application, locate the **Schema Builder** menu on the homepage to start defining your schema.

#### Adding Entities
1. Navigate to the **Entities** section of the Schema Builder.
2. Create entities such as "Teacher" and "Course."
3. For the `Course` entity:
    - Add attributes such as `name`, `status`, `level`, and `description`.
    - Define relationships using specific attribute types.

#### Example Attribute Definitions
1. **Lookup Attribute**
    - Represents a many-to-one relationship (e.g., `Course` to `Teacher`).

| **Attribute**    | **Value**     |
|-------------------|---------------|
| **Field**         | `teacher`    |
| **Header**        | Teacher      |
| **Data Type**     | Int          |
| **In List**       | True         |
| **In Detail**     | True         |
| **Is Default**    | False        |
| **Type**          | Lookup       |
| **Options**       | Teacher      |

2. **Crosstable Attribute**
    - Defines a many-to-many relationship (e.g., `Course` to `Material`).

| **Attribute**    | **Value**     |
|-------------------|---------------|
| **Field**         | `materials`  |
| **Header**        | Materials    |
| **In List**       | False        |
| **In Detail**     | True         |
| **Is Default**    | False        |
| **Type**          | Crosstable   |
| **Options**       | Material     |

---

### Admin Panel: Data Management Features

#### 1. **List Page**
The **List Page** displays entities in a tabular format, enabling sorting, searching, and pagination. Users can efficiently browse or locate specific records.

#### 2. **Detail Page**
The **Detail Page** provides an interface for viewing and managing detailed attributes. Related data such as teachers and materials can be selected or modified.

--- 
This structured approach ensures a systematic development and management of the online course system backend.
</details>