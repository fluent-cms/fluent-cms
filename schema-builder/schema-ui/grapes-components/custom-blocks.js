/*
data-gjs-type: for grapes.js attach trait
data-component-type: for back end to prepare data
* */

export const customBlocks = [
    {
        name:'content-b',
        label: 'Content B',
        media:`<svg viewBox="0 0 24 24">
        <path d="M14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4m-5.2 0L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4z"></path>
      </svg>`,
        content:`
<section class="text-gray-600 body-font">
  <div class="container px-5 py-15 mx-auto">
    <div class="flex flex-wrap w-full mb-20">
      <div class="lg:w-1/2 w-full mb-6 lg:mb-0">
        <h1 class="sm:text-3xl text-2xl font-medium title-font mb-2 text-gray-900">Pitchfork Kickstarter Taxidermy</h1>
        <div class="h-1 w-20 bg-red-500 rounded"></div>
      </div>
      <p class="lg:w-1/2 w-full leading-relaxed text-gray-500">Whatever cardigan tote bag tumblr hexagon brooklyn asymmetrical gentrify, subway tile poke farm-to-table. Franzen you probably haven't heard of them man bun deep jianbing selfies heirloom prism food truck ugh squid celiac humblebrag.</p>
    </div>
    <div data-add="2" class="flex flex-wrap -m-4" data-gjs-type="image-card-list" data-source-type="multiple-records">
      {{#each items}}
      <div class="xl:w-1/4 md:w-1/2 p-4">
        <div class="bg-gray-100 p-6 rounded-lg">
          <a href="/pages/page-name/{{slug}}" style="display: inline-block" >
            <img class="h-40 rounded w-full object-cover object-center mb-6" src="/files{{image}}" alt="content" >
          </a>
          <h3 class="tracking-widest text-red-500 text-xs font-medium title-font">{{subtitle}}</h3>
          <h2 class="text-lg text-gray-900 font-medium title-font mb-4"><a href="/pages/page-name/{{slug}}">{{title}}</a></h2>
          <p class="leading-relaxed text-base">{{{desc}}}</p>
        </div>
      </div>
      {{/each}}
    </div>
  </div>
</section>  
        `
    },
    {
        name:'hero-b',
        label: 'Hero B',
        media: `<svg viewBox="0 0 24 24">
        <path d="M14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4m-5.2 0L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4z"></path>
      </svg>`,
        content: `
<section class="text-gray-600 body-font">
  <div class="container mx-auto flex px-5 py-24 items-center justify-center flex-col">
    <img class="lg:w-2/6 md:w-3/6 w-5/6 mb-10 object-cover object-center rounded" alt="hero" src="/files{{image}}">
    <div class="text-center lg:w-2/3 w-full">
      <h1 class="title-font sm:text-4xl text-3xl mb-4 font-medium text-gray-900">{{title}}</h1>
      <p class="mb-8 leading-relaxed">{{{Content}}}</p>
    </div>
  </div>
</section> 
        `,
            
        
    },
    {
        name:'header-b',
        label: 'Header B',
        media: `<svg viewBox="0 0 24 24">
        <path d="M14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4m-5.2 0L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4z"></path>
      </svg>`,
        content: `
 <header class="text-gray-600 body-font">
  <div class="container mx-auto flex flex-wrap p-5 flex-col md:flex-row items-center">
    <a class="flex title-font font-medium items-center text-gray-900 mb-4 md:mb-0">
      <svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="w-10 h-10 text-white p-2 bg-indigo-500 rounded-full" viewBox="0 0 24 24">
        <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
      </svg>
      <span class="ml-3 text-xl">Tailblocks</span>
    </a>
    <nav class="md:mr-auto md:ml-4 md:py-1 md:pl-4 md:border-l md:border-gray-400	flex flex-wrap items-center text-base justify-center">
      <a class="mr-5 hover:text-gray-900">First Link</a>
      <a class="mr-5 hover:text-gray-900">Second Link</a>
      <a class="mr-5 hover:text-gray-900">Third Link</a>
      <a class="mr-5 hover:text-gray-900">Fourth Link</a>
    </nav>
    <a href="/admin" class="inline-flex items-center bg-gray-100 border-0 py-1 px-3 focus:outline-none hover:bg-gray-200 rounded text-base mt-4 md:mt-0">Admin Panel
      <svg fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="w-4 h-4 ml-1" viewBox="0 0 24 24">
        <path d="M5 12h14M12 5l7 7-7 7"></path>
      </svg>
    </a>
  </div>
</header>    
    `,
    },
    {
        name: 'ecommerce-a',
        label: `Ecommerce A`,
        media: `<svg viewBox="0 0 24 24">
        <path d="M14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4m-5.2 0L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4z"></path>
</svg>`,
        content: `
<section class="text-gray-600 body-font">
  <div class="container px-5 py-10 mx-auto">
    <div class="lg:w-1/2 w-full mb-6 lg:mb-0">
        <h1 class="sm:text-3xl text-2xl font-medium title-font mb-2 text-gray-900">Category Name</h1>
        <div class="h-1 w-20 bg-red-500 rounded"></div>
    </div>
    <div class="flex flex-wrap -m-4" data-gjs-type="image-card-list" data-source-type="multiple-records">
      {{#each items}}
      <div class="lg:w-1/4 md:w-1/2 p-4 w-full">
        <a  style="display: inline-block" href="/pages/pageName/{{slug}}">
          <img alt="ecommerce" class="object-cover object-center w-full h-full block" src="/files{{image}}">
        </a>
        <div class="mt-4">
          <h3 class="text-gray-500 text-xs tracking-widest title-font mb-1">{{category}}</h3>
          <h2 class="text-gray-900 title-font text-lg font-medium"><a href="/pages/pageName/{{slug}}">{{name}}</h2>
          <p class="mt-1">{{price}}</p>
        </div>
      </div>
      {{/each}}
    </div>
  </div>
</section> 
        `
    }
]
