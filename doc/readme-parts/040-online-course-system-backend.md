

## Online Course System Backend
<details> 
<summary> 
The following chapter will guide you through developing a simple online course system, starts with three entity `Teachers`, `Courses`, and `Students`. 
</summary>

### Database Schema
#### 1. **Teachers Table**
This table stores information about the teachers.

| Column Name   | Data Type  | Description                 |
|---------------|------------|-----------------------------|
| `Id`          | `INT`      | Primary Key, unique ID for each teacher. |
| `FirstName`   | `VARCHAR`  | Teacher's first name.        |
| `LastName`    | `VARCHAR`  | Teacher's last name.         |
| `Email`       | `VARCHAR`  | Teacher's email address.     |
| `PhoneNumber` | `VARCHAR`  | Teacher's contact number.    |

#### 2. **Courses Table**
This table stores information about the courses.

| Column Name   | Data Type  | Description                   |
|---------------|------------|-------------------------------|
| `Id`          | `INT`      | Primary Key, unique ID for each course. |
| `CourseName`  | `VARCHAR`  | Name of the course.            |
| `Description` | `TEXT`     | Brief description of the course. |
| `TeacherId`   | `INT`      | Foreign Key, references `TeacherId` in the `Teachers` table. |

#### 3. **Students Table**
This table stores information about the students.

| Column Name      | Data Type  | Description                   |
|------------------|------------|-------------------------------|
| `Id`             | `INT`      | Primary Key, unique ID for each student. |
| `FirstName`      | `VARCHAR`  | Student's first name.         |
| `LastName`       | `VARCHAR`  | Student's last name.          |
| `Email`          | `VARCHAR`  | Student's email address.      |
| `EnrollmentDate` | `DATE`     | Date when the student enrolled. |

#### 4. **Enrollments Table (Junction Table)**
This table manages the many-to-many relationship between `Students` and `Courses`, since a student can enroll in multiple courses, and a course can have multiple students.

| Column Name   | Data Type  | Description                           |
|---------------|------------|---------------------------------------|
| `EnrollmentId`| `INT`      | Primary Key, unique ID for each enrollment. |
| `StudentId`   | `INT`      | Foreign Key, references `StudentId` in the `Students` table. |
| `CourseId`    | `INT`      | Foreign Key, references `CourseId` in the `Courses` table. |

#### Relationships:
- **Teachers to Courses**: One-to-Many (A teacher can teach multiple courses, but a course is taught by only one teacher).
- **Students to Courses**: Many-to-Many (A student can enroll in multiple courses, and each course can have multiple students).
### Build Schema use Fluent CMS Schema builder
After starting your ASP.NET Core application, you will find a menu item labeled "Schema Builder" on the application's home page.

In the Schema Builder, you can add entities such as "Teacher" and "Student."

When adding the "Course" entity, start by adding basic attributes like "Name" and "Description." You can then define relationships by adding attributes as follows:

1. **Teacher Attribute:**  
   Configure it with the following settings:
   ```json
   {
      "DataType": "Int",
      "Field": "teacher",
      "Header": "Teacher",
      "InList": true,
      "InDetail": true,
      "IsDefault": false,
      "Type": "lookup",
      "Options": "teacher"
   }
   ```

2. **Students Attribute:**  
   Configure it with these settings:
   ```json
   {
      "DataType": "Na",
      "Field": "students",
      "Header": "Students",
      "InList": false,
      "InDetail": true,
      "IsDefault": false,
      "Type": "crosstable",
      "Options": "student"
   }
   ```

With these configurations, your minimal viable product is ready to use.
</details>
