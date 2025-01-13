export const threeLayerMenu =
    {
        category:'Navigation',
        name: 'three-level-menu',
        label: 'Three level menu',
        media: `<svg viewBox="0 0 1024 1024" class="icon"  xmlns="http://www.w3.org/2000/svg"><path d="M214.5 264.6h587.4v513.9H214.5z" fill="#E1F0FF" /><path d="M214.5 240.2h587.4v97.9H214.5zM801.9 240.2h24.5v538.4h-24.5zM190 240.2h24.5v538.4H190z" fill="#446EB1" /><path d="M214.5 460.4h587.4v24.5H214.5zM214.5 607.3h587.4v24.5H214.5z" fill="#6D9EE8" /><path d="M385.8 338.1h24.5v416h-24.5zM606.1 338.1h24.5v416h-24.5z" fill="#6D9EE8" /><path d="M214.5 754.1h587.4v24.5H214.5z" fill="#446EB1" /></svg>`,
        content: `
<ul data-gjs-type="data-list" data-source="data-list" class="menu bg-base-200 rounded-box w-56">
  <li>
    <details open>
      <summary>
        <a href=""> {{name}} </a>
      </summary>
      <ul>
      {{#each children}}
        <li>
            <details open>
                <summary>
                   <a href="">{{name}}</a>
                </summary>
                <ul>
                    {{#each children}}
                    <li>
                        <a href=""> {{name}}</a>
                    </li>
                    {{/each}}
                </ul>
            </details>
        </li> 
      {{/each}}
      </ul>
    </details>
  </li>
</ul>
 `
    }