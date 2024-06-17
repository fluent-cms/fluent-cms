# Fluid CMS

## Why
The most Popular CMS system is Wordpress, it is easy to use, with some configuration and plug-in, a no programmer can
easily build a website. But Wordpress has it's limitations:
1. it's hard to extend it's entities, the most important Entity of Wordpress is Posts, Terms, if you want more attribute
   to Post, you have to add a key-value pair to table post_meta. That is counter-intuitive.
2. Wordpress has performance issue, for a Wordpress page, you have to access database many times. And PHP interpreter is
   too heavy comparing to modern Async Architecture.
3. it's hard to add feature to Wordpress, Wordpress's unique schema structure make it hard for other program to read
   it's content. If you stick with PHP and Wordpress, it's Hook and Plug-in also has it's unique pattern.

Fluid CMS is not intend to be as powerful as Wordpress, but simply to resolve above issues.
1. All entities, relationships between entities are configurable in Fluid CMS.
2. Fluid CMS focus on resolve Left Join, N+1 issue when developing.
3. Embrace modern web technology, such as asp.net core, react.

##