

## Drag and Drop Page Designer
<details> 
<summary> 
The page designer utilizes the open-source GrapesJS and Handlebars, enabling seamless binding of `GrapesJS Components` with `FluentCMS Queries` for dynamic content rendering. 
</summary>

---
### Page Types: Landing Page, Detail Page, and Home Page

#### **Landing Page**
A landing page is typically the first page a visitor sees.  
- **URL format**: `/page/<pagename>`  
- **Structure**: Comprised of multiple sections, each section retrieves data via a `query`.  

**Example**:    
[Landing Page](https://fluent-cms-admin.azurewebsites.net/)    
This page fetches data from:  
- [https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured)  
- [https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced)  

---

#### **Detail Page**
A detail page provides specific information about an item.  
- **URL format**: `/page/<pagename>/<router parameter>`  
- **Data Retrieval**: FluentCMS fetches data by passing the router parameter to a `query`.  

**Example**:  
[Course Detail Page](https://fluent-cms-admin.azurewebsites.net/pages/course/22)  
This page fetches data from:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/course/one?course_id=22](https://fluent-cms-admin.azurewebsites.net/api/queries/course/one?course_id=22)

---

#### **Home Page**
The homepage is a special type of landing page named `home`.  
- **URL format**: `/pages/home`   
- **Special Behavior**: If no other route matches the path `/`, FluentCMS renders `/pages/home` by default.  

**Example**:    
The URL `/` will be resolved to `/pages/home` unless explicitly overridden.  

---
### Introduction to GrapesJS Panels

Understanding the panels in GrapesJS is crucial for leveraging FluentCMS's customization capabilities in the Page Designer UI. This section explains the purpose of each panel and highlights how FluentCMS enhances specific areas to streamline content management and page design. 

![GrapesJS Toolbox](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/grapes-toolbox.png)

1. **Style Manager**:
    - Used to customize CSS properties of elements selected on the canvas.
    - *FluentCMS Integration*: This panel is left unchanged by FluentCMS, as it already provides powerful styling options.

2. **Traits Panel**:
    - Allows modification of attributes for selected elements.
    - *FluentCMS Integration*: Custom traits are added to this panel, enabling users to bind data to components dynamically.

3. **Layers Panel**:
    - Displays a hierarchical view of elements on the page, resembling a DOM tree.
    - *FluentCMS Integration*: While FluentCMS does not alter this panel, it’s helpful for locating and managing FluentCMS blocks within complex page designs.

4. **Blocks Panel**:
    - Contains pre-made components that can be dragged and dropped onto the page.
    - *FluentCMS Integration*: FluentCMS enhances this panel by adding custom-designed blocks tailored for its CMS functionality.

By familiarizing users with these panels and their integration points, this chapter ensures a smoother workflow and better utilization of FluentCMS's advanced page-building tools.

---
### Data Binding: Singleton or List

FluentCMS uses [Handlebars expression](https://github.com/Handlebars-Net/Handlebars.Net) for dynamic data binding.

#### Singleton
Singleton fields are enclosed within `{{ }}`.

![Singleton Field](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-single-field.png)

#### Multiple Records
`Handlebars` loops over arrays using the `each` block.

```handlebars
{{#each course}}
    <li>{{title}}</li>
{{/each}}
```

However, you won’t see the `{{#each}}` statement in the GrapesJS Page Designer. FluentCMS adds it automatically for any block under the `List` category.

Steps to bind multiple records:  
1. Drag a block from the `List` category.
    ![Multiple Record Blocks](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-multiple-record-block.png)
2. Hover over the GrapesJS components to find a block with the `Multiple-records` tag in the top-left corner, then click the `Traits` panel. You can also use the GrapesJS Layers Panel to locate the component.
    ![Multiple Record Select](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-multiple-record-select.png)  
3. In the `Traits` panel, you have the following options:
    - **Field**: Specify the field name for the Page-Level Query (e.g., for the FluentCMS Query below, you could set the field as `teacher.skills`).  
      ```json
      {
        "teacher": {
          "firstname": "",
          "skills": [
            {
              "name": "cooking fish",
              "years": 3
            }
          ]
        }
      }
      ```   
    - **Query**: The query to retrieve data.  
    - **Qs**: Query string parameters to pass (e.g., `?status=featured`, `?level=Advanced`).  
    - **Offset**: Number of records to skip.  
    - **Limit**: Number of records to retrieve.  
    - **Pagination** There are 3 Options: 
      - `Button`, content is divided into multiple pages, and navigation buttons (e.g., "Next," "Previous," or numbered buttons) are provided to allow users to move between the pages.
      - `Infinite Scroll` , Content automatically loads as the user scrolls down the page, providing a seamless browsing experience without manual page transitions. It's better to set only one component to `infinite scroll`, and put it to the bottom of the pages. 
      - `None`. Users see all the available content at once, without the need for additional actions.
   ![Multiple Record Trait](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-multiple-record-trait.png)

---
### Linking and Images

FluentCMS does not customize GrapesJS' Image and Link components, but locating where to input `Query Field` can be challenging. The steps below explain how to bind them.

- **Link**:
  Locate the link by hovering over the GrapesJS component or finding it in the `GrapesJS Layers Panel`. Then switch to the `Traits Panel` and input the detail page link, e.g., `/pages/course/{{id}}`. FluentCMS will render this as `<a href="/pages/course/3">...</a>`.

- **Image**:
  Double-click on the image component, input the image path, and select the image. For example, if the image field is `thumbnail_image_url`, input `/files/{{thumbnail_image_url}}`. FluentCMS will replace `{{thumbnail_image_url}}` with the actual field value.

  ![Designer Image](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-image.png)

---
### Customized Blocks
FluentCMS adds customized blocks to simplify web page design and data binding for `FluentCMS Queries`. These blocks use Tailwind CSS.

- **Multiple Records**: Components in this category contain subcomponents with a `Multiple-Records` trait.
- **Card**: Typically used in detail pages.
- **Header**: Represents a navigation bar or page header.
</details>