


## Developing Frontend of a online course website
<details> 
<summary> 
Having established our understanding of Fluent CMS essentials like Entity Schemas, GraphQL-style Querying, and GrapeJS-based Page Design, we’re ready to build a frontend for an online course website.
</summary>

### Introduction of online course website
The online course website is designed to help users easily find courses tailored to their interests and goals. 

- **Home Page(`home`)**: This is the main entry point, providing `Featured Course`, `Advanced Course`, etc. Each course in these sections links to its Course Details page.

- **Latest Courses(`course`)**: A curated list of the newest courses. Each course in this section links to its Course Details page.

- **Course Details(`course/{course_id}`)**: This page provides a comprehensive view of a selected course. Users can navigate to the **Teacher Details** page to learn more about the instructor. 

- **Teacher Details(`teacher/{teacher_id}`)**: Here, users can explore the profile of the instructor, This page contains a `teacher's latest course section`, each course in the sections links back to **Course Details** 
---

```plaintext
             Home Page
                 |
                 |
       +-------------------+
       |                   |
       v                   v
 Latest Courses       Course Details 
       |                   |        
       |                   |       
       v                   v            
Course Details <-------> Teacher Details 

```
### Designing the Home Page
The home page's screenshot shows below.
![Page](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-home-course.png)

In the page designer, we drag a component `Content-B`, set it's `multiple-records` component's data source to Query  `course`.  
The query might return data like
```json
[
  {
    "name": "Object-Oriented Programming(OOP)",
    "id": 20,
    "teacher":{
      "id": 3,
      "firstname": "jane"
    }
  }
]
```
We set link href of each course item to `/pages/course/{{id}}`. 
![Link](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-link.png)   
HandleBar rendering engine renders the link as  `/pages/course/20` by replacing `{{id}}` to `20`.

### Creating Course Detail Page
We name this page `course/{course_id}` to capture the path parameter course_id. 
For example, with the URL `/pages/course/20`, we obtain `{course_id: 20}`. This parameter is passed to the Query Service, which then filters data to match:

```json
{
  "fieldName": "id",
  "operator": "and",
  "omitFail": true,
  "constraints": [
    {
      "match": "in",
      "value": "qs.course_id"
    }
  ]
}
```
The query service produces a where clause as  `where id in (20)`.

### Link to Teacher Detail Page
We set the link of each teacher item as  `/pages/teacher/{{teacher.id}}`, allowing navigation from Course Details to Teacher Details:
For below example data, HandlerBar render the link as `/pages/teacher/3`.
```json
[
  {
    "name": "Object-Oriented Programming(OOP)",
    "id": 20,
    "teacher":{
      "id": 3,
      "firstname": "jane"
    }
  }
]
```
### Creating Teacher's Detail Page
![](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-teacher.png)

Similarly, we name this page as `teacher/{teacher_id}` and set its data source Query to `teacher`. For the URL /pages/teacher/3, the query returns:
```json
{
  "id": 3,
  "firstname": "Jane",
  "lastname": "Debuggins",
  "image": "/2024-10/b44dcb4c.jpg",
  "bio": "<p><strong>Ms. Debuggins</strong> is a seasoned software developer with over a decade of experience in full-stack development and system architecture. </p>",
  "skills": [
    {
      "id": 1,
      "name": "C++",
      "years": 3,
      "teacher_id": 3
    }
  ]
}
```

To add a list of courses by the teacher, we set a `multiple-records` component with Query `course`. 
![](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-teacher.png)
When rendering the Teacher Page, PageService sends `{teacher_id: 3}` to Query `course`. 
The QueryService Apply below filter, resulting in  `WHERE teacher in (3)`.

``` json
    {
      "fieldName": "teacher",
      "operator": "and",
      "omitFail": true,
      "constraints": [
        {
          "match": "in",
          "value": "qs.teacher_id"
        }
      ]
    }
```
This design creates an interconnected online course site, ensuring users can explore course details, instructors.
</details>
