
## Design Web Page
In previous chapter, we have defined query APIs to combine data from multiple entities, now is time to design front-end web pages to render the data.
To manage pages, go to `schema builder` > `pages`.

FluentCMS using open source html design tool GrapesJS to design web pages.   
GrapesJS has a flexible user interface with four main panels that help in designing and managing web pages. Here’s an overview of the four main panels in the GrapesJS toolbox:

1. **Style Manager**: Allows users to customize the styles (CSS properties) of the selected element on the canvas. You can adjust properties like color, size, margin, padding, etc.
2. **Traits Panel**: This panel is used to modify the attributes of the selected element, such as the source of an image, link targets, or other custom attributes. It is highly customizable and can be extended to add specific traits based on the needs of the design.
3. **Layers Panel**: The Layers panel provides a hierarchical view of the page elements, similar to the DOM structure.
4. **Blocks Panel**: This panel contains pre-made blocks or components that can be dragged and dropped onto the canvas. These blocks can be anything from text, images, buttons, forms, and other HTML elements.

These panels work together to provide a comprehensive web design experience, allowing users to build complex layouts with ease.
![Grapes.js-toolbox](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/grapes-toolbox.png)

### Landing Page
![LandingPage](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/landing-page.png)
1. For above page, the data comes from 3 Queries
    - Featured Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?status=featured
    - Advanced Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?level=Advanced
    - Beginner Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?level=Beginner
2. Drag a Content Block from `Blocks Panel` > `Extra` to Canvas,
   To Bind a multiple records trait to a data source, hover mouse to a element with `Multiple-records` tooltips, select the element, then the traits panels shows. There are the following options
    - field
    - query
    - qs : stands for query string, e.g. the Beginner Course section use level=Beginner to add a constraint only beginner course can show in this section.
    - offset
    - limit
      ![Grapes](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/graps-traits.png)
### Detail Page
We normally give a router parameter to Detail page, e.g. https://fluent-cms-admin.azurewebsites.net/pages/course/7.  
The suffix `.detail` should be added to page name, the page `course.detail` corresponds to above path.  
Detail page need to call query with query parameter `router.key`

You can also add `Multipe-records` elements to detail page, if you don't specify query, page render tries to resolve the field from query result of the page.
