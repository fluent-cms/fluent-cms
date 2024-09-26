import { define, html } from 'https://esm.sh/hybrids@^9';
define({
    tag: "nav-bar",
    name: '',
    render: () => html`
    <nav class="navbar navbar-expand-lg navbar-light bg-light">
        <a class="navbar-brand" href="/schema-ui/list.html">
            <img alt="logo" src="img/fluent-cms.png" height="40" class="mr-2">
        </a>
        <div class="navbar-nav">
            <a class="nav-item nav-link border-item" href="./list.html">All Schemas</a>
            <a class="nav-item nav-link border-item" href="./list.html?schema=entity">Entities</a>
            <a class="nav-item nav-link border-item" href="./list.html?schema=query">Queries</a>
            <a class="nav-item nav-link border-item" href="./list.html?schema=page">Pages</a>
            <a class="nav-item nav-link border-item" href="./edit.html?schema=menu&id=top-menu-bar">MenuItems</a>
            <a class="nav-item nav-link border-item" href="/admin">Admin Panel</a>
        </div>
    </nav> 
`,
});