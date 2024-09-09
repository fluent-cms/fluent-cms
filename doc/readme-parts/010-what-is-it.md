
## What is it
Fluent CMS is an open-source Content Management System designed to streamline web development workflows.
It proves valuable even for non-CMS projects by eliminating the need for tedious CRUD API and page development.
- **CRUD:** Fluent CMS offers built-in RESTful CRUD (Create, Read, Update, Delete) APIs along with an Admin Panel that supports a wide range of input types, including datetime, dropdown, image, and rich text, all configurable to suit your needs.
- **GraphQL-style Query** Retrieve multiple related entities in a single call, enhancing security, performance, and flexibility on the client side.
- **Wysiwyg Web Page Designer:** Leveraging [Grapes.js](https://grapesjs.com/) and [HandleBars](https://handlebarsjs.com/), the page designer allows you to create pages and bind query data without coding.
- **Permission Control** Assign read/write, read-only, access to entities based on user roles or individual permissions.
- **Integration and extension** Fluent CMS can be integrated into projects via a NuGet package.
  Validation logic can be implemented using C# statements through [DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso),
  and complex functionalities can be extended using CRUD Hook Functions.
  Additionally, Fluent CMS supports message brokers like Kafka for CRUD operations.
- **Performance:** Utilizing [SqlKata](https://sqlkata.com/) and [Dapper](https://www.learndapper.com/), Fluent CMS achieves performance levels comparable to manually written RESTful APIs using Entity Framework Core. Performance benchmarks include comparisons against Strapi and Entity Framework.
    - [performance vs Strapi](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-strapi.md)
    - [performance vs EF](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-entity-framework.md)
