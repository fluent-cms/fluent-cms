

---
## Admin Panel
<details> 
<summary> 
This section introduce Admin Panel Data Management Features
</summary>


### DisplayType
 Admin Panel can display attribute as different UI Controls

- `"text"`:  a single line text input
- `"textarea"`: multiple line text input
- `"editor"`: rich text input

- `"number"`: single line text input allow only number
- `"datatime"`: datetime picker, allow input date and time
- `"data"`: datetime picker, only allow input data

- `"image"`: allow upload single image, save the image URL 
- `"gallery"`:allow upload multiple image, save the image URLs
- `"file"`: allow upload file, save the file Url

- `"dropdown"`:  select item from pre-defined list
- `"multiselect"`: select multiple items from pre-defined list
- `"lookup"`: select item from another many-to-one entity, require `Lookup` (many-to-one) dataType

- `"picklist"`: select multiples item from another many-to-many entity, require `Junction` dataType
- `"edittable"`: manage items of another one-to-many sub entity , require `Collection` dataType

### DataType to DisplayType Mapping Table

Below is a mapping table of valid `DataType` and `DisplayType` combinations:

| **DataType**   | **DisplayType**          | **Description**                                  |
|-----------------|--------------------------|--------------------------------------------------|
| Int            | Number                   | Input for integers.                             |
| Datetime       | Datetime                 | Datetime picker for date and time input.        |
| Datetime       | Date                     | Date picker for date-only input.               |
| String         | Text                     | Single-line text input.                        |
| String         | Textarea                 | Multi-line text input.                         |
| String         | Image                    | Single image upload.                           |
| String         | Gallery                  | Multiple image uploads.                        |
| String         | File                     | File upload.                                   |
| String         | Dropdown                 | Select an item from a predefined list.         |
| String         | Multiselect              | Select multiple items from a predefined list.  |
| Text           | Multiselect              | Select multiple items from a predefined list.  |
| Text           | Gallery                  | Multiple image uploads.                        |
| Text           | Textarea                 | Multi-line text input.                         |
| Text           | Editor                   | Rich text editor.                              |
| Lookup         | Lookup                   | Select an item from another entity.            |
| Junction       | Picklist                 | Select multiple items from another entity.     |
| Collection     | EditTable                | Manage items of a sub-entity.                  |

### **List Page**  
[Example Course List Page](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20)    
The **List Page** displays entities in a tabular format, enabling sorting, searching, and pagination. Users can efficiently browse or locate specific records.  
#### Sorting  
You can apply sort by click the `↑` or `↓` icon on the table header.  
[Order by Created At Example](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20&sort[created_at]=-1)  
[Order by Name Example](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20&sort[name]=1)  
#### Filtering  
You can apply Filter by click the Funnel icon on the table header.  

[Here is example filter by `Created At` is on 2024-08-07] (https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20&created_at[dateIs]=2024-09-07&sort[created_at]=1)  
[Here is Example filter by `Course Name` starts with A or `Course Name` starts with C](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20&name[operator]=or&name[startsWith]=A&name[startsWith]=C&sort[created_at]=1)  
### **Detail Page**
[Example Course Detail Page](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course/22)  
The **Detail Page** provides an interface for viewing and managing detailed attributes. Related data such as teachers and materials can be selected or modified.

</details>