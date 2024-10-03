


## Designing Web Page in FluentCMS

<details> <summary> The page designer is built using the open-source project GrapesJS and Handlebars, allowing you to bind `GrapesJS Components` with `FluentCMS Queries` for dynamic content rendering. </summary>

### Introduction to GrapesJS Panels
The GrapesJS Page Designer UI provides a toolbox with four main panels:  
![GrapesJS Toolbox](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/grapes-toolbox.png)  
1. **Style Manager**: Lets users customize CSS properties of selected elements on the canvas. FluentCMS does not modify this panel.  
2. **Traits Panel**: Allows you to modify attributes of selected elements. FluentCMS adds custom traits to bind data to components here.  
3. **Layers Panel**: Displays a hierarchical view of page elements similar to the DOM structure. FluentCMS does not customize this panel, but it’s useful for locating FluentCMS blocks.  
4. **Blocks Panel**: Contains pre-made blocks or components for drag-and-drop functionality. FluentCMS adds its own customized blocks here.  

### Tailwind CSS Support
FluentCMS includes Tailwind CSS by default for page rendering, using the following styles:

```html
<link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/base.min.css">
<link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/components.min.css">
<link rel="stylesheet" href="https://unpkg.com/@tailwindcss/typography@0.1.2/dist/typography.min.css">
<link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/utilities.min.css">
```

### Page Types: Landing Page, Detail Page, and Home Page

#### **Landing Page**: A landing page is typically the first page a visitor sees.   
The URL format is `/page/<pagename>`.    
A landing page is typically composed of multiple `Multiple Records Components`, each with its own `Query`, making the page-level `Query` optional.

![Landing Page](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-landing.png)

#### **Detail Page**: A detail page provides specific information about an item.  
The URL format is `/page/<pagename>/<router parameter>`, FluentCMS retrieves data by passing the router parameter to the `FluentCMS Query`. 

For the following settings  
- Page Name: `course/{id}`  
- Query: `courses`  
FluentCMS will call the query `https://fluent-cms-admin.azurewebsites.net/api/queries/courses/one?id=3` for URL `https://fluent-cms-admin.azurewebsites.net/pages/course/3`

![Course Detail Page](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-course.png)

#### **Home Page**:
The homepage is a special landing page with the name `home`. Its URL is `/pages/home`. If no other route handles the path `/`, FluentCMS will render `/` as `/pages/home`.

### Data Binding: Singleton or Multiple Records

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

However, you won’t see the `{{#each}}` statement in the GrapesJS Page Designer. FluentCMS adds it automatically for any block under the `Multiple Records` category.

Steps to bind multiple records:  
1. Drag a block from the `Multiple Records` category.
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

### Linking and Images

FluentCMS does not customize GrapesJS' Image and Link components, but locating where to input `Query Field` can be challenging. The steps below explain how to bind them.

- **Link**:
  Locate the link by hovering over the GrapesJS component or finding it in the `GrapesJS Layers Panel`. Then switch to the `Traits Panel` and input the detail page link, e.g., `/pages/course/{{id}}`. FluentCMS will render this as `<a href="/pages/course/3">...</a>`.

- **Image**:
  Double-click on the image component, input the image path, and select the image. For example, if the image field is `thumbnail_image_url`, input `/files/{{thumbnail_image_url}}`. FluentCMS will replace `{{thumbnail_image_url}}` with the actual field value.

  ![Designer Image](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-image.png)

### Customized Blocks
FluentCMS adds customized blocks to simplify web page design and data binding for `FluentCMS Queries`. These blocks use Tailwind CSS.

- **Multiple Records**: Components in this category contain subcomponents with a `Multiple-Records` trait.
- **Card**: Typically used in detail pages.
- **Header**: Represents a navigation bar or page header.
</details>