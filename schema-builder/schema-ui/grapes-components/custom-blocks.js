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
    <div data-add="2" class="flex flex-wrap -m-4" data-gjs-type="multiple-records" data-source-type="multiple-records">
      <div class="xl:w-1/4 md:w-1/2 p-4">
        <div class="bg-gray-100 p-6 rounded-lg">
          <a href="/pages/page-name/{{slug}}" style="display: inline-block" >
            <img class="h-40 rounded w-full object-cover object-center mb-6" src="/files{{image}}" alt="content" >
          </a>
          <h3 class="tracking-widest text-red-500 text-xs font-medium title-font">{{subtitle}}</h3>
          <h2 class="text-lg text-gray-900 font-medium title-font mb-4"><a href="/pages/page-name/{{slug}}">{{title}}</a></h2>
          <p class="leading-relaxed text-base prose">{{{desc}}}</p>
        </div>
      </div>
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
<div class="container px-8 mx-auto xl:px-5  max-w-screen-lg py-5 lg:py-8 !pt-0">
  <div class="mx-auto max-w-screen-md ">
    <div class="flex justify-center"> <div class="flex gap-3"> <span class="inline-block text-xs font-medium tracking-wider mt-5 text-pink-600">{{tag}}</span> </div> </div>
    <h1 class="text-brand-primary mb-3 mt-2 text-center text-3xl font-semibold tracking-tight dark:text-white lg:text-4xl lg:leading-snug">{{title}}</h1>
    <div class="mt-3 flex justify-center space-x-3 text-gray-500 ">
      <div class="flex items-center gap-3">
        <div class="relative h-10 w-10 flex-shrink-0">
            <img alt="Li Bai" loading="lazy" decoding="async" data-nimg="fill" class="rounded-full object-cover" src="/files{{author.image}}" style="position: absolute; height: 100%; width: 100%; inset: 0px; color: transparent;"/>
        </div>
        <div class="flex items-center space-x-2 text-sm"> <p class="text-gray-800 dark:text-gray-400"> <a href=""> {{author.name}} </a> </p> </div>
        <div> <div class="flex items-center space-x-2 text-sm"> <time class="text-gray-500 dark:text-gray-400" >{{date}}</time> <span>{{time_to_read}}</span> </div>
      </div>
    </div>
  </div>
</div>
<div class="relative z-0 mx-auto aspect-video max-w-screen-lg overflow-hidden lg:rounded-lg"><img alt="Thumbnail" loading="eager" decoding="async" data-nimg="fill" class="object-cover" src="/files{{image}}"/></div>
<div class="container px-8 mx-auto xl:px-5  max-w-screen-lg py-5 lg:py-8"> 
    <article class="mx-auto max-w-screen-md ">
       <div class="prose mx-auto my-3 dark:prose-invert prose-a:text-blue-600"> {{{content}}}</div>
    </article>
</div>
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
    <h1 class="sm:text-3xl text-2xl font-medium title-font mb-2 text-gray-900">Category Name</h1>
    <div class="flex flex-wrap -m-4" data-gjs-type="multiple-records" data-source-type="multiple-records">
      <div class="lg:w-1/4 md:w-1/2 p-4 w-full">
        <a  style="display: inline-block" href="/pages/pageName/{{slug}}">
          <img alt="ecommerce" class="object-cover object-center w-full h-full block" src="/files{{image}}">
        </a>
        <div class="mt-4">
          <h3 class="text-gray-500 text-xs tracking-widest title-font mb-1">{{category}}</h3>
          <h2 class="text-gray-900 title-font text-lg font-medium"><a href="/pages/pageName/{{slug}}">{{title}}</h2>
          <p class="mt-1">{{price}}</p>
        </div>
      </div>
    </div>
  </div>
</section> 
        `
    },
    {
        name: 'card-a',
        label: 'Card A',
        media: `<svg viewBox="0 0 24 24">
        <path d="M14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4m-5.2 0L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4z"></path>
</svg>`,
        content: `
<div class="container px-8 mx-auto xl:px-5  max-w-screen-lg py-5 lg:py-8 !pt-0">
  <div class="mx-auto max-w-screen-md ">
    <div class="mt-3 rounded-2xl bg-gray-50 px-8 py-8 text-gray-500 dark:bg-gray-900 dark:text-gray-400">
      <div class="flex flex-wrap items-start sm:flex-nowrap sm:space-x-6">
        <div class="relative mt-1 h-24 w-24 flex-shrink-0 ">
            <a href="pages/{{page}}"> <img loading="lazy" decoding="async" data-nimg="fill" class="rounded-full object-cover" style="position:absolute;height:100%;width:100%;left:0;top:0;right:0;bottom:0;color:transparent" src="/files/{{image}}"> </a>
        </div>
        <div>
            <div class="mb-3"> <h3 class="text-lg font-medium text-gray-800 dark:text-gray-300">{{title}}</h3> </div>
            <div>{{{content}}}</div>
        </div>
      </div>
    </div>        
  </div>
</div>
 `
    },
    {
        name: 'list-a',
        label: 'List A',
        media: `<svg viewBox="0 0 24 24">
        <path d="M14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4m-5.2 0L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4z"></path>
</svg>`,
        content: `
<div class="mt-32">
    <div class="px-4 sm:px-8 max-w-5xl m-auto">
        <h1 class="text-center font-semibold text-sm">List Group</h1>
        <ul class="border border-gray-200 rounded overflow-hidden shadow-md"  data-gjs-type="multiple-records" data-source-type="multiple-records">
            <li class="px-4 py-2 bg-white hover:bg-sky-100 hover:text-sky-900 border-b last:border-none border-gray-200 transition-all duration-300 ease-in-out">{{title}}</li>
        </ul>
    </div>
</div>
 `
    }
    
]
