# Fluent CMS

## Why another CMS
A wide range of websites can benefit from using a Content Management System (CMS), 
such as Blogs, E-commerce, News, Online Learning, Real Estate Listing. 
They all need a convenient way to to manage content.

The most Popular CMS system is Wordpress, it is easy to use, with some configuration and plug-in, a none-programmer can
easily build a website. But Wordpress has it's limitations:
1. it's hard to extend it's entities, the most important Entity of Wordpress is Posts, Terms, if you want more attribute
   to Post, the new attributes are saved as Key-Value Pair. It's very difficult to use the data outside of wordpress.
2. Wordpress has performance issue, for one request, Wordpress have to access database many times. Wordpress team are 
   tread performance of flexibility.
3. it's hard to add feature to Wordpress, you have to be familiar with Wordpress Hook and Plug-in to extend Wordpress.

Headless CMSs are becoming increasingly popular due to their flexibility and ability to 
deliver content across multiple platforms and devices. I have tried to build a social media platform based on Strapi.
I like strapi's idea of put schema's definition to a .json file, so we can easily add new entities and extend entities
But the overall developing experience with Strapi is not good.
1. it's hard to extend strapi's feature, without strong type(strapi is written in node.js), 
it's very hard to extend a strapi.
2. strapi's performance is also not good, again, strapi team is also tread performance for flexibility.

So I think why not build a CMS myself, using the language and framework I am comfortable with. It's not difficult with 
right tools and focus. 
## System overview
![img.png](doc/images/overview.png)

### Frontend UI
For demo purpose, I build a frontend presentation App based on stablo's next.js template. Thanks for stablo's nice and clean template.
- next.js
- stablo https://github.com/web3templates/stablo

![frontend_ui/homepage](doc/images/frontend_ui/homepage.png)

![frontend_ui/post-page](doc/images/frontend_ui/postpage.png)

### Admin UI
- react
- prime react https://primereact.org/
- swr https://swr.vercel.app/

![admin_ui/post_list](doc/images/admin_ui/postlist_page.png)

![admin_ui/post_edit](doc/images/admin_ui/post-edit-page.png)
### Schema Editor
- json-editor https://github.com/json-editor/json-editor
![schema-editor](doc/images/schema_editor/schema-edit-page.png)
### Server
- asp.net core
- entity framework core
- sqlkata, it using dapper ORM behind the scene(https://sqlkata.com/)

Both entity framework and sqlkata can abstract query from specific Database dialect, so I extract database access to 
another layer, currently fluent-cms support postgres sql, it can easily support SQL Server and MySQL in the future. 




