import {customTypes} from "../grapes-components/custom-types.js";
import {customBlocks} from "../grapes-components/custom-blocks.js";
$(document).ready(function() {
    const editor = loadEditor();
    addCustomTypes(editor);
    addCustomBlocks(editor);

    const searchParams = new URLSearchParams(window.location.search);
    let id = searchParams.get("id");
    
    editor.on('load',async () => {
        if (id) {
            $.LoadingOverlay("show");
            const {data, error} = await one(id);
            $.LoadingOverlay("hide");
            if (data){
                id = data.id;
                restoreFormData(data);
                editor.setComponents(JSON.parse(data.components));
                editor.setStyle(JSON.parse(data.styles));
            }else {
                $('#errorPanel').text(error).show();
            }
        }
    });

    $('#visitPage').on('click', function () {
        const name = $(`#name`).val();
        if (name){
            window.location.href = `/pages/${name}`;
        }
    });
    
    $('#savePage').on('click', async function() {
        console.log(editor.getHtml());
        
        const payload = {
            html: editor.getHtml(),
            css: editor.getCss(),
            components:JSON.stringify(editor.getComponents()),
            styles: JSON.stringify(editor.getStyle()),
            type: 'page',
        };
        
        if (id) {
            payload.id = +id;
        }
        if (!collectFormData(payload)){
            return false;
        }
        
        $.LoadingOverlay("show");
        const {data, error} = await save(payload);
        if (data) {
            alert("submit succeed!");
            if (!id) {
                window.location.href = `page.html?id=${data.name}`;
            }
            $('#errorPanel').text('').hide();
        } else {
            $('#errorPanel').text(error).show();
        }
        $.LoadingOverlay("hide"); 
    });
});

function loadEditor() {
    return grapesjs.init({
        storageManager: false,
        container: '#gjs',
        plugins: [
            'gjs-blocks-basic',
            'grapesjs-preset-webpage'
        ],
        pluginsOpts: {
            'grapesjs-preset-webpage':{
                flexGrid: true, // Enables flex-based grid, optional
            },
            'gjs-blocks-basic': {
                flexGrid: true, // Enables flex-based grid, optional
            },
        },
        canvas: {
            scripts: [],
            styles: [
                'https://cdnjs.cloudflare.com/ajax/libs/tailwindcss/2.0.2/tailwind.min.css',
            ],
        }

    });
}
function addCustomBlocks(editor){
    for(const [name, content] of  Object.entries(customBlocks)){
        editor.Blocks.add(name, {
            label:name,
            content: content,
        });
    }
}
function addCustomTypes(editor){
    for(const [name, traits] of  Object.entries(customTypes)){
        editor.Components.addType(name, {
            model: {
                defaults: {traits},
            }
        });       
    }
}


const controls = ['name']

function restoreFormData(payload){
    for (const ctl of controls){
       $(`#${ctl}`).val(payload[ctl]);
    }
}

function collectFormData(payload){
    for (const ctlName of controls){
        const ctl = $(`#${ctlName}`);
        if (ctl.prop('required') && !ctl.val()){
            $('#errorPanel').text(`${ctlName} can not be empty`).show();
            return false;
        }
        payload[ctlName] = ctl.val();
    }
    return true;
}