

---
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

---

### Relationships
- **Teachers to Courses**: One-to-Many (Each teacher can teach multiple courses; each course is assigned to one teacher).
- **Teachers to Skills**: Many-to-Many (Multiple teachers can share skills, and one teacher may have multiple skills).
- **Courses to Materials**: Many-to-Many (A course may include multiple materials, and the same material can be used in different courses).

---

### Schema Creation via Fluent CMS Schema Builder

#### Accessing Schema Builder
After launching the web application, locate the **Schema Builder** menu on the homepage to start defining your schema.

#### Adding Entities
[Example Configuration](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/schema-ui/list.html?schema=entity)  
1. Navigate to the **Entities** section of the Schema Builder.
2. Create entities such as "Teacher" and "Course."
3. For the `Course` entity, add attributes such as `name`, `status`, `level`, and `description`.
---
### Defining Relationships
[Example Configuration](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/schema-ui/edit.html?schema=entity&id=27)  

#### 1. **Course and Teacher (Many-to-One Relationship)**
To establish a many-to-one relationship between the `Course` and `Teacher` entities, you can include a `Lookup` attribute in the `Course` entity. This allows selecting a single `Teacher` record when adding or updating a `Course`.

| **Attribute**  | **Value**    |
|----------------|--------------|
| **Field**      | `teacher`    |
| **Type**       | Lookup       |
| **Options**    | Teacher      |

**Description:** When a course is created or modified, a teacher record can be looked up and linked to the course.

#### 2. **Course and Materials (Many-to-Many Relationship)**
To establish a many-to-many relationship between the `Course` and `Material` entities, use a `Crosstable` attribute in the `Course` entity. This enables associating multiple materials with a single course.

| **Attribute**    | **Value**    |
|-------------------|--------------|
| **Field**         | `materials` |
| **Type**          | Crosstable  |
| **Options**       | Material    |

**Description:** When managing a course, you can select multiple material records from the `Material` table to associate with the course.

---

### Admin Panel: Data Management Features

#### 1. **List Page**
[Example Course List Page](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20)  
The **List Page** displays entities in a tabular format, enabling sorting, searching, and pagination. Users can efficiently browse or locate specific records.

#### 2. **Detail Page**
[Example Course Detail Page](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course/22)  
The **Detail Page** provides an interface for viewing and managing detailed attributes. Related data such as teachers and materials can be selected or modified.

--- 
</details>